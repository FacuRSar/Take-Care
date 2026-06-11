using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Flujo de la escena InGame.
 *
 * Arranca con un fade in lento, dispara un dialogo y, cuando ese dialogo termina,
 * espera unos segundos y recien ahi habilita las quests de la muneca junto con un
 * timer. Si el timer se acaba sin que el jugador haya escapado, aparece la Pursuer.
 *
 * Tambien lleva un parametro de "complete": cada mision completada suma puntos segun su
 * dificultad y, al llegar a 100, se abre la puerta de salida (condicion de victoria).
 * Fallar una mision no suma nada (no resta tampoco).
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
    // Puerta de salida. Se deja bloqueada por flag hasta llegar al progreso necesario.
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
    // Espera entre que termina el dialogo y que la muneca queda lista para activarse al acercarte.
    [SerializeField] private float delayBeforeQuests = 0f;
    // Flag que se prende cuando termina la intro. Util para enganchar HintDialogue.
    [SerializeField] private string introFinishedFlag = "intro_finished";

    [Header("Activacion por cercania a la muneca")]
    // Cuando termino la intro y el jugador entra en este radio de la muneca, arrancan las quests.
    [SerializeField] private float dollApproachDistance = 3f;
    // Flag que se prende al arrancar las quests. Util para enganchar HintDialogue.
    [SerializeField] private string questsStartedFlag = "quests_started";

    [Header("Victoria (progreso de escape)")]
    // El parametro "complete" arranca en 0 y cada mision suma sus completePoints (por dificultad).
    // Al llegar a este tope se abre la puerta. Pensado para necesitar 3 o 4 misiones.
    [SerializeField] private int completeToWin = 100;
    [SerializeField, TextArea] private string escapeMessage = "La muñeca esta feliz y te abrio la puerta. RAPIDO! ESCAPA!";
    [SerializeField] private float escapeMessageDuration = 6f;
    // Escena que se carga cuando el jugador cruza la puerta ya abierta (victoria).
    [SerializeField] private string winSceneName = "Win";

    [Header("Derrota (timer)")]
    // 10 minutos por defecto.
    [SerializeField] private float questPhaseDuration = 600f;
    // Segundos que pasan desde que termina la intro hasta que arranca el timer global.
    // El timer corre aunque el jugador todavia no se haya acercado a la muneca (mete presion).
    [SerializeField] private float timerStartDelayAfterIntro = 10f;
    // Opcional: texto para mostrar el tiempo restante. Si queda vacio no se usa.
    [SerializeField] private TMP_Text timerLabel;

    [Header("Debug (solo para testear, apagar en la entrega)")]
    [SerializeField] private bool debugKeys = false;
    // Tecla que fuerza la victoria: llena la felicidad y dispara el escape.
    [SerializeField] private Key debugWinKey = Key.F1;
    // Tecla que pone el timer en 0 y spawnea la Pursuer.
    [SerializeField] private Key debugPursuerKey = Key.F2;

    private int completeProgress;
    private bool introFinished;
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
        if (debugKeys)
        {
            HandleDebugKeys();
        }

        // ya termino la intro pero todavia no arrancaron las quests: espero que el jugador
        // se acerque a la muneca para disparar todo.
        if (introFinished && !questsStarted)
        {
            CheckDollApproach();
        }

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

        // la intro termino: a partir de aca, acercarse a la muneca dispara las quests.
        introFinished = true;
        SetFlag(introFinishedFlag, true);
        SetIntroUiVisible(true);

        // el timer global arranca solo, unos segundos despues de la intro.
        StartCoroutine(StartTimerAfterDelay());
    }

    private IEnumerator StartTimerAfterDelay()
    {
        yield return new WaitForSeconds(timerStartDelayAfterIntro);
        StartGlobalTimer();
    }

    private void StartGlobalTimer()
    {
        if (timerRunning || hasEscaped)
        {
            return;
        }

        timeLeft = questPhaseDuration;
        timerRunning = true;
        UpdateTimerLabel();
    }

    private void CheckDollApproach()
    {
        if (dollEmotionSystem == null || playerMovement == null)
        {
            return;
        }

        // Uso el transform real de la muneca (no el del objeto que tiene el script,
        // que esta corrido del modelo visible).
        Transform dollTransform = dollEmotionSystem.Doll != null ? dollEmotionSystem.Doll : dollEmotionSystem.transform;

        float distance = Vector3.Distance(playerMovement.transform.position, dollTransform.position);

        if (distance <= dollApproachDistance)
        {
            StartQuests();
        }
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
            // arranca la primera quest sin esperar el idle largo de la muneca
            dollEmotionSystem.ForceStartQuest();
        }

        SetFlag(questsStartedFlag, true);
    }

    // La llama Quest al completar una mision, con los puntos de "complete" de esa mision.
    public void OnMissionCompleted(int completePoints)
    {
        if (hasEscaped)
        {
            return;
        }

        completeProgress += Mathf.Max(0, completePoints);
        Debug.Log($"InGameSequenceController: complete = {completeProgress}/{completeToWin}");

        if (completeProgress >= completeToWin)
        {
            TriggerEscape();
        }
    }

    // La llama Quest al fallar una mision. Fallar no da complete (ni resta).
    public void OnMissionFailed()
    {
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

    private void HandleDebugKeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[debugWinKey].wasPressedThisFrame)
        {
            Debug.Log("[DEBUG] Forzando victoria: progreso de escape al maximo.");
            completeProgress = completeToWin;
            TriggerEscape();
        }

        if (Keyboard.current[debugPursuerKey].wasPressedThisFrame)
        {
            Debug.Log("[DEBUG] Forzando timer en 0 y spawn de la Pursuer.");
            timeLeft = 0f;
            timerRunning = false;
            SpawnPursuer();
        }
    }
}
