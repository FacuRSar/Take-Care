using UnityEngine;

// trigger que activa el sonido del telefono una vez nomas cuando el jugador pasa la zona (no esta implementado al 21/04)
public class PhoneTrigger : MonoBehaviour
{
    [Header("Audio del telefono")]
    [SerializeField] private AudioSource phoneAudio;

    private bool activated;
    //evita que el trigger se active varias veces

    private void OnTriggerEnter(Collider other)
    {
        if (activated)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        activated = true;

        if (phoneAudio != null)
        {
            phoneAudio.Play();
        }
    }
}