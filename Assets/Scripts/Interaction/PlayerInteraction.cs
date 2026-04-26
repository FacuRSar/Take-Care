using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/* este script va en el jugador para detectar objetos interactuables con raycast
*  y disparar la interaccion cuando el jugador aprieta el boton correspondiente
*/
public class PlayerInteraction : MonoBehaviour
{

    [SerializeField] private string heldObjectLayerName = "Agarrado";
    // layer que va a usar el objeto mientras esta agarrado

    [Header("Raycast")]
    [SerializeField] private Transform cameraTransform;
    // referencia a la camara del jugador para usar como origen del raycast

    [SerializeField] private float interactionDistance = 3f;
    // distancia maxima a la que el jugador puede interactuar con un objeto, me parecio que sumaba

    [SerializeField] private LayerMask interactionMask;
    // mascara de capas para limitar el raycast solo a objetos interactuables asi no hace nada raro con cosas que no corresponde

    [Header("Prompt de UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    // texto que muestra el mensaje de interaccion

    [Header("Configuracion de agarrar")]
    [SerializeField] private Transform handPoint;
    // punto donde se posicionan los objetos agarrados

    private Interactable currentInteractable;
    private GrabbableObject currentGrabbable;
    // guarda referencia al objeto interactuable que el jugador esta mirando actualmente y el agarrado

    private bool interactPressed;
    private GrabbableObject pickedObject;
    // bandera temporal para cuando el input de interactuar se apreto y la otra igual para los agarrados

    GameObject Select;

    private void Update()
    {
        // checkeo que esta mirando
        CheckInteraction();
    }

    public void OnInteract(InputValue value)
    {
        // este metodo llama al componente player input cuando la accion "interact" se ejecuta (si el componente esta configurado en "send messages")

        if (value.isPressed)
        {
            interactPressed = true;
        }
    }

    private void LateUpdate()
    {
        // reseteo la bandera al final del frame para que el interact se procese solo una vez por pulsacion
        interactPressed = false;
    }

    private void CheckInteraction()
    {
        // si el jugador aprieta interactuar y ya tiene algo en la mano, primero suelta ese objeto, porque me da que puede romper todo
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

            SelectedObject(hit.transform);

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

            // si no hay interactuable pero si agarrable, le meto el prompt para agarrar
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
        else
        {
            Deselect();
        }

        // si no estamos mirando nada interactuable
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
        // dejo esto por si en el futuro un interactuable necesita saber si el jugador ya tiene un objeto agarrado
        return pickedObject != null;
    }

    public GameObject GetPickedObject()
    {
        // tiro el gameobject agarrado si existe
        if (pickedObject == null)
        {
            return null;
        }

        return pickedObject.gameObject;
    }

    void SelectedObject(Transform transform)
    {
        // busco renderer en el objeto o en el padre. algunos colliders son hijos pelados,
        // y sin este check se rompe antes de interactuar, me apso
        Renderer renderer = transform.GetComponent<Renderer>();

        if (renderer == null)
        {
            renderer = transform.GetComponentInParent<Renderer>();
        }

        if (renderer == null)
        {
            Select = null;
            return;
        }

        renderer.material.color = Color.green;
        Select = renderer.gameObject;
    }

    void Deselect()
    {
        if (Select != null)
        {
            Renderer renderer = Select.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }

            Select = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * interactionDistance);
    }
}
