using System.Collections;
using UnityEngine;

public class IntroSequenceController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DialogueSequencePlayer introDialoguePlayer;
    [SerializeField] private HintDialogueController lightHintController;
    [SerializeField] private PhoneInteractable phoneInteractable;
    [SerializeField] private FixedCameraWithZoom energyFocusCamera;
    [SerializeField] private FixedCameraWithZoom phoneFocusCamera;
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
    [SerializeField] private DialogueLine[] introLines;

    [Header("Timing")]
    [SerializeField] private float energyFocusDuration = 2f;
    [SerializeField] private float phoneFocusDuration = 2f;
    [SerializeField] private float bathroomFocusDuration = 2f;
    [SerializeField] private float dollLaughDelay = 0f; // antes era 1.25f; lo dejo configurable para tunear el golpe fino
    // este delay antes estaba en phonescareroutine. ahora lo uso despues del focus del telefono,
    // para que la risa y la aparicion de la muneca sigan siendo configurables desde inspector
    [SerializeField] private float bathroomReactionDelay = 1f;

    [Header("Subtitulutos")]
    [SerializeField] private string bathroomReactionSubtitle = "Ese ruido... Vino del baño?";
    [SerializeField] private float bathroomReactionSubtitleDuration = 2f;
    [SerializeField] private string dollReactionSubtitle = "esto estaba antes aca?";
    [SerializeField] private float dollReactionSubtitleDuration = 2.5f;
    [SerializeField] private float mirrorReactionDelay = 2f;
    [SerializeField] private float mirrorReactionSubtitleDuration = 2.5f;
    [SerializeField] private string mirrorReactionSubtitle = "pero que carajo?";

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

        if (playIntroOnStart && introDialoguePlayer != null)
        {
            if (introLines != null && introLines.Length > 0)
            {
                // Debug.Log("IntroSequenceController: dialogos de intro desde el controlador.");
                introDialoguePlayer.PlaySequence(introLines);
            }
            else if (introDialoguePlayer.HasConfiguredLines)
            {
                // Debug.Log("IntroSequenceController: dialogos configurados en DialogueSequencePlayer.");
                introDialoguePlayer.PlayConfiguredSequence();
            }
            else
            {
                Debug.LogWarning("IntroSequenceController: no hay dialogos de intro configurados.");
            }
        }

        if (startLightHintsOnStart && lightHintController != null)
        {
            lightHintController.StartHints();
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

        if (lightHintController != null)
        {
            lightHintController.StopHints();
        }

        Play2D("EnergyRestored");
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
        PlayFocus(phoneFocusCamera, phoneFocusDuration);
        PlayLoop2D("PhoneStatic");

        if (SubtitleUI.Instance != null) SubtitleUI.Instance.ShowSubtitle("hola?", 2f);

        yield return new WaitForSeconds(phoneFocusDuration);

        StopLoop("PhoneStatic");

        // version vieja: play2d("dolllaugh") se ejecutaba inmediatamente al terminar el focus
        // ahora uso dolllaughdelay para ajustar desde inspector cuanto tarda en aparecer la muneca
        if (dollLaughDelay > 0f)
        {
            yield return new WaitForSeconds(dollLaughDelay);
        }

        Play2D("DollLaugh");

        if (dollObject != null)
        {
            dollObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("IntroSequenceController: la muneca no esta asignada, no aparece pero no rompe.");
        }
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
        if (SubtitleUI.Instance != null)
        {
            // version vieja: subtitleui.instance.showsubtitle("esto estaba antes aca?", 2.5f);
            // ahora uso las variables serializadas para ajustar texto y duracion sin tocar codigo
            SubtitleUI.Instance.ShowSubtitle(dollReactionSubtitle, dollReactionSubtitleDuration);
        }

        // version vieja: yield return new waitforseconds(2f);
        // uso la duracion del subtitulo como espera base para que el ruido llegue despues de la reaccion
        yield return new WaitForSeconds(dollReactionSubtitleDuration);

        Vector3 bathroomPosition = bathroomSoundPoint != null ? bathroomSoundPoint.position : transform.position;

        // version vieja: play2d("bathroombreak");
        // ahora uso el punto del bano para que el sonido salga desde el lugar configurable
        Play3D("BathroomBreak", bathroomPosition);
        Play2D("JumpScare");
        PlayLoop3D("FaucetLoop", bathroomPosition);

        yield return new WaitForSeconds(bathroomReactionDelay);
        if (SubtitleUI.Instance != null) SubtitleUI.Instance.ShowSubtitle(bathroomReactionSubtitle, bathroomReactionSubtitleDuration);

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

        if (SubtitleUI.Instance != null)
        {
            // version vieja: subtitleui.instance.showsubtitle("pero que carajo?", 2.5f);
            // ahora uso las variables serializadas para poder ajustar texto y duracion desde inspector
            SubtitleUI.Instance.ShowSubtitle(mirrorReactionSubtitle, mirrorReactionSubtitleDuration);
        }

        // version vieja: yield return new waitforseconds(bathroomfocusduration);
        // ahora espero al menos lo que dure el focus y tambien respeto mirrorreactiondelay como pausa narrativa configurable
        yield return new WaitForSeconds(Mathf.Max(bathroomFocusDuration, mirrorReactionDelay));

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

    private IEnumerator EnablePhoneAfterFocus()
    {
        if (energyFocusDuration > 0f)
        {
            yield return new WaitForSeconds(energyFocusDuration);
        }

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
