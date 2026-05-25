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
 * SETUP EN UNITY (no lo hace este script solo):
 * 1) AudioMixer "MainMixer" con grupos Music/SFX/Ambient y parametros expuestos:
 *    MasterVolume, MusicVolume, SFXVolume, AmbientVolume
 * 2) Asignar Output de cada AudioSource al grupo correcto del mixer.
 * 3) Volume (URP) en la camara con Color Adjustments > Post Exposure override.
 * 4) Invertir eje Y: requiere mini-cambio en PlayerCamera (ver comentario InvertYKey abajo).
 */
public class PauseMenuController : MonoBehaviour
{
    public const string InvertYKey = "settings_invert_y";
    // PlayerCamera debe leer este PlayerPrefs y aplicar en HandleMouseCam:
    // xRotation += (invertY ? mouseY : -mouseY);

    private const string PrefSensitivity = "settings_mouse_sensitivity";
    private const string PrefBrightness = "settings_brightness";
    private const string PrefMasterVol = "settings_master_volume";
    private const string PrefMusicVol = "settings_music_volume";
    private const string PrefSfxVol = "settings_sfx_volume";
    private const string PrefAmbientVol = "settings_ambient_volume";

    [Header("Paneles UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Sliders - Opciones")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;
    [SerializeField] private Toggle invertYToggle;

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
    [SerializeField] private bool pauseAudioOnPause = true;
    [SerializeField] private bool blockPauseDuringDialogue = true;

    private bool isPaused;
    private ColorAdjustments colorAdjustments;
    private bool hasColorAdjustments;

    private void Awake()
    {
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

        if (blockPauseDuringDialogue && IsDialoguePlaying())
        {
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

        if (playerMovement != null)
        {
            playerMovement.CantMove = true;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
            playerMovement.CantMove = false;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CloseAllPanels();
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

        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
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
        bool invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;

        SetSliderValueWithoutNotify(sensitivitySlider, Normalize(sensitivity, sensitivityMin, sensitivityMax));
        SetSliderValueWithoutNotify(brightnessSlider, brightness);
        SetSliderValueWithoutNotify(masterVolumeSlider, master);
        SetSliderValueWithoutNotify(musicVolumeSlider, music);
        SetSliderValueWithoutNotify(sfxVolumeSlider, sfx);
        SetSliderValueWithoutNotify(ambientVolumeSlider, ambient);

        if (invertYToggle != null)
        {
            invertYToggle.SetIsOnWithoutNotify(invertY);
        }

        ApplySensitivity(sensitivity);
        ApplyBrightness(brightness);
        SetMixerVolume(masterVolumeParam, master);
        SetMixerVolume(musicVolumeParam, music);
        SetMixerVolume(sfxVolumeParam, sfx);
        SetMixerVolume(ambientVolumeParam, ambient);
        PlayerPrefs.SetInt(InvertYKey, invertY ? 1 : 0);
    }

    private void OnSensitivityChanged(float normalized)
    {
        float value = Mathf.Lerp(sensitivityMin, sensitivityMax, normalized);
        PlayerPrefs.SetFloat(PrefSensitivity, value);
        ApplySensitivity(value);
    }

    private void OnBrightnessChanged(float normalized)
    {
        PlayerPrefs.SetFloat(PrefBrightness, normalized);
        ApplyBrightness(normalized);
    }

    private void OnMasterVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefMasterVol, linear);
        SetMixerVolume(masterVolumeParam, linear);
    }

    private void OnMusicVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefMusicVol, linear);
        SetMixerVolume(musicVolumeParam, linear);
    }

    private void OnSfxVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefSfxVol, linear);
        SetMixerVolume(sfxVolumeParam, linear);
    }

    private void OnAmbientVolumeChanged(float linear)
    {
        PlayerPrefs.SetFloat(PrefAmbientVol, linear);
        SetMixerVolume(ambientVolumeParam, linear);
    }

    private void OnInvertYChanged(bool value)
    {
        PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
        // Hasta autorizar el cambio en PlayerCamera, esto solo guarda la preferencia.
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
            return;
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

    private void CacheColorAdjustments()
    {
        hasColorAdjustments = false;

        if (globalVolume == null || globalVolume.profile == null)
        {
            return;
        }

        if (globalVolume.profile.TryGet(out colorAdjustments))
        {
            hasColorAdjustments = true;
        }
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

    private static bool IsDialoguePlaying()
    {
        return DialogueController.Instance != null && DialogueController.Instance.IsPlaying;
    }
}
