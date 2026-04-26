using UnityEngine;

/* script reutilizable para puertas
*  te deja configurar desde Inspector si:
*  la puerta puede abrirse o no, si necesita una condición del juego o no y qué mensaje mostrar cuando está bloqueada
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
    [SerializeField] private bool canOpen = false;
    //define si esta puerta puede abrirse en general
    //false = siempre bloqueada (Por si acaso)

    [SerializeField] private bool startsOpened = false;
    // te tira si una puerta empieza abierta por si hace falta

    [SerializeField] private string lockedMessage = "Parece que alguien la cerró desde el otro lado.";
    // mensaje cuando la puerta no puede abrirse full generico

    [Header("Requerimientos de puerta")]
    [SerializeField] private DoorRequirementType requirementType = DoorRequirementType.None;
    //tipo de requisito que necesita esta puerta para poder abrirse, asi lo podemos escalar

    [SerializeField] private string customFlagName = "";
    //nombre de flag personalizado si usamos DoorRequirementType.CustomFlag

    private bool isOpen;
    // estado actual de la puerta
    private bool isMoving;
    // para controlar si la puerta se esta abriendo

    private Quaternion targetRotation;
    // rotacion

    private string pendingEndSound;
    // Sonido que queda pendiente para reproducirse cuando termina el movimiento

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
        // si la puerta está moviendose le tiramos pa que siga hasta si objetivo
        if (isMoving && doorPivot != null)
        {
            doorPivot.localRotation = Quaternion.Slerp(
                doorPivot.localRotation,
                targetRotation,
                openSpeed * Time.deltaTime
            );

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
            SubtitleUI.Instance.ShowSubtitle(lockedMessage, 2.5f);
            SFXManager.Instance.Play3D("LockedDoor", transform.position);
            return;
        }

        // si necesita una condición y no se cumple mete feedback y sale
        if (!CanOpenByState())
        {
            SubtitleUI.Instance.ShowSubtitle(lockedMessage, 2.5f);
            SFXManager.Instance.Play3D("LockedDoor", transform.position);
            return;
        }

        // si pasa todas las validaciones, alterna apertura o cierre
        ToggleDoor();
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
}