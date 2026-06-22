using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* Manager general del juego. Persiste entre escenas (DontDestroyOnLoad).
 *
 * Responsabilidades:
 *  - Cambiar de escena (Play/Menu/Intro) y limpiar flags no persistentes al hacerlo.
 *  - Guardar y exponer las configuraciones del jugador (volumen, brillo, sensibilidad)
 *    persistentes entre sesiones via PlayerPrefs.
 *
 * El PauseMenuController y cualquier otro menu de ajustes leen y escriben
 * a traves de GameController.Instance.
 */
public class GameController : MonoBehaviour
{
    public static GameController Instance;

    [Header("Referencias en el mismo GameObject")]
    [SerializeField] private GameStateController gameStateController;

    [Header("Nombres de escenas")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string introSceneName = "Intro";
    [SerializeField] private string playSceneName = "InGame";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Transicion de escena")]
    [SerializeField] private float sceneMusicFadeOut = 1f;

    [Header("Flags persistentes entre escenas")]
    // Ids de flags del GameStateController que NO se borran al cambiar de escena.
    // El resto se limpia automaticamente en cada transicion.
    [SerializeField] private List<string> persistentFlagIds = new List<string>();

    // Defaults para los settings. Si nunca se guardo, devuelve estos.
    public const float DefaultSensitivity = 0.4f;
    public const float DefaultBrightness = 0.5f;
    public const float DefaultMasterVolume = 0.5f;
    public const float DefaultMusicVolume = 0.5f;
    public const float DefaultSfxVolume = 0.5f;
    public const float DefaultAmbientVolume = 0.5f;

    private const string PrefSensitivity = "settings_mouse_sensitivity";
    private const string PrefBrightness = "settings_brightness";
    private const string PrefMaster = "settings_master_volume";
    private const string PrefMusic = "settings_music_volume";
    private const string PrefSfx = "settings_sfx_volume";
    private const string PrefAmbient = "settings_ambient_volume";

    private bool isTransitioning;

    private void Awake()
    {
        // singleton: si ya hay una instancia, esta se autodestruye
        if (Instance != null && Instance != this)
        {
            // No destruir el GameObject completo por si tiene componentes locales
            // de la escena. Solo quitamos este GameController duplicado.
            Destroy(this);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad solo funciona sobre objetos raiz. Si estamos como hijo
        // de algun "Manager" en la escena, nos despegamos antes de marcar persistente.
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        if (gameStateController == null)
        {
            gameStateController = GetComponent<GameStateController>();
        }
    }

    // -------------- Transicion de escenas --------------

    public void Play()
    {
        TransitionToScene(playSceneName);
    }

    public void Menu()
    {
        TransitionToScene(menuSceneName);
    }

    public void Intro()
    {
        TransitionToScene(introSceneName);
    }
    
    public void GameOver()
    {
        TransitionToScene(gameOverSceneName);
    }

    public void GoToScene(string sceneName)
    {
        TransitionToScene(sceneName);
    }

    private void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameController] TransitionToScene llamado con nombre vacio.");
            return;
        }

        StartCoroutine(TransitionToSceneRoutine(sceneName));
    }

    private IEnumerator TransitionToSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        yield return FadeOutSceneMusic();

        if (gameStateController != null)
        {
            gameStateController.ClearFlagsExcept(persistentFlagIds);
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);

        isTransitioning = false;
    }

    private IEnumerator FadeOutSceneMusic()
    {
        if (sceneMusicFadeOut <= 0f || MusicManager.Instance == null)
        {
            yield break;
        }

        MusicManager.Instance.Stop(sceneMusicFadeOut);
        yield return new WaitForSecondsRealtime(sceneMusicFadeOut);
    }

    // -------------- Configuraciones persistentes (PlayerPrefs) --------------

    public float Sensitivity
    {
        get => GetSavedSensitivity();
        set => SetSavedSensitivity(value);
    }

    public float Brightness
    {
        get => GetSavedBrightness();
        set => SetSavedBrightness(value);
    }

    public float MasterVolume
    {
        get => GetSavedMasterVolume();
        set => SetSavedMasterVolume(value);
    }

    public float MusicVolume
    {
        get => GetSavedMusicVolume();
        set => SetSavedMusicVolume(value);
    }

    public float SfxVolume
    {
        get => GetSavedSfxVolume();
        set => SetSavedSfxVolume(value);
    }

    public float AmbientVolume
    {
        get => GetSavedAmbientVolume();
        set => SetSavedAmbientVolume(value);
    }

    public void ResetSettingsToDefaults()
    {
        Sensitivity = DefaultSensitivity;
        Brightness = DefaultBrightness;
        MasterVolume = DefaultMasterVolume;
        MusicVolume = DefaultMusicVolume;
        SfxVolume = DefaultSfxVolume;
        AmbientVolume = DefaultAmbientVolume;
    }

    private static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static float GetSavedSensitivity() => PlayerPrefs.GetFloat(PrefSensitivity, DefaultSensitivity);
    public static float GetSavedBrightness() => PlayerPrefs.GetFloat(PrefBrightness, DefaultBrightness);
    public static float GetSavedMasterVolume() => PlayerPrefs.GetFloat(PrefMaster, DefaultMasterVolume);
    public static float GetSavedMusicVolume() => PlayerPrefs.GetFloat(PrefMusic, DefaultMusicVolume);
    public static float GetSavedSfxVolume() => PlayerPrefs.GetFloat(PrefSfx, DefaultSfxVolume);
    public static float GetSavedAmbientVolume() => PlayerPrefs.GetFloat(PrefAmbient, DefaultAmbientVolume);

    public static void SetSavedSensitivity(float value) => SaveFloat(PrefSensitivity, value);
    public static void SetSavedBrightness(float value) => SaveFloat(PrefBrightness, value);
    public static void SetSavedMasterVolume(float value) => SaveFloat(PrefMaster, value);
    public static void SetSavedMusicVolume(float value) => SaveFloat(PrefMusic, value);
    public static void SetSavedSfxVolume(float value) => SaveFloat(PrefSfx, value);
    public static void SetSavedAmbientVolume(float value) => SaveFloat(PrefAmbient, value);
}
