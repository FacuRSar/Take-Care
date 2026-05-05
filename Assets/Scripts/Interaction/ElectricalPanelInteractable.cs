using UnityEngine;

public class ElectricalPanelInteractable : Interactable
{
    [Header("Referencias")]
    [SerializeField] private Transform triggerPivot;
    [SerializeField] private float onRotation = -35f;
    [SerializeField] private float offRotation = 35f;
    [SerializeField] private float movementSpeed = 6f;

    [Header("Configuraciones")]
    [SerializeField] private IntroSequenceController introSequenceController;
    [SerializeField] private string powerFlagName = "power_on";
    [SerializeField] private string restoredFlagName = "energy_restored";
    [SerializeField] private bool notifyIntroSequence = true;

    private string pendingEndSound;
    private float offSpeed;
    private Quaternion targetRotation;
    private bool introAlreadyNotified;
    private bool isMoving;
    private bool movingToOn;

    private void Start()
    {
        movingToOn = false;
        isMoving = false;

        if (triggerPivot != null)
        {
            triggerPivot.localRotation = Quaternion.Euler(offRotation, 0f, 0f);
            targetRotation = triggerPivot.localRotation;
        }
    }

    private void Update()
    {
        // si la palanca está moviendose le tiramos pa que siga hasta si objetivo
        if (isMoving && triggerPivot != null)
        {
            if (movingToOn)
            {
                triggerPivot.localRotation = Quaternion.Slerp(
                    triggerPivot.localRotation,
                    targetRotation,
                    movementSpeed * Time.deltaTime
                );
            } else
            {
                triggerPivot.localRotation = Quaternion.RotateTowards(
                    triggerPivot.localRotation,
                    targetRotation,
                    offSpeed * Time.deltaTime
                );
            }


            // Cuando esta muy cerquita del objetivo, isMoving pasa a false para dejar interactuar
            if (Quaternion.Angle(triggerPivot.localRotation, targetRotation) < 0.5f)
            {
                triggerPivot.localRotation = targetRotation;
                isMoving = false;

                // Si habia un sonido pendiente para el final del movimiento, lo reproduzco ahora.
                if (!string.IsNullOrEmpty(pendingEndSound))
                {
                    SFXManager.Instance.Play2D(pendingEndSound);
                    pendingEndSound = null;
                }
            }
        }
    }

    public override void Interact(PlayerInteraction player)
    {
        GameStateController targetState = GameStateController.Instance;

        if (targetState == null)
        {
            Debug.LogWarning("No hay GameStateController en escena");
            return;
        }

        if (targetState != null && targetState.GetFlag(powerFlagName))
        {
            // Debug.Log("la electricidad ya esta activa");
            return;
        }

        if (targetState != null)
        {
            EnergyOn();
            targetState.SetFlag(powerFlagName, true);
            targetState.SetFlag(restoredFlagName, true);
        }

        if (notifyIntroSequence && !introAlreadyNotified && introSequenceController != null)
        {
            introAlreadyNotified = true;
            introSequenceController.OnEnergyRestored();
        }

        Debug.Log("panel electrico activado");
    }

    private void EnergyOn()
    {
        isMoving = true;
        movingToOn = true;
        pendingEndSound = "EnergyRestored";
        targetRotation = Quaternion.Euler(onRotation, 0f, 0f);
    }

    public void EnergyOff()
    {
        GameStateController targetState = GameStateController.Instance;

        if (targetState != null)
        {
            targetState.SetFlag(powerFlagName, false);
        }

        isMoving = true;
        movingToOn = false;
        offSpeed = movementSpeed * Mathf.Abs(onRotation - offRotation);
        pendingEndSound = "EnergyOff";
        targetRotation = Quaternion.Euler(offRotation, 0f, 0f);

        Debug.Log("electricidad apagada");
    }
}
