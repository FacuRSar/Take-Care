using System.Collections;
using UnityEngine;

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
    [SerializeField] private FixedCameraWithZoom energyFocusCamera;
    [SerializeField] private FixedCameraWithZoom phoneFocusCamera;
    [SerializeField] private FixedCameraWithZoom dollFocusCamera;
    [SerializeField] private FixedCameraWithZoom bathroomFocusCamera;
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private SFXManager sfxManager;
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private DemoEndController demoEndController;
    [SerializeField] private MirrorMessageController mirrorMessageController;

    [Header("Objetos de la escena")]
    [SerializeField] private GameObject dollObject;
    [SerializeField] private Transform bathroomSoundPoint;

    [Header("Dialogos de intro")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private bool startLightHintsOnStart = true;
    [SerializeField] private float introDialogueStartDelay = 0f;

    [Header("Timing")]
    [SerializeField] private float energyFocusDuration = 2f;
    [SerializeField] private float phoneFocusDuration = 2f;
    [SerializeField] private float dollFocusDuration = 0.5f;
    [SerializeField] private float bathroomFocusDuration = 2f;

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

        PlayFocus(energyFocusCamera, energyFocusDuration);
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
        // version vieja: esto pasaba todo junto aca abajo, pum pum pum, y quedaba medio atragantado
        // lo dejo comentado para recordar que ahora el ritmo vive en phoneanswerroutine
        // play2d("phonestatic");
        // playfocus(phonefocuscamera, phonefocusduration);
        // if (dollobject != null) dollobject.setactive(true);
        // startcoroutine(phonescareroutine());
    }

    private IEnumerator PhoneAnswerRoutine()
    {
        // orden del telefono: miro el telefono, suena la estatica, digo hola, y recien despues cae la risa
        // si lo hacemos todo de una queda medio chueco, lo se porque tuve que meter esto porque hacia eso jajajj
        float phoneDialogueWait = phoneHelloDialogueDelay + GetDialogueDuration(PhoneHelloDialogueId);
        float phoneTotalWait = Mathf.Max(0f, phoneDialogueWait) + phoneFocusDuration;
        PlayFocus(phoneFocusCamera, phoneTotalWait);
        PlayLoop2D("PhoneStatic");
        StartCoroutine(PlayDialogueAfterDelay(PhoneHelloDialogueId, phoneHelloDialogueDelay));

        yield return new WaitForSeconds(phoneTotalWait);

        StopLoop("PhoneStatic");

        // version vieja: play2d("dolllaugh") se ejecutaba inmediatamente al terminar el focus
        // ahora uso dolllaughdelay para ajustar desde inspector cuanto tarda en aparecer la muneca
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

        PlayFocus(dollFocusCamera, dollFocusDuration);
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
        // el delay del ruido ahora parte desde el final del dialogo de la muneca
        // si hace falta adelantarlo, se pone un valor negativo en dollbreakdelay
        yield return StartCoroutine(PlayDialogueAndWait(DollReactionDialogueId, dollReactionDialogueDelay, dollBreakDelay));

        Vector3 bathroomPosition = bathroomSoundPoint != null ? bathroomSoundPoint.position : transform.position;

        // version vieja: play2d("bathroombreak");
        // ahora uso el punto del bano para que el sonido salga desde el lugar configurable
        Play3D("BathroomBreak", bathroomPosition);
        Play2D("JumpScare");
        PlayLoop3D("FaucetLoop", bathroomPosition);

        yield return new WaitForSeconds(bathroomReactionDelay);
        StartCoroutine(PlayDialogueAfterDelay(BathroomReactionDialogueId, bathroomReactionDialogueDelay));

    }

    // version vieja: este evento disparaba el espejo desde otro lado
    // lo dejo comentado porque ahora el espejo nace cuando se cierra la canilla, mas ordenadito el drama
    // public void onmirrormessagetriggered()
    // {
    //     if (mirrortriggered) return;
    //     mirrortriggered = true;
    //     debug.log("mensaje del espejo activado.");
    //     setflag("mirror_message_triggered", true);
    //     play2d("mirrorreveal");
    //     startcoroutine(bathroomrevealroutine());
    // }

    public void OnFaucetClosed()
    {
        if (faucetClosed)
        {
            return;
        }

        faucetClosed = true;
        // Debug.Log("Canilla cerrada.");

        Vector3 bathroomPosition = bathroomSoundPoint != null ? bathroomSoundPoint.position : transform.position;

        SetFlag("faucet_closed", true);
        StopLoop("FaucetLoop");
        Play3D("CloseFaucet", bathroomPosition);

        StartCoroutine(BathroomRevealRoutine());

        // version vieja: aca se mostraba el espejo y se llamaba a onmirrormessagetriggered()
        // ahora lo maneja bathroomrevealroutine para que focus, subtitulo y musica salgan en orden
        // play3d("closefaucet", bathroomposition);
        // mirrormessagecontroller.showmessage();
        // onmirrormessagetriggered();
    }

    private IEnumerator BathroomRevealRoutine()
    {
        if (mirrorTriggered)
        {
            yield break;
        }

        // version vieja: mirrortriggered se usaba en onmirrormessagetriggered()
        // ahora lo uso aca porque la revelacion del espejo nace desde la canilla
        mirrorTriggered = true;

        // la revelacion arranca apenas tocamos la canilla: foco, espejo, sonido y frase
        // la musica espera al final para que el jugador entienda "ah, ahora si corro"
        PlayFocus(bathroomFocusCamera, bathroomFocusDuration);

        if (mirrorMessageController != null)
        {
            mirrorMessageController.ShowMessage();
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: el controlador del espejo no esta asignado.");
        }

        Play2D("MirrorReveal");

        float mirrorDialogueDuration = GetDialogueDuration(MirrorReactionDialogueId);
        StartCoroutine(PlayDialogueAfterDelay(MirrorReactionDialogueId, mirrorReactionDialogueDelay));

        // espero el focus y tambien el dialogo completo del espejo mas el offset narrativo
        // si queres que la musica entre antes de la ultima linea, mirrorreactiondelay puede ser negativo
        float revealWait = Mathf.Max(
            bathroomFocusDuration,
            Mathf.Max(0f, mirrorReactionDialogueDelay + mirrorDialogueDuration + mirrorReactionDelay)
        );

        yield return new WaitForSeconds(revealWait);

        // parada de boxes antes de la musica
        yield return new WaitForSeconds(1f);

        SetFlag("escape_phase_started", true);
        // Debug.Log("ESCAPA WACHIN!");

        if (musicManager != null)
        {
            musicManager.PlayTensionMusic();
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

    private IEnumerator PlayDialogueAfterDelay(string dialogueId, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        DialogueSequencePlayer targetDialogue = DialogueSequencePlayer.Instance;

        if (targetDialogue != null)
        {
            targetDialogue.PlayDialogue(dialogueId);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: no hay DialogueSequencePlayer para reproducir el dialogo: " + dialogueId);
        }
    }

    private IEnumerator PlayDialogueAndWait(string dialogueId, float startDelay, float afterDialogueDelay)
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        DialogueSequencePlayer targetDialogue = DialogueSequencePlayer.Instance;
        float dialogueDuration = 0f;

        if (targetDialogue != null)
        {
            dialogueDuration = targetDialogue.GetDialogueDuration(dialogueId);
            targetDialogue.PlayDialogue(dialogueId);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: no hay DialogueSequencePlayer para reproducir el dialogo: " + dialogueId);
        }

        float waitTime = Mathf.Max(0f, dialogueDuration + afterDialogueDelay);

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator EnablePhoneAfterFocus()
    {
        if (energyFocusDuration > 0f)
        {
            yield return new WaitForSeconds(energyFocusDuration);
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

    // version vieja: la rutina de susto del telefono estaba separada
    // quedo comentada porque ahora todo el timing del telefono vive en phoneanswerroutine
    // private ienumerator phonescareroutine()
    // {
    //     if (dolllaughdelay > 0f)
    //     {
    //         yield return new waitforseconds(dolllaughdelay);
    //     }
    //
    //     play2d("dolllaugh");
    //     play2d("jumpscare");
    // }

    private void PlayFocus(FixedCameraWithZoom focusCamera, float duration)
    {
        if (focusCamera == null)
        {
            return;
        }

        focusCamera.ActivateForDuration(duration);
    }

    private float GetDialogueDuration(string dialogueId)
    {
        DialogueSequencePlayer targetDialogue = DialogueSequencePlayer.Instance;
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

        if (targetSfx != null)
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
}
