using UnityEngine;


public class InteractObject : MonoBehaviour
{
    LayerMask Mask;
    private float DistanceDetection = 3f;
    
    [SerializeField]GameObject TextDetect;
    [SerializeField] Transform HandPoint;

    GameObject Select = null;
    GameObject PickedObect = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mask = LayerMask.GetMask("Raycast Detect");
    }

    // Update is called once per frame
    void Update()
    {
        Interact();
    }

    void Interact()
    {

        /*
         * voy a hacer lo siguiente, acomodo esto:
         * PlayerInteraction tira el raycast, detecta un "Interactable" y muestra el texto "E - Interactuar",
         * si tocas la E, ejecuta la accion el objeto.
         * 
         */

        if (Input.GetKeyDown(KeyCode.E) && PickedObect != null) //si se presiona E y hay un objeto en mano, suelta el objeto
        {
            Debug.Log("Solte objeto: " + PickedObect);

            Rigidbody rb = PickedObect.GetComponent<Rigidbody>();

            rb.useGravity = true;
            rb.isKinematic = false;
            // rb.detectCollisions = true;

            //el sistema de agarrar esta rompiendo en unos temillas, me da paja resolverlo


            PickedObect.gameObject.transform.SetParent(null);

            PickedObect = null;
            return; //evita que el código de detección se ejecute después de soltar el objeto
        }


        RaycastHit hit;
        //raycast( Origen, direccion, out hit, distancia, mascara)
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, DistanceDetection, Mask))
        {
            if (hit.collider.tag == "ObjectInteract")
            {
                Debug.Log("Detecte" + hit.collider.tag);


                //solo cambia si es un objeto distinto
                if (Select != hit.transform.gameObject)
                {
                    Deselect();
                    SelectedObject(hit.transform);
                }

                if (Input.GetKeyDown(KeyCode.E) && PickedObect == null) //si se presiona E y no hay un objeto en mano, recoge el objeto
                {
                    Debug.Log("Objeto en mano: " + PickedObect);

                    GameObject obj = hit.collider.gameObject;
                    Rigidbody rb = obj.GetComponent<Rigidbody>();

                    rb.useGravity = false;
                    rb.isKinematic = true;
                    // rb.detectCollisions = false;

                    obj.transform.position = HandPoint.position;

                    obj.gameObject.transform.SetParent(HandPoint.gameObject.transform);

                    PickedObect = obj.gameObject;
                }
            }
            else
            {
                Deselect();
            }

        }
        else
        {
            Deselect();
        }

        void SelectedObject(Transform transform)
        {
            TextDetect.SetActive(true);
            transform.GetComponent<MeshRenderer>().material.color = Color.green;
            Select = transform.gameObject;

        }
    }
    void Deselect()
    {
        if (Select != null)
        {
            TextDetect.SetActive(false);
            Select.GetComponent<Renderer>().material.color = Color.white;
            Select = null;
        }
    }
}
