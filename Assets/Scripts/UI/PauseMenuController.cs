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
    private const string PrefSensitivity = "settings_mouse_sensitivity";
    private const string PrefBrightness = "settings_brightness";
    private const string PrefMasterVol = "settings_master_volume";
    private const string PrefMusicVol = "settings_music_volume";
    private const string PrefSfxVol = "settings_sfx_volume";
    private const string PrefAmbientVol = "settings_ambient_volume";

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
    [SerializeField] private float defaultSensitivity = 0.4f;
    [SerializeField] private float brightnessMinExposure = -1.5f;
    [SerializeField] private float brightnessMaxExposure = 1.5f;

    [Header("Referencias de juego")]
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Audio (AudioMixer)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string ambientVolumeParam = "AmbientVolume";

    [Header("Brillo (URP Post Exposure)")]
    [SerializeField] private Volume globalVolume;

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
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.tabKey.wasPressedThisFrame)
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
                ResumeGame();
            }
        }
        else
        {
            PauseGame();
        }
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
        ResumeGame();
    }

    public void OnOptionsClicked()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void OnOptionsBackClicked()
    {
        ShowPausePanel();
    }

    public void OnBackToMenuClicked()
    {
        Time.timeScale = 1f;

        if (pauseAudioOnPause)
        {
            AudioListener.pause = false;
        }

        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseAudioOnPause)
        {
            AudioListener.pause = true;
        }

        // Desactivo los components enteros: mas robusto que solo setear las flags
        // CantMove/CantMoveCamera pueden quedar mal si la referencia no es la correcta
        if (playerMovement != null)
        {
            playerMovement.CantMove = true;
            playerMovement.enabled = false;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = true;
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

        if (pauseAudioOnPause)
        {
            AudioListener.pause = false;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.CantMove = false;
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.CantMoveCamera = false;
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
        float sensitivity = PlayerPrefs.GetFloat(PrefSensitivity, defaultSensitivity);
        float brightness = PlayerPrefs.GetFloat(PrefBrightness, 0.5f);
        float master = PlayerPrefs.GetFloat(PrefMasterVol, 0.75f);
        float music = PlayerPrefs.GetFloat(PrefMusicVol, 0.75f);
        float sfx = PlayerPrefs.GetFloat(PrefSfxVol, 1f);
        float ambient = PlayerPrefs.GetFloat(PrefAmbientVol, 1f);

        SetSliderValueWithoutNotify(sensitivitySlider, Normalize(sensitivity, sensitivityMin, sensitivityMax));
        SetSliderValueWithoutNotify(brightnessSlider, brightness);
        SetSliderValueWithoutNotify(masterVolumeSlider, master);
        SetSliderValueWithoutNotify(musicVolumeSlider, music);
        SetSliderValueWithoutNotify(sfxVolumeSlider, sfx);
        SetSliderValueWithoutNotify(ambientVolumeSlider, ambient);

        ApplySensitivity(sensitivity);
        ApplyBrightness(brightness);
        SetMixerVolume(masterVolumeParam, master);
        SetMixerVolume(musicVolumeParam, music);
        SetMixerVolume(sfxVolumeParam, sfx);
        SetMixerVolume(ambientVolumeParam, ambient);

        UpdateLabel(sensitivityValueLabel, FormatTwoDecimals(sensitivity));
        UpdateLabel(brightnessValueLabel, FormatPercent(brightness));
        UpdateLabel(masterVolumeValueLabel, FormatPercent(master));
        UpdateLabel(musicVolumeValueLabel, FormatPercent(music));
        UpdateLabel(sfxVolumeValueLabel, FormatPercent(sfx));
        UpdateLabel(ambientVolumeValueLabel, FormatPercent(ambient));
    }

    private void OnSensitivityChanged(float normalized)
    {
        float value = Mathf.Lerp(sensitivityMin, sensitivityMax, normalized);
        PlayerPrefs.SetFloat(PrefSensitivity, value);
        ApplySensitivity(value);
        UpdateLabel(sensitivityValueLabel, FormatTwoDecimals(value));
    }

    private void OnBrightnessChanged(float normalized)
    {
        PlayerPrefs.SetFloat(PrefBrightness, normalized);
        ApplyBrightness(normalized);
        UpdateLabel(brightnessValueLabel, FormatPercent(normalized));
    }

    private void OnMasterVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefMasterVol, linear);
        SetMixerVolume(masterVolumeParam, linear);
        UpdateLabel(masterVolumeValueLabel, FormatPercent(linear));
    }

    private void OnMusicVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefMusicVol, linear);
        SetMixerVolume(musicVolumeParam, linear);
        UpdateLabel(musicVolumeValueLabel, FormatPercent(linear));
    }

    private void OnSfxVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefSfxVol, linear);
        SetMixerVolume(sfxVolumeParam, linear);
        UpdateLabel(sfxVolumeValueLabel, FormatPercent(linear));
    }

    private void OnAmbientVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefAmbientVol, linear);
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

    private void ApplyBrightness(float normalized01)
    {
        if (!hasColorAdjustments)
        {
            // segundo intento por si el Volume se inicializo despues que el script
            CacheColorAdjustments();

            if (!hasColorAdjustments)
            {
                return;
            }
        }

        float exposure = Mathf.Lerp(brightnessMinExposure, brightnessMaxExposure, normalized01);
        colorAdjustments.postExposure.Override(exposure);
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

    private void CacheColorAdjustments()
    {
        hasColorAdjustments = false;

        if (globalVolume == null)
        {
            LogBrightnessWarning("globalVolume no asignado en el inspector del PauseMenuController.");
            return;
        }

        VolumeProfile profile = globalVolume.profile;

        if (profile == null)
        {
            LogBrightnessWarning("El Volume no tiene Profile asignado.");
            return;
        }

        if (profile.TryGet(out colorAdjustments))
        {
            hasColorAdjustments = true;
        }
        else
        {
            LogBrightnessWarning("El Volume Profile no tiene override de Color Adjustments. Agregalo en el inspector del profile.");
        }
    }

    private void LogBrightnessWarning(string detail)
    {
        if (brightnessWarningLogged)
        {
            return;
        }

        brightnessWarningLogged = true;
        Debug.LogWarning("[PauseMenu] Brillo no aplica: " + detail);
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
