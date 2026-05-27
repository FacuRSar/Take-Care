using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequenceController : MonoBehaviour
{
    // IDs para poner los dialogos en la pool
    private const string IntroDialogueId = "intro";
    private const string energyReactionDialogueId = "energy_restored";
    private const string PhoneHelloDialogueId = "phone_hello";
    private const string PhoneSurpriseDialogueId = "phone_surprise";
    private const string DollReactionDialogueId = "doll_reaction";
    private const string BathroomReactionDialogueId = "bathroom_reaction";
    private const string MirrorReactionDialogueId = "mirror_reaction";

    [Header("Referencias")]
    [SerializeField] private PhoneInteractable phoneInteractable;
    [SerializeField] private FixedCameraWithZoom focusCamera;

    [Tooltip("Indices en focusCamera.sequences: 0 = energia, 1 = telefono, etc. Ajustalos al orden que armen en el controlador.")]
    [SerializeField] private int energyFocusSequenceIndex;
    [SerializeField] private int phoneFocusSequenceIndex = 1;
    [SerializeField] private int dollFocusSequenceIndex = 2;
    [SerializeField] private int bathroomFocusSequenceIndex = 3;

    [Header("Zoom personalizado de focus")]
    [SerializeField] private bool useCustomEnergyZoom = false;
    [SerializeField] private float energyCustomZoomFov = 35f;

    [SerializeField] private bool useCustomPhoneZoom = false;
    [SerializeField] private float phoneCustomZoomFov = 30f;

    [SerializeField] private bool useCustomDollZoom = false;
    [SerializeField] private float dollCustomZoomFov = 25f;

    [SerializeField] private bool useCustomBathroomZoom = false;
    [SerializeField] private float bathroomCustomZoomFov = 28f;

    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private SFXManager sfxManager;
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private DemoEndController demoEndController;

    [Header("Persecucion")]
    [SerializeField] private PursuerSpawnController pursuerSpawnController;
    // Reemplaza al MirrorChaseEpilogue viejo. Se dispara al terminar el focus del ba�o.

    [Header("Escape / Shock")]
    [SerializeField] private string agitatedBreathingLoopId = "AgitatedBreathing";
    [SerializeField] private GameObject playerHeadBobTarget;
    [SerializeField] private string headBobEscapeMessage = "ApplyEscapeConfig";
    [SerializeField] private Image mentalFatigueImage;
    [SerializeField] private float mentalFatigueFadeDuration = 45f;
    [SerializeField] private float mentalFatigueTargetAlpha = 0.55f;
    /* Esto deja preparado el estado mental del jugador para la persecucion:
     * respiracion agitada en loop
     * cambio opcional del headbob usando SendMessage
     * cierre lento de pantalla con una Image negra/vignette
     */

    [Header("Mensaje del espejo (objetos en escena)")]
    [SerializeField] private GameObject[] mirrorMessageObjects;
    [SerializeField] private bool mirrorMessageHideOnStart = true;

    [Header("Objetos de la escena")]
    [SerializeField] private GameObject dollObject;
    [SerializeField] private Transform bathroomSoundPoint;

    [Header("Dialogos de intro")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private bool startLightHintsOnStart = true;
    [SerializeField] private float introDialogueStartDelay = 0f;

    [Header("SFX opcionales")]
    [SerializeField] private string dollAppearExtraSfxId = "";

    [Space(10)] // Para separar los focus
    [SerializeField] private float dollLaughDelay = 0f;
    [SerializeField] private float energyReactionDialogueDelay = 0f;
    [SerializeField] private float phoneHelloDialogueDelay = 0f;
    [SerializeField] private float dollReactionDialogueDelay = 0f;
    [SerializeField] private float bathroomReactionDialogueDelay = 0f;
    [SerializeField] private float mirrorReactionDialogueDelay = 0f;

    [Space(10)] // Para separar los DialogueDelays
    [SerializeField] private float PhoneRingDelay = 2f;
    [SerializeField] private float dollBreakDelay = 2f;
    [SerializeField] private float bathroomReactionDelay = 1f;
    [SerializeField] private float mirrorReactionDelay = 2f;

    private bool energyRestored;
    private bool phoneAnswered;
    private bool dollTriggered;
    private bool mirrorTriggered;
    private bool faucetClosed;
    private bool escapeAttempted;
    private Coroutine mentalFatigueRoutine;

    private void Awake()
    {
        // agarro singletons si no los cargamos a mano en inspector
        // esto para teo que no le gusta mover al inspector
        if (gameStateController == null)
        {
            gameStateController = GameStateController.Instance;
        }

        if (sfxManager == null)
        {
            sfxManager = SFXManager.Instance;
        }
    }

    private void Start()
    {
        if (phoneInteractable != null)
        {
            phoneInteractable.SetCanAnswer(false);
        }

        if (dollObject != null)
        {
            dollObject.SetActive(false);
        }

        if (mirrorMessageHideOnStart)
        {
            SetMirrorMessageObjectsActive(false);
        }

        if (mentalFatigueImage != null)
        {
            Color color = mentalFatigueImage.color;
            color.a = 0f;
            mentalFatigueImage.color = color;
            mentalFatigueImage.gameObject.SetActive(true);
        }

        if (playIntroOnStart)
        {
            StartCoroutine(PlayDialogueAfterDelay(IntroDialogueId, introDialogueStartDelay));
        }

        if (startLightHintsOnStart && HintDialogueController.Instance != null)
        {
            HintDialogueController.Instance.StartHints();
        }
    }

    public void OnEnergyRestored()
    {
        if (energyRestored)
        {
            return;
        }

        energyRestored = true;
        // Debug.Log("Energia restaurada, vamo arriba.");

        SetFlag("power_on", true);
        SetFlag("energy_restored", true);

        if (HintDialogueController.Instance != null)
        {
            HintDialogueController.Instance.StopHints();
        }

        PlayFocusSequence(energyFocusSequenceIndex, useCustomEnergyZoom, energyCustomZoomFov);
        StartCoroutine(EnablePhoneAfterFocus());
    }

    public void OnPhoneAnswered()
    {
        if (phoneAnswered)
        {
            return;
        }

        phoneAnswered = true;
        // Debug.Log("Telefono contestado.");

        SetFlag("phone_answered", true);
        StopLoop("PhoneRing");

        StartCoroutine(PhoneAnswerRoutine());
    }

    private IEnumerator PhoneAnswerRoutine()
    {
        // orden del telefono: miro el telefono, suena la estatica, digo hola, y recien despues cae la risa
        // si lo hacemos todo de una queda medio chueco, lo se porque tuve que meter esto porque hacia eso jajajj
        float phoneDialogueWait = phoneHelloDialogueDelay + GetDialogueDuration(PhoneHelloDialogueId);
        float phoneTotalWait = Mathf.Max(0f, phoneDialogueWait);

        PlayFocusSequence(phoneFocusSequenceIndex, useCustomPhoneZoom, phoneCustomZoomFov);
        PlayLoop2D("PhoneStatic");
        StartCoroutine(PlayDialogueAfterDelay(PhoneHelloDialogueId, phoneHelloDialogueDelay));

        yield return new WaitForSeconds(phoneTotalWait);

        StopLoop("PhoneStatic");

        if (dollLaughDelay > 0f)
        {
            yield return new WaitForSeconds(dollLaughDelay);
        }

        Play2D("DollLaugh");

        if (!string.IsNullOrEmpty(dollAppearExtraSfxId))
        {
            Play2D(dollAppearExtraSfxId);
        }

        if (dollObject != null)
        {
            dollObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: la muneca no esta asignada, no aparece pero no rompe.");
        }

        PlayFocusSequence(dollFocusSequenceIndex, useCustomDollZoom, dollCustomZoomFov);
        StartCoroutine(PlayDialogueAndWait(PhoneSurpriseDialogueId, 0f, 0f));
    }

    public void OnDollProximityTriggered()
    {
        if (dollTriggered)
        {
            return;
        }

        dollTriggered = true;
        // Debug.Log("Se activo el trigger de la muneca.");

        SetFlag("doll_triggered", true);
        StartCoroutine(DollTriggerRoutine());
    }

    private IEnumerator DollTriggerRoutine()
    {
        // primero dejamos que el jugador procese la muneca
        // despues rompemos algo en el bano, para romper las bolas nomas
        yield return StartCoroutine(PlayDialogueAndWait(DollReactionDialogueId, dollReactionDialogueDelay, dollBreakDelay));

        Vector3 bathroomPosition = bathroomSoundPoint != null ? bathroomSoundPoint.position : transform.position;

        Play3D("BathroomBreak", bathroomPosition);
        Play2D("JumpScare");
        PlayLoop3D("FaucetLoop", bathroomPosition);

        yield return new WaitForSeconds(bathroomReactionDelay);
        StartCoroutine(PlayDialogueAfterDelay(BathroomReactionDialogueId, bathroomReactionDialogueDelay));
    }

    public void OnFaucetClosed()
    {
        if (faucetClosed)
        {
            return;
        }

        faucetClosed = true;
        // Debug.Log("Canilla cerrada.");

        SetFlag("faucet_closed", true);
        StopLoop("FaucetLoop");

        Vector3 bathroomPosition = bathroomSoundPoint != null ? bathroomSoundPoint.position : transform.position;
        Play3D("CloseFaucet", bathroomPosition);

        StartCoroutine(BathroomRevealRoutine());
    }

    private IEnumerator BathroomRevealRoutine()
    {
        if (mirrorTriggered)
        {
            yield break;
        }

        mirrorTriggered = true;

        // la revelacion arranca apenas tocamos la canilla: foco, espejo, sonido y frase
        // la musica espera al final para que el jugador entienda "ah, ahora si corro"
        PlayFocusSequence(bathroomFocusSequenceIndex, useCustomBathroomZoom, bathroomCustomZoomFov);

        SetMirrorMessageObjectsActive(true);

        Play2D("MirrorReveal");

        float mirrorDialogueDuration = GetDialogueDuration(MirrorReactionDialogueId);
        StartCoroutine(PlayDialogueAfterDelay(MirrorReactionDialogueId, mirrorReactionDialogueDelay));

        // espero el focus y tambien el dialogo completo del espejo mas el offset narrativo
        // si queres que la musica entre antes de la ultima linea, mirrorreactiondelay puede ser negativo
        float bathroomFocusLength = GetActiveFocusSequenceDuration();
        float revealWait = Mathf.Max(
            bathroomFocusLength,
            Mathf.Max(0f, mirrorReactionDialogueDelay + mirrorDialogueDuration + mirrorReactionDelay)
        );

        yield return new WaitForSeconds(revealWait);

        StartEscapeSequence();
    }

    private void StartEscapeSequence()
    {
        if (GetFlag("escape_phase_started"))
        {
            return;
        }

        SetFlag("escape_phase_started", true);
        // Debug.Log("IntroSequenceController: fase de escape iniciada.");

        if (musicManager != null)
        {
            musicManager.PlayTensionMusic();
        }

        if (!string.IsNullOrEmpty(agitatedBreathingLoopId))
        {
            PlayLoop2D(agitatedBreathingLoopId);
        }

        if (playerHeadBobTarget != null && !string.IsNullOrEmpty(headBobEscapeMessage))
        {
            playerHeadBobTarget.SendMessage(headBobEscapeMessage, SendMessageOptions.DontRequireReceiver);
        }

        StartMentalFatigueFade();

        if (pursuerSpawnController != null)
        {
            pursuerSpawnController.StartSpawnSequence();
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: pursuerSpawnController no esta asignado.");
        }
    }

    public void OnEscapeAttempted()
    {
        if (escapeAttempted || !GetFlag("escape_phase_started"))
        {
            return;
        }

        escapeAttempted = true;
        // Debug.Log("Cierre de demo iniciado");

        SetFlag("escape_attempted", true);

        StopLoop(agitatedBreathingLoopId);

        if (demoEndController != null)
        {
            demoEndController.StartDemoEnd();
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: el controlador del cierre de demo no esta asignado.");
        }
    }

    public bool IsEscapePhaseStarted()
    {
        return GetFlag("escape_phase_started");
    }

    private void StartMentalFatigueFade()
    {
        if (mentalFatigueImage == null)
        {
            return;
        }

        if (mentalFatigueRoutine != null)
        {
            StopCoroutine(mentalFatigueRoutine);
        }

        mentalFatigueRoutine = StartCoroutine(MentalFatigueFadeRoutine());
    }

    private IEnumerator MentalFatigueFadeRoutine()
    {
        float elapsed = 0f;

        Color color = mentalFatigueImage.color;
        float startAlpha = color.a;

        while (elapsed < mentalFatigueFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = mentalFatigueFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / mentalFatigueFadeDuration);
            color.a = Mathf.Lerp(startAlpha, mentalFatigueTargetAlpha, t);
            mentalFatigueImage.color = color;

            yield return null;
        }

        color.a = mentalFatigueTargetAlpha;
        mentalFatigueImage.color = color;
    }

    private IEnumerator PlayDialogueAfterDelay(string dialogueId, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        DialogueController targetDialogue = DialogueController.Instance;

        if (targetDialogue != null)
        {
            targetDialogue.PlayDialogue(dialogueId);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: no hay DialogueController para reproducir el dialogo: " + dialogueId);
        }
    }

    private IEnumerator PlayDialogueAndWait(string dialogueId, float startDelay, float afterDialogueDelay)
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        DialogueController targetDialogue = DialogueController.Instance;
        float dialogueDuration = 0f;

        if (targetDialogue != null)
        {
            dialogueDuration = targetDialogue.GetDialogueDuration(dialogueId);
            targetDialogue.PlayDialogue(dialogueId);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: no hay DialogueController para reproducir el dialogo: " + dialogueId);
        }

        float waitTime = Mathf.Max(0f, dialogueDuration + afterDialogueDelay);

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator EnablePhoneAfterFocus()
    {
        float energyFocusLength = GetActiveFocusSequenceDuration();
        if (energyFocusLength > 0f)
        {
            yield return new WaitForSeconds(energyFocusLength);
        }

        // phoneringdelay ahora es offset desde el final del dialogo de energia
        // ejemplo: si queres que suene antes de la ultima linea, usa un valor negativo
        yield return StartCoroutine(PlayDialogueAndWait(energyReactionDialogueId, energyReactionDialogueDelay, PhoneRingDelay));

        PlayLoop2D("PhoneRing");

        if (phoneInteractable != null)
        {
            phoneInteractable.SetCanAnswer(true);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: el telefono no esta asignado.");
        }
    }

    private void PlayFocusSequence(int sequenceIndex, bool useCustomZoom = false, float customZoomFov = 0f)
    {
        if (focusCamera == null || sequenceIndex < 0)
        {
            return;
        }

        if (useCustomZoom)
        {
            focusCamera.PlaySequence(sequenceIndex, customZoomFov);
        }
        else
        {
            focusCamera.PlaySequence(sequenceIndex);
        }
    }

    // Debe llamarse despues de PlayFocusSequence para esa misma secuencia (GetTotalSequenceDuration usa currentSequenceIndex internamente).
    private float GetActiveFocusSequenceDuration()
    {
        return focusCamera != null ? focusCamera.GetTotalSequenceDuration() : 0f;
    }

    private float GetDialogueDuration(string dialogueId)
    {
        DialogueController targetDialogue = DialogueController.Instance;
        return targetDialogue != null ? targetDialogue.GetDialogueDuration(dialogueId) : 0f;
    }

    private void Play2D(string id)
    {
        SFXManager targetSfx = sfxManager != null ? sfxManager : SFXManager.Instance;

        if (targetSfx != null)
        {
            targetSfx.Play2D(id);
        }
    }

    private void Play3D(string id, Vector3 position)
    {
        SFXManager targetSfx = sfxManager != null ? sfxManager : SFXManager.Instance;

        if (targetSfx != null)
        {
            targetSfx.Play3D(id, position);
        }
    }

    private void PlayLoop2D(string id)
    {
        SFXManager targetSfx = sfxManager != null ? sfxManager : SFXManager.Instance;

        if (targetSfx != null)
        {
            targetSfx.PlayLoop2D(id);
        }
    }

    private void PlayLoop3D(string id, Vector3 position)
    {
        SFXManager targetSfx = sfxManager != null ? sfxManager : SFXManager.Instance;

        if (targetSfx != null)
        {
            targetSfx.PlayLoop3D(id, position);
        }
    }

    private void StopLoop(string id)
    {
        SFXManager targetSfx = sfxManager != null ? sfxManager : SFXManager.Instance;

        if (targetSfx != null && !string.IsNullOrEmpty(id))
        {
            targetSfx.StopLoop(id);
        }
    }

    private void SetFlag(string flagName, bool value)
    {
        // reviso si gamestatecontroller esta, si esta, lo meto en "targetstate" para usarlo
        // Queda mas fachero asi
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;

        if (targetState != null)
        {
            targetState.SetFlag(flagName, value);
        }
    }

    private bool GetFlag(string flagName)
    {
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;
        return targetState != null && targetState.GetFlag(flagName);
    }

    private void SetMirrorMessageObjectsActive(bool value)
    {
        if (mirrorMessageObjects == null)
        {
            return;
        }

        foreach (GameObject messageObject in mirrorMessageObjects)
        {
            if (messageObject != null)
            {
                messageObject.SetActive(value);
            }
        }
    }
}