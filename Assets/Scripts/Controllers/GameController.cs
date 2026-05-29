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
    [SerializeField] private string playSceneName = "Play";

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

    private void Awake()
    {
        // singleton: si ya hay una instancia, esta se autodestruye
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    public void GoToScene(string sceneName)
    {
        TransitionToScene(sceneName);
    }

    private void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameController] TransitionToScene llamado con nombre vacio.");
            return;
        }

        // Limpiamos los flags que NO esten en la lista de persistentes.
        // Los persistentes solo se borran con metodos explicitos del GameStateController.
        if (gameStateController != null)
        {
            gameStateController.ClearFlagsExcept(persistentFlagIds);
        }

        // Por si veniamos en pausa, restauramos el timeScale.
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }

    // -------------- Configuraciones persistentes (PlayerPrefs) --------------

    public float Sensitivity
    {
        get => PlayerPrefs.GetFloat(PrefSensitivity, DefaultSensitivity);
        set => SaveFloat(PrefSensitivity, value);
    }

    public float Brightness
    {
        get => PlayerPrefs.GetFloat(PrefBrightness, DefaultBrightness);
        set => SaveFloat(PrefBrightness, value);
    }

    public float MasterVolume
    {
        get => PlayerPrefs.GetFloat(PrefMaster, DefaultMasterVolume);
        set => SaveFloat(PrefMaster, value);
    }

    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat(PrefMusic, DefaultMusicVolume);
        set => SaveFloat(PrefMusic, value);
    }

    public float SfxVolume
    {
        get => PlayerPrefs.GetFloat(PrefSfx, DefaultSfxVolume);
        set => SaveFloat(PrefSfx, value);
    }

    public float AmbientVolume
    {
        get => PlayerPrefs.GetFloat(PrefAmbient, DefaultAmbientVolume);
        set => SaveFloat(PrefAmbient, value);
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
}
