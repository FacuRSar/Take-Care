using System.Collections;
using UnityEngine;

/* Orquesta la pantalla final (GameOver / Win):
 * lluvia y truenos bajos al entrar, cancion una sola vez, y al terminar sube el ambiente.
 */
public class EndScreenController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AmbientManager ambientManager;
    [SerializeField] private MusicManager musicManager;

    [Header("Lluvia / truenos (fase inicial)")]
    [SerializeField] private float initialAmbientScale = 0.18f;
    [SerializeField] private float initialThunderScale = 0.3f;
    [SerializeField, Range(0f, 1f)] private float initialThunderChance = 0.35f;
    [SerializeField] private Vector2 initialThunderDelayRange = new Vector2(10f, 22f);
    [SerializeField] private float ambientInitialFade = 2f;

    [Header("Lluvia / truenos (despues de la cancion)")]
    [SerializeField] private float boostedAmbientScale = 0.5f;
    [SerializeField] private float boostedThunderScale = 0.65f;
    [SerializeField, Range(0f, 1f)] private float boostedThunderChance = 0.55f;
    [SerializeField] private Vector2 boostedThunderDelayRange = new Vector2(6f, 16f);
    [SerializeField] private float ambientBoostFade = 2.5f;

    [Header("Cancion final")]
    [SerializeField] private string endMusicId = "musicBox";
    [SerializeField] private float musicFadeIn = 1.5f;

    private void Awake()
    {
        ReleaseCursor();

        if (ambientManager == null)
        {
            ambientManager = FindFirstObjectByType<AmbientManager>();
        }

        if (musicManager == null)
        {
            musicManager = FindFirstObjectByType<MusicManager>();
        }
    }

    private void Start()
    {
        StartCoroutine(EndSequence());
    }

    private IEnumerator EndSequence()
    {
        float userAmbient = GameController.Instance != null
            ? GameController.Instance.AmbientVolume
            : GameController.DefaultAmbientVolume;

        float userMusic = GameController.Instance != null
            ? GameController.Instance.MusicVolume
            : GameController.DefaultMusicVolume;

        if (musicManager != null)
        {
            musicManager.SetVolume(userMusic);
            musicManager.Stop(0f);
        }

        if (ambientManager != null)
        {
            ambientManager.SetThunderActivity(initialThunderDelayRange, initialThunderChance);
            ambientManager.SetVolumeScales(0f, 0f, 0f);
            ambientManager.StartAmbient();
            ambientManager.StartThunderLoop();
            ambientManager.SetVolumeScales(
                initialAmbientScale * userAmbient,
                initialThunderScale * userAmbient,
                ambientInitialFade);
        }

        if (musicManager != null && !string.IsNullOrEmpty(endMusicId))
        {
            bool musicFinished = false;
            musicManager.PlayOnce(endMusicId, musicFadeIn, () => musicFinished = true);

            while (!musicFinished)
            {
                yield return null;
            }
        }

        if (ambientManager != null)
        {
            ambientManager.SetThunderActivity(boostedThunderDelayRange, boostedThunderChance);
            ambientManager.SetVolumeScales(
                boostedAmbientScale * userAmbient,
                boostedThunderScale * userAmbient,
                ambientBoostFade);
        }
    }

    public void Replay()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.Intro();
        }
    }

    public void GoToMenu()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.Menu();
        }
    }

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
