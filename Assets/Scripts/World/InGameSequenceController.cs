using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Flujo de la escena InGame.
 *
 * Arranca con un fade in lento, dispara un dialogo y, cuando ese dialogo termina,
 * espera unos segundos y recien ahi habilita las quests de la muneca junto con un
 * timer. Si el timer se acaba sin que el jugador haya escapado, aparece la Pursuer.
 *
 * Tambien lleva la cuenta de la felicidad: cada mision completada suma puntos y, si
 * se llega al tope, se abre la puerta de salida (condicion de victoria). Fallar una
 * mision resta felicidad (nunca baja de 0).
 *
 * Quest.cs avisa a este controlador cuando una mision se completa o falla.
 */
public class InGameSequenceController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMovement playerMovement;
    // La muneca debe arrancar con el DollEmotionSystem DESACTIVADO en el inspector:
    // este controlador lo prende cuando empiezan las quests.
    [SerializeField] private DollEmotionSystem dollEmotionSystem;
    [SerializeField] private PursuerSpawnController pursuerSpawn;
    // Puerta de salida. Se deja bloqueada por flag hasta llegar a la felicidad necesaria.
    [SerializeField] private DoorInteractable escapeDoor;
    [SerializeField] private string escapeDoorFlagName = "escape_door_unlocked";

    [Header("Fade in")]
    // Imagen negra que tapa la pantalla al arrancar y se desvanece.
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 3f;
    [SerializeField] private GameObject[] uiObjectsHiddenDuringIntro;

    [Header("Intro")]
    [SerializeField] private string introDialogueId = "ingame_intro";
    [SerializeField] private bool lockPlayerDuringIntro = false;
    // Espera entre que termina el dialogo y arrancan las quests.
    [SerializeField] private float delayBeforeQuests = 5f;

    [Header("Victoria (felicidad)")]
    [SerializeField] private int happinessPerMission = 30;
    // Cuanto se resta al fallar una mision (no baja de 0). Ajustable.
    [SerializeField] private int happinessLostOnFail = 30;
    [SerializeField] private int happinessToWin = 100;
    [SerializeField, TextArea] private string escapeMessage = "La muñeca esta feliz y te abrio la puerta. RAPIDO! ESCAPA!";
    [SerializeField] private float escapeMessageDuration = 6f;
    // Escena que se carga cuando el jugador cruza la puerta ya abierta (victoria).
    [SerializeField] private string winSceneName = "Win";

    [Header("Derrota (timer)")]
    // 10 minutos por defecto.
    [SerializeField] private float questPhaseDuration = 600f;
    // Opcional: texto para mostrar el tiempo restante. Si queda vacio no se usa.
    [SerializeField] private TMP_Text timerLabel;

    private int happiness;
    private bool questsStarted;
    private bool timerRunning;
    private bool hasEscaped;
    private bool pursuerSpawned;
    private bool winLoading;
    private float timeLeft;

    // true cuando el jugador ya llego a la felicidad de victoria y la puerta se abrio.
    public bool HasEscaped => hasEscaped;

    private void Awake()
    {
        PrepareFadeImage();
        SetIntroUiVisible(false);
        SetFlag(escapeDoorFlagName, false);

        // me aseguro de que la muneca no pida quests hasta que arranque la fase
        if (dollEmotionSystem != null)
        {
            dollEmotionSystem.enabled = false;
        }
    }

    private void Start()
    {
        if (lockPlayerDuringIntro && playerMovement != null)
        {
            playerMovement.CantMove(true);
        }

        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (!timerRunning || hasEscaped)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        UpdateTimerLabel();

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            SpawnPursuer();
        }
    }

    private IEnumerator IntroRoutine()
    {
        yield return StartCoroutine(FadeIn());

        float dialogueDuration = 0f;

        if (DialogueController.Instance != null && !string.IsNullOrEmpty(introDialogueId))
        {
            dialogueDuration = DialogueController.Instance.GetDialogueDuration(introDialogueId);
            DialogueController.Instance.PlayDialogue(introDialogueId);
        }

        yield return new WaitForSeconds(dialogueDuration);
        yield return new WaitForSeconds(delayBeforeQuests);

        StartQuests();
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        float elapsed = 0f;

        color.a = 1f;
        fadeImage.color = color;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = fadeInDuration <= 0f ? 0f : 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }

    private void StartQuests()
    {
        if (questsStarted)
        {
            return;
        }

        questsStarted = true;

        if (lockPlayerDuringIntro && playerMovement != null)
        {
            playerMovement.CantMove(false);
        }

        if (dollEmotionSystem != null)
        {
            dollEmotionSystem.enabled = true;
        }

        SetIntroUiVisible(true);

        timeLeft = questPhaseDuration;
        timerRunning = true;
        UpdateTimerLabel();
    }

    // La llama Quest al completar una mision.
    public void OnMissionCompleted()
    {
        if (hasEscaped)
        {
            return;
        }

        happiness = Mathf.Clamp(happiness + happinessPerMission, 0, happinessToWin);

        if (happiness >= happinessToWin)
        {
            TriggerEscape();
        }
    }

    // La llama Quest al fallar una mision.
    public void OnMissionFailed()
    {
        if (hasEscaped)
        {
            return;
        }

        happiness = Mathf.Max(0, happiness - happinessLostOnFail);
    }

    private void TriggerEscape()
    {
        if (hasEscaped)
        {
            return;
        }

        hasEscaped = true;
        timerRunning = false;

        SetFlag(escapeDoorFlagName, true);

        if (escapeDoor != null)
        {
            escapeDoor.OpenFromAI();
        }

        if (SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(escapeMessage, escapeMessageDuration, SubtitlePriority.Critical);
        }

        // la muneca ya cumplio, no sigue pidiendo quests
        if (dollEmotionSystem != null)
        {
            dollEmotionSystem.enabled = false;
        }
    }

    // La llama el trigger de la puerta cuando el jugador cruza despues de ganar.
    public void NotifyReachedExit()
    {
        if (!hasEscaped || winLoading)
        {
            return;
        }

        winLoading = true;
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        yield return StartCoroutine(FadeOut());

        if (!string.IsNullOrWhiteSpace(winSceneName))
        {
            SceneManager.LoadScene(winSceneName);
        }
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    private void SpawnPursuer()
    {
        if (pursuerSpawned || hasEscaped)
        {
            return;
        }

        pursuerSpawned = true;

        if (pursuerSpawn != null)
        {
            pursuerSpawn.StartSpawnSequence();
        }
        else
        {
            Debug.LogWarning("InGameSequenceController: no hay PursuerSpawnController asignado, no aparece la Pursuer.");
        }
    }

    private void UpdateTimerLabel()
    {
        if (timerLabel == null)
        {
            return;
        }

        int total = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timerLabel.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private void PrepareFadeImage()
    {
        if (fadeImage == null)
        {
            return;
        }

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;
    }

    private void SetIntroUiVisible(bool visible)
    {
        if (uiObjectsHiddenDuringIntro == null)
        {
            return;
        }

        foreach (GameObject uiObject in uiObjectsHiddenDuringIntro)
        {
            if (uiObject != null)
            {
                uiObject.SetActive(visible);
            }
        }
    }

    private void SetFlag(string flagName, bool value)
    {
        if (GameStateController.Instance != null && !string.IsNullOrEmpty(flagName))
        {
            GameStateController.Instance.SetFlag(flagName, value);
        }
    }
}
