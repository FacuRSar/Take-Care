using UnityEngine;


public class InteractObject : MonoBehaviour
{
    LayerMask Mask;
    private float DistanceDetection = 3f;
    
    [SerializeField]GameObject TextDetect;
    GameObject Select = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mask = LayerMask.GetMask("Raycast Detect");
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        //Raycast( Origen, direccion, out hit, distancia, mascara)

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, DistanceDetection, Mask))
        {


            if (hit.collider.tag == "ObjectInteract")
            {
                // Solo cambia si es un objeto distinto
                if (Select != hit.transform.gameObject)
                {
                    Deselect();
                    SelectedObject(hit.transform);
                }

                if (Input.GetKeyDown(KeyCode.E))
                    {
                        hit.collider.GetComponent<ObjectFuntion>().Interact();
                    }
                
            }
        }
        else
        {
            Deselect();
        }

    }

    void SelectedObject(Transform transform)
    {
        TextDetect.SetActive(true);
        transform.GetComponent<MeshRenderer>().material.color = Color.green;
        Select = transform.gameObject;

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
