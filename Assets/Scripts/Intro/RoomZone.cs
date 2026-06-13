using UnityEngine;

/* Marca una habitacion con un trigger (BoxCollider con Is Trigger).
*  Avisa en que habitacion esta parado el jugador y cual es la puerta de entrada,
*  asi el Pursuer en patrulla puede decidir ir a abrir el cuarto donde esta el jugador.
*/
[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    // puerta que el Pursuer abre para entrar a esta habitacion
    [SerializeField] private DoorInteractable entranceDoor;

    // habitacion donde esta el jugador ahora mismo (null si esta en un pasillo / sin zona)
    public static RoomZone CurrentPlayerRoom { get; private set; }

    public DoorInteractable EntranceDoor => entranceDoor;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            CurrentPlayerRoom = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && CurrentPlayerRoom == this)
        {
            CurrentPlayerRoom = null;
        }
    }

    private void OnDisable()
    {
        if (CurrentPlayerRoom == this)
        {
            CurrentPlayerRoom = null;
        }
    }
}
