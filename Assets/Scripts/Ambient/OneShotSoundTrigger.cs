using UnityEngine;

/* Trigger generico para reproducir un sonido una o varias veces
*  sirve para crujidos, golpes, susurros o sonidos puntuales del ambiente
*/
public class OneShotSoundTrigger : MonoBehaviour
{
    [Header("Sonido")]
    [SerializeField] private string sfxId;
    // ID de la pool que se va a reproducir en SFXManager

    [SerializeField] private bool playAs3D = true;
    // si esta activo, el sonido sale desde la posicion del trigger
    // si esta apagado, suena como sonido 2D/global

    [Header("Configuraciones del trigger")]
    [SerializeField] private bool triggerOnce = true;
    // si esta activo, solo se dispara una vez

    [SerializeField] private bool onlyPlayer = true;
    // si esta activo, solo se dispara cuando entra el Player

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered)
        {
            return;
        }

        if (onlyPlayer && !other.CompareTag("Player"))
        {
            return;
        }

        triggered = true;

        if (SFXManager.Instance == null)
        {
            Debug.LogWarning("No hay SFXManager en escena.");
            return;
        }

        if (playAs3D)
        {
            SFXManager.Instance.Play3D(sfxId, transform.position);
        }
        else
        {
            SFXManager.Instance.Play2D(sfxId);
        }
    }
}