using UnityEngine;

/* Volumen detras de la puerta de salida.
 * Cuando el jugador lo cruza y ya gano (la puerta se abrio por felicidad),
 * le avisa al controlador para cargar la escena de victoria.
 * Si todavia no gano, no hace nada.
 */
[RequireComponent(typeof(Collider))]
public class WinExitTrigger : MonoBehaviour
{
    [SerializeField] private InGameSequenceController flow;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (flow != null)
        {
            flow.NotifyReachedExit();
        }
    }
}
