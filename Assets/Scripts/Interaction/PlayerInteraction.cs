using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Image = UnityEngine.UI.Image;

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

    [SerializeField] private GrabSettings grabSettings = new GrabSettings();
    // config compartida (distancia min/max, paso de rueda, sensibilidad de giro) para todos los objetos

    private Interactable currentInteractable;
    private GrabbableObject currentGrabbable;
    // guarda referencia al objeto interactuable que el jugador esta mirando actualmente y el agarrado

    private bool interactPressed;
    [SerializeField] private GrabbableObject pickedObject;

    public GrabbableObject PickedObject => pickedObject;
    // bandera temporal para cuando el input de interactuar se apreto y la otra igual para los agarrados

    GameObject Select;

    [Header("Inventario")]
    [SerializeField] private Transform InventoryContent;
    [SerializeField] private GameObject ItemIconPrefab_1;
    [SerializeField] private GameObject ItemIconPrefab_2;
    [SerializeField] private GameObject ItemIconPrefab_3;


    private GrabbableObject[] slots = new GrabbableObject[3];
    private GameObject[] UiInventory = new GameObject[3];

    public GrabbableObject[] Slots => slots;

    private GameObject[] UiItem = new GameObject[3];

    int Slot;

    private void Update()
    {
        // checkeo que esta mirando
        CheckInteraction();


        InputNumSlot();
    }

    private void InputNumSlot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Slot = 1;
            if (OccupiedSlot_1)
            {
                RemoveToInventory(Slot);
            }
            else if (!OccupiedSlot_1 && HasObjectInHand())
            {
                AddToInventory(pickedObject, Slot);
            }
            else
            {
                Debug.Log($"No Tenes ningun obj en la mano ni en el {Slot}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Slot = 2;
            if (OccupiedSlot_2)
            {
                RemoveToInventory(Slot);
            }
            else if (!OccupiedSlot_2 && HasObjectInHand())
            {
                AddToInventory(pickedObject, Slot);
            }
            else
            {
                Debug.Log($"No Tenes ningun obj en la mano ni en el {Slot}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Slot = 3;
            if (OccupiedSlot_3)
            {
                RemoveToInventory(Slot);
            }
            else if (!OccupiedSlot_3 && HasObjectInHand())
            {
                AddToInventory(pickedObject, Slot);
            }
            else
            {
                Debug.Log($"No Tenes ningun obj en la mano ni en el {Slot}");
            }
        }
        else return;
    }
    private void AddToInventory(GrabbableObject item, int slot)
    {
        int index = slot - 1;

        if (slots[index] != null)
        {
            Debug.Log($"El slot {slot} ya está ocupado");
            return;
        }


        slots[index] = item;
        GameStateController.Instance.SetFlag(slots[index].objectID.ToString());
        slots[index].transform.SetParent(handPoint);

        switch (slot)
        {
            case 1:
                UiItem[index] = Instantiate(ItemIconPrefab_1, InventoryContent);
                break;
            case 2:
                UiItem[index] = Instantiate(ItemIconPrefab_2, InventoryContent);
                break;
            case 3:
                UiItem[index] = Instantiate(ItemIconPrefab_3, InventoryContent);
                break;
        }

        Image image = UiItem[index].GetComponent<Image>();
        image.sprite = item.itemIcon;

        UiInventory[index] = UiItem[index];

        item.gameObject.SetActive(false);
        pickedObject = null;
    }

    private void RemoveToInventory(int slot)
    {
        int index = slot - 1;

        if (slots[index] == null)
        {
            Debug.Log($"El slot {slot} está vacío");
            return;
        }

        ForceToObject();

        GameStateController.Instance.RemoveFlag(slots[index].objectID.ToString());

        slots[index].transform.SetParent(null);
        slots[index].gameObject.SetActive(true);

        pickedObject = slots[index].gameObject.GetComponent<GrabbableObject>();

        slots[index] = null;

        Destroy(UiInventory[index]);
        UiInventory[index] = null;

    }

    // Fuerza soltar el objeto en mano
    // Nota: la lógica de "agarrado" está en la clase GrabbableObject (métodos PickUp, Drop, InputNumSlot).
    private void ForceToObject()
    {
        if (pickedObject != null)
        {
            // Si el jugador tiene un objeto en mano, forzamos su soltado.
            pickedObject.Drop();
            pickedObject = null;
            return;
        }
        else
        {
            Debug.Log("No hay objeto en mano para forzar su soltado.");
        }
    }

    public bool OccupiedSlot_1 => slots[0] != null;
    public bool OccupiedSlot_2 => slots[1] != null;
    public bool OccupiedSlot_3 => slots[2] != null;

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

                // objeto marcado como no agarrable (ej: pesado): el prompt sigue normal
                // y el mensaje de "pesa demasiado" solo sale como subtitulo al apretar E
                if (!grabbable.CanBeGrabbed)
                {
                    ShowPrompt("E - Interactuar");

                    if (interactPressed && SubtitleUI.Instance != null)
                    {
                        SubtitleUI.Instance.ShowSubtitle(grabbable.CannotGrabMessage, 2.5f);
                    }
                    return;
                }

                ShowPrompt("E - Agarrar");

                if (interactPressed && pickedObject == null)
                {
                    pickedObject = grabbable;
                    int heldLayer = LayerMask.NameToLayer(heldObjectLayerName);
                    if (heldLayer == -1) heldLayer = grabbable.gameObject.layer;
                    pickedObject.PickUp(handPoint, heldLayer, grabSettings);
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

    public void NotifyObjectDropped(GrabbableObject obj)
    {
        // lo llama el objeto cuando se suelta solo (ej: quedo trabado y se alejo demasiado),
        // asi el jugador deja de considerarlo "en mano"
        if (pickedObject == obj)
        {
            pickedObject = null;
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
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * interactionDistance);
    }
}
