using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DollProximityTrigger : MonoBehaviour
{
    [Header("Controlador de intro")]
    [SerializeField] private IntroSequenceController introSequenceController;

    [Header("Deteccion")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private bool useLayerCheck;

    private bool triggered;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        // puede detectar por tag o por layer, segun como este armada la escena
        // lo hice doble porque me hinche los huevos de marearme
        if (!IsPlayer(other))
        {
            return;
        }

        triggered = true;

        if (introSequenceController != null)
        {
            introSequenceController.OnDollProximityTriggered();
        }
        else
        {
            Debug.LogWarning("DollProximityTrigger: no tiene IntroSequenceController asignado.");
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (useLayerCheck)
        {
            int layerMask = 1 << other.gameObject.layer;
            return (playerLayers.value & layerMask) != 0;
        }

        return other.CompareTag(playerTag);
    }
}
