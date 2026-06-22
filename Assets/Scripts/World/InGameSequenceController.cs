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

    [Header("Activacion por flags (las prenden eventos)")]
    // Cuando esta flag se prende, arrancan las quests de la muneca.
    // La idea es setearla con un evento de cercania (AmbientEvent PlayerEnter cerca de la muneca).
    [SerializeField] private string startQuestsFlag = "doll_quests_start";
    // Cuando esta flag se prende, arranca el timer global.
    // Por ahora se setea con un evento de tiempo (AmbientEvent Timed), pero puede ser cualquier evento.
    [SerializeField] private string startTimerFlag = "game_timer_start";
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

    [Header("Timer global")]
    [Tooltip("Duracion total del timer en segundos. 600 = 10 minutos.")]
    [SerializeField, Min(1f)] private float questPhaseDuration = 600f;
    [SerializeField] private ClockRadialUI clockUI;
    // Opcional: texto MM:SS. Si queda vacio no se usa (podes usar solo el reloj).
    [SerializeField] private TMP_Text timerLabel;

    [Header("Flags del timer (25% / 50% / 75% / fin)")]
    [SerializeField] private string timer25Flag = "game_timer_25";
    [SerializeField] private string timer50Flag = "game_timer_50";
    [SerializeField] private string timer75Flag = "game_timer_75";
    [SerializeField] private string timerCompleteFlag = "game_timer_complete";

    [Header("Debug (solo para testear, apagar en la entrega)")]
    [SerializeField] private bool debugKeys = false;
    // Tecla que fuerza la victoria: llena la felicidad y dispara el escape.
    [SerializeField] private Key debugWinKey = Key.F1;
    // Tecla que pone el timer en 0 y spawnea la Pursuer.
    [SerializeField] private Key debugPursuerKey = Key.F2;

    private int completeProgress;
    private bool questsStarted;
    private bool timerRunning;
    private bool hasEscaped;
    private bool pursuerSpawned;
    private bool winLoading;
    private float timeLeft;
    private bool milestone25Fired;
    private bool milestone50Fired;
    private bool milestone75Fired;
    private bool milestone100Fired;

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

        if (clockUI != null)
        {
            clockUI.SetFull();
        }
    }

    private void OnEnable()
    {
        GameStateController.OnFlagChanged += HandleFlagChanged;
    }

    private void OnDisable()
    {
        GameStateController.OnFlagChanged -= HandleFlagChanged;
    }

    private void Start()
    {
        if (lockPlayerDuringIntro && playerMovement != null)
        {
            playerMovement.CantMove(true);
        }

        StartCoroutine(IntroRoutine());

        // por si alguna flag ya estaba puesta antes de arrancar
        if (IsFlagOn(startQuestsFlag))
        {
            StartQuests();
        }

        if (IsFlagOn(startTimerFlag))
        {
            StartGlobalTimer();
        }
    }

    // Las quests y el timer ahora los disparan flags (que prenden eventos).
    private void HandleFlagChanged(string flagName, bool value)
    {
        if (!value)
        {
            return;
        }

        if (!string.IsNullOrEmpty(startQuestsFlag) && flagName == startQuestsFlag)
        {
            StartQuests();
        }

        if (!string.IsNullOrEmpty(startTimerFlag) && flagName == startTimerFlag)
        {
            StartGlobalTimer();
        }
    }

    private bool IsFlagOn(string flagName)
    {
        return GameStateController.Instance != null &&
               !string.IsNullOrEmpty(flagName) &&
               GameStateController.Instance.GetFlag(flagName);
    }

    private void Update()
    {
        if (debugKeys)
        {
            HandleDebugKeys();
        }

        if (!timerRunning || hasEscaped)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        UpdateTimerDisplay();
        CheckTimerMilestones();

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            UpdateTimerDisplay();
            FireTimerCompleteFlag();
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

        // la intro termino. El timer y las quests ahora los disparan flags (las prenden eventos).
        SetFlag(introFinishedFlag, true);
        SetIntroUiVisible(true);
    }

    private void StartGlobalTimer()
    {
        if (timerRunning || hasEscaped)
        {
            return;
        }

        timeLeft = questPhaseDuration;
        timerRunning = true;
        ResetTimerMilestones();
        UpdateTimerDisplay();
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
        //Debug.Log($"InGameSequenceController: complete = {completeProgress}/{completeToWin}");

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
            //Debug.LogWarning("InGameSequenceController: no hay PursuerSpawnController asignado, no aparece la Pursuer.");
        }
    }

    private void UpdateTimerDisplay()
    {
        float elapsed = questPhaseDuration - timeLeft;

        if (clockUI != null)
        {
            clockUI.UpdateClock(elapsed, questPhaseDuration);
        }

        if (timerLabel == null)
        {
            return;
        }

        int total = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timerLabel.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private void ResetTimerMilestones()
    {
        milestone25Fired = false;
        milestone50Fired = false;
        milestone75Fired = false;
        milestone100Fired = false;
    }

    private void CheckTimerMilestones()
    {
        if (questPhaseDuration <= 0f)
        {
            return;
        }

        float elapsed = questPhaseDuration - timeLeft;
        float percent = elapsed / questPhaseDuration;

        if (!milestone25Fired && percent >= 0.25f)
        {
            milestone25Fired = true;
            SetFlag(timer25Flag, true);
        }

        if (!milestone50Fired && percent >= 0.50f)
        {
            milestone50Fired = true;
            SetFlag(timer50Flag, true);
        }

        if (!milestone75Fired && percent >= 0.75f)
        {
            milestone75Fired = true;
            SetFlag(timer75Flag, true);
        }
    }

    private void FireTimerCompleteFlag()
    {
        if (milestone100Fired)
        {
            return;
        }

        milestone100Fired = true;
        SetFlag(timerCompleteFlag, true);
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
            //Debug.Log("[DEBUG] Forzando victoria: progreso de escape al maximo.");
            completeProgress = completeToWin;
            TriggerEscape();
        }

        if (Keyboard.current[debugPursuerKey].wasPressedThisFrame)
        {
            //Debug.Log("[DEBUG] Forzando timer en 0 y spawn de la Pursuer.");
            timeLeft = 0f;
            timerRunning = false;
            UpdateTimerDisplay();
            FireTimerCompleteFlag();
            SpawnPursuer();
        }
    }
}
