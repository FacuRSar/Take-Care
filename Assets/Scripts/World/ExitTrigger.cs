using UnityEngine;

// trigger de la puerta principal
// antes de la fase final bloquea/sugiere no salir
// durante escape_phase_started avisa al introsequencecontroller y ejecuta el cierre de demo
public class ExitTrigger : MonoBehaviour
{
    [Header("Controlador de intro")]
    [SerializeField] private IntroSequenceController introSequenceController;

    [Header("Configuracion de puerta")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float closedZRotation = 0f;
    [SerializeField] private float closeSpeed = 6f;

    [Header("Empuje")]
    [SerializeField] private float pushBackForce = 4f;

    [Header("Mensajes")]
    [SerializeField] private string beforeIntroMessage = "Hay una tormenta muy grande, aca estoy mas seguro.";
    [SerializeField] private string blockedMessage = "No... mejor no salir ahora.";
    [SerializeField] private float messageDuration = 2.5f;

    private bool closingDoor;
    private bool alreadyClosed;
    private bool escapeAttemptSent;

    private void Update()
    {
        if (!closingDoor || doorPivot == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, closedZRotation);

        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation,
            targetRotation,
            closeSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(doorPivot.localRotation, targetRotation) < 0.5f)
        {
            doorPivot.localRotation = targetRotation;
            closingDoor = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = other.GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogWarning("ExitTrigger: el jugador no tiene Rigidbody.");
            return;
        }

        if (IsEscapePhaseStarted())
        {
            StartClosingDoor();
            PushBack(rb);
            NotifyEscapeAttempt();
            return;
        }

        if (!IsIntroActivated())
        {
            ShowSubtitle(beforeIntroMessage);
            PushBack(rb);
            return;
        }

        if (!alreadyClosed)
        {
            alreadyClosed = true;
            StartClosingDoor();
            ShowSubtitle(blockedMessage);
            PushBack(rb);
            return;
        }

        PushBack(rb);
    }

    private bool IsEscapePhaseStarted()
    {
        return introSequenceController != null && introSequenceController.IsEscapePhaseStarted();
    }

    private bool IsIntroActivated()
    {
        return GameStateController.Instance != null && GameStateController.Instance.IntroActivated;
    }

    private void NotifyEscapeAttempt()
    {
        if (escapeAttemptSent)
        {
            return;
        }

        escapeAttemptSent = true;

        // en fase final avisamos una sola vez. si no el cierre de demo arranca en loop,
        // y ahi si que la puerta se pone intensa
        if (introSequenceController != null)
        {
            introSequenceController.OnEscapeAttempted();
        }
        else
        {
            Debug.LogWarning("ExitTrigger: no tiene IntroSequenceController asignado.");
        }
    }

    private void StartClosingDoor()
    {
        if (doorPivot == null)
        {
            Debug.LogWarning("ExitTrigger: doorPivot no esta asignado.");
            return;
        }

        closingDoor = true;
        alreadyClosed = true;
    }

    private void PushBack(Rigidbody rb)
    {
        Vector3 pushDirection = -transform.forward;
        pushDirection.y = 0f;
        pushDirection.Normalize();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(pushDirection * pushBackForce, ForceMode.Impulse);
    }

    private void ShowSubtitle(string message)
    {
        if (SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(message, messageDuration);
        }
    }
}
