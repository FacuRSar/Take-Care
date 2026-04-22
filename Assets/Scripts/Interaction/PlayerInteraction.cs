using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/* este script va en el jugador para detectar objetos interactuables con raycast
*  y disparar la interaccion cuando el jugador aprieta el boton correspondiente.
*/
public class PlayerInteraction : MonoBehaviour
{

    [SerializeField] private string heldObjectLayerName = "Agarrado";
    // Layer que va a usar el objeto mientras está agarrado

    [Header("Raycast")]
    [SerializeField] private Transform cameraTransform;
    // referencia a la camara del jugador para usar como origen del raycast

    [SerializeField] private float interactionDistance = 3f;
    // distancia máxima a la que el jugador puede interactuar con un objeto, me parecio que sumaba

    [SerializeField] private LayerMask interactionMask;
    // mascara de capas para limitar el raycast solo a objetos interactuables asi no hace nada raro con cosas que no corresponde

    [Header("Prompt de UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    // texto que muestra el mensaje de interacción

    [Header("Configuracion de agarrar")]
    [SerializeField] private Transform handPoint;
    // punto donde se posicionan los objetos agarrados

    private Interactable currentInteractable;
    private GrabbableObject currentGrabbable;
    // Guarda referencia al objeto interactuable que el jugador está mirando actualmente y el agarrado

    private bool interactPressed;
    private GrabbableObject pickedObject;
    // Bandera temporal para cuando el input de interactuar se apreto y la otra igual para los agarrados

    private void Update()
    {
        // checkeo que esta mirando
        CheckInteraction();
    }

    public void OnInteract(InputValue value)
    {
        // Este metodo llama al componente Player Input cuando la acción "Interact" se ejecuta (si el componente está configurado en "Send Messages")

        if (value.isPressed)
        {
            interactPressed = true;
        }
    }

    private void LateUpdate()
    {
        // reseteo la bandera al final del frame para que el interact se procese solo una vez por pulsación.
        interactPressed = false;
    }

    private void CheckInteraction()
    {
        // Si el jugador aprieta interactuar y ya tiene algo en la mano, primero suelta ese objeto, porque me da que puede romper todo
        if (interactPressed && pickedObject != null)
        {
            pickedObject.Drop();
            pickedObject = null;
            return;
        }
        RaycastHit hit;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance, interactionMask))
        {
            // busco si el objeto golpeado es interactuable o agarrable
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            GrabbableObject grabbable = hit.collider.GetComponentInParent<GrabbableObject>();

            // guardo la referencia del agarrable actual
            currentGrabbable = grabbable;

            if (interactable != null)
            {
                // actualizo el foco por si cambia el interactuable
                if (currentInteractable != interactable)
                {
                    ClearCurrentInteractable();
                    currentInteractable = interactable;
                    currentInteractable.OnFocus();
                }

                ShowPrompt(currentInteractable.PromptMessage);

                if (interactPressed)
                {
                    currentInteractable.Interact(this);
                }
                return;
            }

            // si no hay interactuable pero sí agarrable, le meto el prompt para agarrar
            if (grabbable != null)
            {
                ClearCurrentInteractable();
                ShowPrompt("E - Agarrar");

                if (interactPressed && pickedObject == null)
                {
                    pickedObject = grabbable;
                    int heldLayer = LayerMask.NameToLayer(heldObjectLayerName);
                    if (heldLayer == -1) heldLayer = grabbable.gameObject.layer;
                    pickedObject.PickUp(handPoint, heldLayer);
                }
                return;
            }
        }

        // Si no estamos mirando nada interactuable
        currentGrabbable = null;
        ClearCurrentInteractable();
        HidePrompt();
    }

    private void ShowPrompt(string message)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void HidePrompt()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    private void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    public bool HasObjectInHand()
    {
        //dejo esto por si en el futuro un interactuable necesita saber si el jugador ya tiene un objeto agarrado.
        return pickedObject != null;
    }

    public GameObject GetPickedObject()
    {
        // tiro el GameObject agarrado si existe
        if (pickedObject == null)
        {
            return null;
        }

        return pickedObject.gameObject;
    }
}