using UnityEngine;

/* script reutilizable para puertas
*  te deja configurar desde Inspector si:
*  la puerta puede abrirse o no, si necesita una condiciùn del juego o no y quù mensaje mostrar cuando estù bloqueada
*  no me gusto el anterior que habia armado, este es mas escalable
*/
public class DoorInteractable : Interactable
{
    public enum DoorRequirementType
    {
        None,
        IntroActivated,
        CustomFlag
    }

    [Header("Referencias de la puerta")]
    [SerializeField] private Transform doorPivot;
    //pivot real de la puerta (es el que abre o cierra)

    [Header("Rotacion de puerta")]
    [SerializeField] private float closedZRotation = 0f;
    //rotacion de la puerta cerrada
    [SerializeField] private float openedZRotation = 90f;
    //rotacion de la puerta abierta

    [SerializeField] private float openSpeed = 6f;
    //velocidad de apertura/cierre

    [Header("Configuracion de puerta")]
    [SerializeField] private bool ClosedSound = true;
    [SerializeField] private bool canOpen = false;
    //define si esta puerta puede abrirse en general
    //false = siempre bloqueada (Por si acaso)

    [SerializeField] private bool startsOpened = false;
    // te tira si una puerta empieza abierta por si hace falta

    [SerializeField] private string lockedMessage = "Parece que alguien la cerrù desde el otro lado.";
    // mensaje cuando la puerta no puede abrirse full generico

    [Header("Requerimientos de puerta")]
    [SerializeField] private DoorRequirementType requirementType = DoorRequirementType.None;
    //tipo de requisito que necesita esta puerta para poder abrirse, asi lo podemos escalar

    [SerializeField] private string customFlagName = "";
    //nombre de flag personalizado si usamos DoorRequirementType.CustomFlag

    [Header("Mensaje personalizado por flag (opcional)")]
    [SerializeField] private bool useFlagMessageOverride = false;
    // si esta activo y la flag de abajo esta encendida, el feedback al interactuar
    // usa un mensaje del pool en vez del mensaje normal
    [SerializeField] private string messageOverrideFlagName = "";
    [TextArea]
    [SerializeField] private string[] flagActiveMessages = new string[] { "La puerta no se abre" };
    // pool de mensajes: se elige uno al azar al interactuar con la puerta bloqueada

    [Header("Cierre automatico por contacto (opcional)")]
    [SerializeField] private bool closeOnPlayerContact = false;
    // pensado para la intro: cuando el jugador toca la puerta, se cierra sola.
    // en gameplay normal se deja desactivado.
    [SerializeField] private string playerContactTag = "Player";
    [SerializeField] private string contactCloseSfxId = "CloseDoor";
    // el cierre por contacto SOLO ocurre si esta flag esta activa.
    // si se deja vacio, cierra siempre que haya contacto.
    [SerializeField] private string contactCloseFlagName = "";
    // mensaje que salta al tocar la puerta mientras todavia no se puede cerrar/salir
    [TextArea]
    [SerializeField] private string contactBlockedMessage = "Todavia no puedo salir.";

    private bool isOpen;
    // estado actual de la puerta
    private bool isMoving;
    // para controlar si la puerta se esta abriendo

    private Quaternion targetRotation;
    // rotacion

    private string pendingEndSound;
    // Sonido que queda pendiente para reproducirse cuando termina el movimiento

    private bool contactCloseDone;
    // para que el cierre por contacto pase una sola vez

    private void Start()
    {
        // le mando el inicio
        isOpen = startsOpened;

        if (doorPivot != null)
        {
            //Debug.LogWarning(gameObject.name + " no tiene doorPivot");
            float initialZ = isOpen ? openedZRotation : closedZRotation;
            doorPivot.localRotation = Quaternion.Euler(0f, 0f, initialZ);
            targetRotation = doorPivot.localRotation;
        }
    }

    private void Update()
    {
        // si la puerta estù moviendose le tiramos pa que siga hasta si objetivo
        if (isMoving && doorPivot != null)
        {
            doorPivot.localRotation = Quaternion.Slerp(
                doorPivot.localRotation,
                targetRotation,
                openSpeed * Time.deltaTime
            );

            // Cuando esta muy cerquita del objetivo, isMoving pasa a false para dejar interactuar
            if (Quaternion.Angle(doorPivot.localRotation, targetRotation) < 0.5f)
            {
                doorPivot.localRotation = targetRotation;
                isMoving = false;

                // Si habia un sonido pendiente para el final del movimiento, lo reproduzco ahora.
                if (!string.IsNullOrEmpty(pendingEndSound))
                {
                    SFXManager.Instance.Play3D(pendingEndSound, transform.position);
                    pendingEndSound = null;
                }
            }
        }
    }

    public override void Interact(PlayerInteraction player)
    {
        // Si la puerta esta moviendose, no dejo interactuar para evitar bugs o spam.
        if (isMoving)
        {
            return;
        }

        // si la puerta no puede abrirse por configuracion general mete feedback y sale
        if (!canOpen)
        {
            SubtitleUI.Instance.ShowSubtitle(GetLockedFeedbackMessage(), 2.5f);
            if (ClosedSound) SFXManager.Instance.Play3D("LockedDoor", transform.position);
            return;
        }

        // si necesita una condiciùn y no se cumple mete feedback y sale
        if (!CanOpenByState())
        {
            SubtitleUI.Instance.ShowSubtitle(GetLockedFeedbackMessage(), 2.5f);
            if (ClosedSound) SFXManager.Instance.Play3D("LockedDoor", transform.position);
            return;
        }

        // si pasa todas las validaciones, alterna apertura o cierre
        ToggleDoor();
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public void OpenFromAI()
    {
        // Metodo para que una IA pueda abrir puertas sin hacer toggle.
        // Esto evita que el stalker abra y cierre la misma puerta en loop.
        if (isOpen || isMoving)
        {
            return;
        }

        if (!canOpen)
        {
            return;
        }

        if (!CanOpenByState())
        {
            return;
        }

        OpenDoor();
    }

    private bool CanOpenByState()
    {
        // se fija si la puerta cumple la condicion
        switch (requirementType)
        {
            case DoorRequirementType.None:
                return true;

            case DoorRequirementType.IntroActivated:
                return GameStateController.Instance != null && GameStateController.Instance.IntroActivated;

            case DoorRequirementType.CustomFlag:
                return GameStateController.Instance != null &&
                       GameStateController.Instance.GetFlag(customFlagName);

            default:
                return false;
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        float targetZ;

        if (isOpen)
        {
            targetZ = openedZRotation;

            // Al abrir, el sonido suena apenas empieza el movimiento.
            SFXManager.Instance.Play3D("OpenDoor", transform.position);

            // No dejo sonido pendiente para el final de apertura asi eso lo tira cuando se cierra
            pendingEndSound = null;
        }
        else
        {
            targetZ = closedZRotation;

            // Al cerrar, el sonido queda pendiente y suena cuando llega al final.
            pendingEndSound = "CloseDoor";
        }

        targetRotation = Quaternion.Euler(0f, 0f, targetZ);
        isMoving = true;
    }

    private void OpenDoor()
    {
        isOpen = true;

        // Apertura directa para IA. No hace toggle, solo abre.
        SFXManager.Instance.Play3D("OpenDoor", transform.position);
        pendingEndSound = null;

        targetRotation = Quaternion.Euler(0f, 0f, openedZRotation);
        isMoving = true;
    }

    private bool IsMessageOverrideFlagActive()
    {
        if (string.IsNullOrEmpty(messageOverrideFlagName))
        {
            return false;
        }

        return GameStateController.Instance != null &&
               GameStateController.Instance.GetFlag(messageOverrideFlagName);
    }

    private string PickFlagMessage()
    {
        if (flagActiveMessages == null || flagActiveMessages.Length == 0)
        {
            return base.PromptMessage;
        }

        int index = Random.Range(0, flagActiveMessages.Length);
        return flagActiveMessages[index];
    }

    private string GetLockedFeedbackMessage()
    {
        // cuando la flag de override esta activa, solo el feedback al interactuar usa el pool.
        // el prompt de cercania sigue siendo el PromptMessage base ("Interactuar", etc.).
        if (useFlagMessageOverride && IsMessageOverrideFlagActive())
        {
            return PickFlagMessage();
        }

        return lockedMessage;
    }

    private void OnTriggerEnter(Collider other)
    {
        // cierre por contacto: solo si esta habilitado y no paso todavia
        if (!closeOnPlayerContact || contactCloseDone)
        {
            return;
        }

        if (!string.IsNullOrEmpty(playerContactTag) && !other.CompareTag(playerContactTag))
        {
            return;
        }

        // El cierre solo ocurre con la flag activa. Antes de eso, solo mostramos un mensaje al intentar salir.
        if (IsContactCloseFlagActive())
        {
            ForceClose();
        }
        else
        {
            ShowContactBlockedMessage();
        }
    }

    private bool IsContactCloseFlagActive()
    {
        // sin flag configurada se comporta como antes: cierra al primer contacto
        if (string.IsNullOrEmpty(contactCloseFlagName))
        {
            return true;
        }

        return GameStateController.Instance != null &&
               GameStateController.Instance.GetFlag(contactCloseFlagName);
    }

    private void ShowContactBlockedMessage()
    {
        if (string.IsNullOrEmpty(contactBlockedMessage) || SubtitleUI.Instance == null)
        {
            return;
        }

        SubtitleUI.Instance.ShowSubtitle(contactBlockedMessage, 2.5f);
    }

    // Cierre forzado (ej: portazo en la intro). No depende de canOpen ni de requisitos.
    // Tambien se puede llamar desde un evento externo si no se usa el trigger.
    public void ForceClose()
    {
        contactCloseDone = true;

        if (doorPivot == null || (!isOpen && !isMoving))
        {
            return;
        }

        isOpen = false;
        targetRotation = Quaternion.Euler(0f, 0f, closedZRotation);
        isMoving = true;

        if (!string.IsNullOrEmpty(contactCloseSfxId))
        {
            pendingEndSound = contactCloseSfxId;
        }
    }
}