using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Menu de pausa + opciones.
 * Un solo script: abre/cierra con Tab, pausa el juego y aplica settings con PlayerPrefs.
 *
 * Debe vivir en un GameObject que arranque ACTIVO (ej: el Canvas o un
 * GameObject vacio "PauseSystem"), NUNCA dentro de PausePanel/OptionsPanel
 * porque esos arrancan desactivados.
 *
 * Si en algun futuro hace falta bloquear la pausa (cinematica, intro, etc),
 * llamar desde otro script: pauseMenu.SetPauseAllowed(false) y luego true.
 */
public class PauseMenuController : MonoBehaviour
{
    [Header("Paneles UI")]
    // Root completo de la UI de pausa (ej: PauseUI). Lo que contiene title + paneles.
    // Se activa/desactiva entero al pausar/despausar.
    [SerializeField] private GameObject pauseRoot;
    // Contenedor de los botones principales (Continuar/Ajustes/Volver). Se oculta al entrar a Opciones.
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Sliders - Opciones")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;

    [Header("Audio UI")]
    [SerializeField] private UIAudioManager uiAudioManager;



    [Header("Etiquetas numericas (opcional)")]
    // Si las dejas vacias no se muestra nada, no rompe.
    [SerializeField] private TMP_Text sensitivityValueLabel;
    [SerializeField] private TMP_Text brightnessValueLabel;
    [SerializeField] private TMP_Text masterVolumeValueLabel;
    [SerializeField] private TMP_Text musicVolumeValueLabel;
    [SerializeField] private TMP_Text sfxVolumeValueLabel;
    [SerializeField] private TMP_Text ambientVolumeValueLabel;

    [Header("Rangos")]
    [SerializeField] private float sensitivityMin = 0.1f;
    [SerializeField] private float sensitivityMax = 2f;
    [SerializeField] private float brightnessMinExposure = -1.5f;
    [SerializeField] private float brightnessMaxExposure = 1.5f;

    [Header("Modo")]
    // Si esta en true: skipea la logica de pausa (Tab, lock de player, etc).
    // Usar para el menu principal donde solo queremos los ajustes.
    [SerializeField] private bool menuMode = false;

    [Header("Referencias de juego")]
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio (AudioMixer)")]
    [SerializeField] private AudioMixer audioMixer;
    // Lowpass filter para el menu de pausa
    [SerializeField] private string pauseLowPassParam = "PauseLowPass";
    [SerializeField] private float normalLowpass = 22000f;
    [SerializeField] private float pausedLowpass = 500f;

    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string ambientVolumeParam = "AmbientVolume";

    [Header("Brillo (URP Post Exposure)")]
    // YA NO se usa para escribir, queda solo para no romper la referencia en el inspector.
    // El script crea su PROPIO Volume con prioridad alta en runtime.
    [SerializeField] private Volume globalVolume;
    [SerializeField] private int runtimeBrightnessVolumePriority = 100;

    [Header("Escena menu")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Comportamiento")]
    // Si esta en true pausa TODO el audio (musica, ambiente, etc). Default false: ambiente sigue.
    [SerializeField] private bool pauseAudioOnPause = false;
    // Si algun sistema necesita bloquear la pausa temporalmente, usar SetPauseAllowed
    [SerializeField] private bool pauseAllowed = true;

    private bool isPaused;
    private ColorAdjustments colorAdjustments;
    private bool hasColorAdjustments;

    public bool IsPaused => isPaused;

    // Eventos para que otros sistemas (ej: menu 3D) sepan cuando se abre/cierra el panel de ajustes.
    public event Action OnSettingsOpened;
    public event Action OnSettingsClosed;

    private void Awake()
    {
        // Debug.Log("[PauseMenu] Awake en GameObject: " + gameObject.name + " | activeInHierarchy: " + gameObject.activeInHierarchy);

        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<PlayerCamera>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        CacheColorAdjustments();
        EnablePostProcessingOnSceneCameras();
        WireSliderListeners();
        LoadAndApplyAllSettings();
        CloseAllPanels();

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }
    }

    private void Update()
    {
        // Click derecho = volver atras en la UI (ajustes / menu de pausa).
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleBackNavigation();
        }

        // En menuMode no escuchamos Tab ni hacemos nada relacionado a pausa.
        if (menuMode)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        // Tab o Escape hacen lo mismo: poner/sacar la pausa.
        bool pausePressed = Keyboard.current.tabKey.wasPressedThisFrame ||
                            Keyboard.current.escapeKey.wasPressedThisFrame;

        if (!pausePressed)
        {
            return;
        }

        if (!pauseAllowed && !isPaused)
        {
            // bloqueado: no se puede entrar a pausa pero si salir (por si quedo trabada)
            return;
        }

        if (isPaused)
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                ShowPausePanel();
            }
            else
            {
                uiAudioManager?.PlayBack();
                ResumeGame();
            }
        }
        else
        {
            PauseGame();
        }
    }

    // Click derecho: retrocede un nivel en la UI segun el contexto.
    private void HandleBackNavigation()
    {
        if (menuMode)
        {
            // Menu principal: si el panel de opciones esta abierto, lo cerramos.
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseSettings();
            }
            return;
        }

        // En juego: solo tiene sentido si estamos en pausa.
        if (!isPaused)
        {
            return;
        }

        // Si estamos en opciones volvemos al panel principal de pausa.
        // Si ya estamos en el panel principal, reanudamos el juego.
        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            ShowPausePanel();
        }
        else
        {
            ResumeGame();
        }
    }

    // Para usar desde el menu principal: el boton "Ajustes" llama aca.
    public void OpenSettings()
    {
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }

        RefreshOptionsUI();

        OnSettingsOpened?.Invoke();
    }

    // Boton "Volver" del panel de opciones cuando estamos en menu principal.
    public void CloseSettings()
    {

        uiAudioManager?.PlayBack();
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (menuMode && pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        OnSettingsClosed?.Invoke();
    }

    // API publica para que otros scripts bloqueen/desbloqueen la pausa
    public void SetPauseAllowed(bool allowed)
    {
        pauseAllowed = allowed;
    }

    // API publica para pausar/despausar desde otro script
    public void RequestPause()
    {
        if (!isPaused)
        {
            PauseGame();
        }
    }

    public void RequestResume()
    {
        if (isPaused)
        {
            ResumeGame();
        }
    }

    public void OnContinueClicked()
    {
        uiAudioManager?.PlaySelect();
        ResumeGame();
    }

    public void OnOptionsClicked()
    {

        uiAudioManager?.PlaySelect();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }

        // Refrescamos al abrir: el panel arranca desactivado y los sliders
        // no siempre toman el valor seteado en Awake hasta que se renderizan.
        RefreshOptionsUI();
    }

    public void OnOptionsBackClicked()
    {
        uiAudioManager?.PlayBack();
        ShowPausePanel();
    }

    public void OnBackToMenuClicked()
    {
        uiAudioManager?.PlaySelect();
        Time.timeScale = 1f;
      
        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void PauseGame()
    {
        uiAudioManager?.PlayBack();
        isPaused = true;
        Time.timeScale = 0f;
        
        if (audioMixer != null)
        {
            audioMixer.SetFloat(pauseLowPassParam, pausedLowpass);
        }
        /*
        if (pauseAudioOnPause)
        {
            AudioListener.pause = true;
        }
        */
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.OnGamePauseStart();
        }

        // Desactivo los components enteros: mas robusto que solo setear las flags
        // CantMove/CantMoveCamera pueden quedar mal si la referencia no es la correcta
        if (playerMovement != null)
        {
            playerMovement.CantMove(true);
            playerMovement.enabled = false;
        }

        if (playerCamera != null)
        {
            playerCamera._MoveCamera(false);
            playerCamera.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(true);
        }

        ShowPausePanel();
    }

    private void ResumeGame()
    {
        
        isPaused = false;
        Time.timeScale = 1f;
        if (audioMixer != null)
        {
            audioMixer.SetFloat(pauseLowPassParam, normalLowpass);
        }

        if (pauseAudioOnPause)
        {
            AudioListener.pause = false;
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.OnGamePauseEnd();
        }

        if (playerMovement != null)
        {
            playerMovement.CantMove(false);
            playerMovement.enabled = true;
        }

        if (playerCamera != null)
        {
            playerCamera._MoveCamera(false);
            playerCamera.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CloseAllPanels();

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }
    }

    private void ShowPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void CloseAllPanels()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void WireSliderListeners()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
        }
    }

    private void LoadAndApplyAllSettings()
    {
        // Leemos los valores actuales desde GameController (persistente via PlayerPrefs).
        ApplySensitivity(GetSensitivity());
        ApplyBrightness(GetBrightness());
        SetMixerVolume(masterVolumeParam, GetMasterVolume());
        SetMixerVolume(musicVolumeParam, GetMusicVolume());
        SetMixerVolume(sfxVolumeParam, GetSfxVolume());
        SetMixerVolume(ambientVolumeParam, GetAmbientVolume());

        if (audioMixer == null)
        {
            //Debug.LogWarning("[PauseMenu] AudioMixer no asignado. Los sliders de volumen no van a afectar nada hasta que lo asignes y rutes los AudioSources al mixer.");
        }

        RefreshOptionsUI();
    }

    // Sincroniza sliders + labels con los valores actuales del GameController.
    // Se llama al iniciar y CADA VEZ que se abre Options, para que los sliders
    // muestren los valores reales y no los que tenian cuando empezo la escena.
    private void RefreshOptionsUI()
    {
        SetSliderValueWithoutNotify(sensitivitySlider, Normalize(GetSensitivity(), sensitivityMin, sensitivityMax));
        SetSliderValueWithoutNotify(brightnessSlider, GetBrightness());
        SetSliderValueWithoutNotify(masterVolumeSlider, GetMasterVolume());
        SetSliderValueWithoutNotify(musicVolumeSlider, GetMusicVolume());
        SetSliderValueWithoutNotify(sfxVolumeSlider, GetSfxVolume());
        SetSliderValueWithoutNotify(ambientVolumeSlider, GetAmbientVolume());

        UpdateLabel(sensitivityValueLabel, FormatTwoDecimals(GetSensitivity()));
        UpdateLabel(brightnessValueLabel, FormatPercent(GetBrightness()));
        UpdateLabel(masterVolumeValueLabel, FormatPercent(GetMasterVolume()));
        UpdateLabel(musicVolumeValueLabel, FormatPercent(GetMusicVolume()));
        UpdateLabel(sfxVolumeValueLabel, FormatPercent(GetSfxVolume()));
        UpdateLabel(ambientVolumeValueLabel, FormatPercent(GetAmbientVolume()));
    }

    // En menuMode leemos/escribimos PlayerPrefs directo para que el menu principal
    // guarde ajustes aunque el singleton aun no este listo. En gameplay directo sin
    // GameController usamos defaults, asi testear una escena suelta arranca limpio.
    private float GetSensitivity() => GameController.Instance != null || menuMode ? GameController.GetSavedSensitivity() : GameController.DefaultSensitivity;
    private float GetBrightness() => GameController.Instance != null || menuMode ? GameController.GetSavedBrightness() : GameController.DefaultBrightness;
    private float GetMasterVolume() => GameController.Instance != null || menuMode ? GameController.GetSavedMasterVolume() : GameController.DefaultMasterVolume;
    private float GetMusicVolume() => GameController.Instance != null || menuMode ? GameController.GetSavedMusicVolume() : GameController.DefaultMusicVolume;
    private float GetSfxVolume() => GameController.Instance != null || menuMode ? GameController.GetSavedSfxVolume() : GameController.DefaultSfxVolume;
    private float GetAmbientVolume() => GameController.Instance != null || menuMode ? GameController.GetSavedAmbientVolume() : GameController.DefaultAmbientVolume;

    private void OnSensitivityChanged(float normalized)
    {
        float value = Mathf.Lerp(sensitivityMin, sensitivityMax, normalized);
        if (GameController.Instance != null || menuMode) GameController.SetSavedSensitivity(value);
        ApplySensitivity(value);
        UpdateLabel(sensitivityValueLabel, FormatTwoDecimals(value));
    }

    private void OnBrightnessChanged(float normalized)
    {
        if (verboseBrightnessLog)
        {
            //Debug.Log("[PauseMenu] OnBrightnessChanged disparado: slider=" + normalized.ToString("0.00"));
        }
        if (GameController.Instance != null || menuMode) GameController.SetSavedBrightness(normalized);
        ApplyBrightness(normalized);
        UpdateLabel(brightnessValueLabel, FormatPercent(normalized));
    }

    private void OnMasterVolumeChanged(float linear)
    {
        if (GameController.Instance != null || menuMode) GameController.SetSavedMasterVolume(linear);
        SetMixerVolume(masterVolumeParam, linear);
        UpdateLabel(masterVolumeValueLabel, FormatPercent(linear));
    }

    private void OnMusicVolumeChanged(float linear)
    {
        if (GameController.Instance != null || menuMode) GameController.SetSavedMusicVolume(linear);
        SetMixerVolume(musicVolumeParam, linear);
        UpdateLabel(musicVolumeValueLabel, FormatPercent(linear));
    }

    private void OnSfxVolumeChanged(float linear)
    {
        if (GameController.Instance != null || menuMode) GameController.SetSavedSfxVolume(linear);
        SetMixerVolume(sfxVolumeParam, linear);
        UpdateLabel(sfxVolumeValueLabel, FormatPercent(linear));
    }

    private void OnAmbientVolumeChanged(float linear)
    {
        if (GameController.Instance != null || menuMode) GameController.SetSavedAmbientVolume(linear);
        SetMixerVolume(ambientVolumeParam, linear);
        UpdateLabel(ambientVolumeValueLabel, FormatPercent(linear));
    }

    private static void UpdateLabel(TMP_Text label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }

    private static string FormatPercent(float normalized01)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(normalized01) * 100f);
        return percent + "%";
    }

    private static string FormatTwoDecimals(float value)
    {
        return value.ToString("0.00");
    }

    private void ApplySensitivity(float value)
    {
        if (playerCamera != null)
        {
            playerCamera.mouseSensitivity = value;
        }
    }

    // Verbose: loguea cada vez que se aplica brillo, para diagnosticar.
    // Cuando todo este funcionando, podes poner esto en false desde el inspector.
    [Header("Diagnostico")]
    [SerializeField] private bool verboseBrightnessLog = true;

    private void ApplyBrightness(float normalized01)
    {
        if (!hasColorAdjustments)
        {
            CacheColorAdjustments();

            if (!hasColorAdjustments)
            {
                if (verboseBrightnessLog)
                {
                    //Debug.LogWarning("[PauseMenu] ApplyBrightness sin colorAdjustments (no encontrado).");
                }
                return;
            }
        }

        float exposure = Mathf.Lerp(brightnessMinExposure, brightnessMaxExposure, normalized01);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.value = exposure;
        colorAdjustments.postExposure.overrideState = true;

        if (verboseBrightnessLog)
        {
            string volumeName = ownedBrightnessVolume != null ? ownedBrightnessVolume.name : "?";
            //Debug.Log("[PauseMenu] ApplyBrightness slider=" + normalized01.ToString("0.00") + " range=[" + brightnessMinExposure.ToString("0.00") + "," + brightnessMaxExposure.ToString("0.00") + "]" + " => postExposure=" + exposure.ToString("0.00") + " sobre Volume='" + volumeName + "'");
        }
    }

    private void SetMixerVolume(string parameterName, float linear01)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;
        audioMixer.SetFloat(parameterName, db);
    }

    private bool brightnessWarningLogged;
    private Volume ownedBrightnessVolume;
    private VolumeProfile ownedBrightnessProfile;

    private void CacheColorAdjustments()
    {
        if (hasColorAdjustments && colorAdjustments != null)
        {
            return;
        }

        // Creamos un Volume propio en runtime, con prioridad alta para que pise cualquier
        // otro Volume de la escena. Solo lleva un override de Color Adjustments con Post Exposure.
        GameObject volumeGO = new GameObject("PauseMenuBrightnessVolume");
        volumeGO.transform.SetParent(transform);

        ownedBrightnessVolume = volumeGO.AddComponent<Volume>();
        ownedBrightnessVolume.isGlobal = true;
        ownedBrightnessVolume.priority = runtimeBrightnessVolumePriority;
        ownedBrightnessVolume.weight = 1f;

        ownedBrightnessProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        ownedBrightnessProfile.name = "PauseMenuBrightnessProfile";
        ownedBrightnessVolume.sharedProfile = ownedBrightnessProfile;

        colorAdjustments = ownedBrightnessProfile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = 0f;

        hasColorAdjustments = true;

        //Debug.Log("[PauseMenu] Brightness Volume creado en runtime. priority=" + runtimeBrightnessVolumePriority);
    }

    private void EnablePostProcessingOnSceneCameras()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera sceneCamera in cameras)
        {
            UniversalAdditionalCameraData cameraData = sceneCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = true;
            }
        }
    }

    private void OnDestroy()
    {
        // Limpieza del Volume y profile que creamos para no dejar restos
        if (ownedBrightnessProfile != null)
        {
            Destroy(ownedBrightnessProfile);
        }

        if (ownedBrightnessVolume != null)
        {
            Destroy(ownedBrightnessVolume.gameObject);
        }
    }

    private void LogBrightnessWarning(string detail)
    {
        if (brightnessWarningLogged)
        {
            return;
        }

        brightnessWarningLogged = true;
        //Debug.LogWarning("[PauseMenu] Brillo no aplica: " + detail);
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
        }
    }

    private static float Normalize(float value, float min, float max)
    {
        if (Mathf.Approximately(max, min))
        {
            return 0f;
        }

        return Mathf.InverseLerp(min, max, value);
    }
}
