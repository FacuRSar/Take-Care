using UnityEngine;

// el trigger cambia si la intro ya fue activada o no.
public class ExitTrigger : MonoBehaviour
{
    [Header("Configuracion de puerta")]
    [SerializeField] private Transform doorPivot;
    // referencia al objeto que rota la puerta

    [SerializeField] private float closedZRotation = 0f;
    // rotación objetivo de la puerta al cerrarse

    [SerializeField] private float closeSpeed = 6f;
    // velocidad de cierre. Meto estas aca porque no se como va a quedar

    [Header("Empuje")]
    [SerializeField] private float pushBackForce = 4f;
    // fuerza con la que se empuja al jugador hacia atras

    private bool closingDoor;
    private bool alreadyClosed;

    private void Update()
    {
        if (closingDoor && doorPivot != null)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, closedZRotation);
            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, targetRotation, closeSpeed * Time.deltaTime);
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
            return;
        }

        if (!GameStateController.Instance.IntroActivated)
        {
            SubtitleUI.Instance.ShowSubtitle("Hay una tormenta muy grande, acá estoy más seguro.", 2.5f);
            PushBack(rb);
        }
        else
        {
            if (!alreadyClosed)
            {
                alreadyClosed = true;
                closingDoor = true;
                SubtitleUI.Instance.ShowSubtitle("No... mejor no salir ahora.", 2.5f);
            }
            // Tengoq ue revisarlo, pero la idea es que la primera vez que intente salir lo empuje para atras cuando se cierre la puerta
            else
            {
                PushBack(rb);
            }

        }
    }

    private void PushBack(Rigidbody rb)
    {
        Vector3 pushDirection = -transform.forward;
        pushDirection.y = 0f;
        pushDirection.Normalize();

        // freno los demas movimientos para que no pueda salir algun bug raro
        rb.linearVelocity = Vector3.zero;

        // meto la fuerza de empuje nomas
        rb.AddForce(pushDirection * pushBackForce, ForceMode.Impulse);
    }
}