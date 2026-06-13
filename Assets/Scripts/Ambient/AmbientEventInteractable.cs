using UnityEngine;

/* puente entre el sistema de interaccion del jugador y un AmbientEvent.
*  cuando el jugador interactua (E) con este objeto, dispara el evento.
*
*  uso tipico: poner un AmbientEvent con TriggerMode = OnInteract en el mismo objeto.
*  el AmbientEvent agrega y configura este componente solo. Igual se puede usar a mano:
*  asignar el evento en targetEvent y un prompt en overridePrompt.
*  el objeto necesita un Collider en la layer de interaccion para que el raycast lo detecte.
*/
public class AmbientEventInteractable : Interactable
{
    [SerializeField] private AmbientEvent targetEvent;

    // si se deja vacio, usa el prompt que le pase el AmbientEvent (o el de la clase base)
    [SerializeField] private string overridePrompt = "";

    private string boundPrompt;

    public override string PromptMessage
    {
        get
        {
            if (!string.IsNullOrEmpty(overridePrompt))
            {
                return overridePrompt;
            }

            if (!string.IsNullOrEmpty(boundPrompt))
            {
                return boundPrompt;
            }

            return base.PromptMessage;
        }
    }

    private void Awake()
    {
        if (targetEvent == null)
        {
            targetEvent = GetComponent<AmbientEvent>();
        }
    }

    // lo llama el AmbientEvent en modo OnInteract para enlazarse y pasar su prompt
    public void Bind(AmbientEvent ev, string prompt)
    {
        targetEvent = ev;
        boundPrompt = prompt;
    }

    public override void Interact(PlayerInteraction player)
    {
        if (targetEvent != null)
        {
            targetEvent.FireFromInteraction();
        }
        else
        {
            Debug.LogWarning("[AmbientEventInteractable] No hay AmbientEvent asignado en " + name + ".");
        }
    }
}
