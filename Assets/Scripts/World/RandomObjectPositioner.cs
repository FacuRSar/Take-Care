using UnityEngine;
using System.Collections.Generic;


public class RandomObjectPositioner : MonoBehaviour
{
    [SerializeField] private List<Transform> Position;
    [SerializeField] private List<GameObject> Obj;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        objValidator();
    }
    void Start()
    {
        ObjRemove();
    }


    private void objValidator()
    {
        foreach (GameObject Object in Obj)
        {
            Debug.Log("Validando el objeto: " + Object.name);

            if (Object.TryGetComponent(out GrabbableObject grabbable))
            {
                if (!grabbable.IsAssigned)
                {
                    grabbable.IsAssigned = true;
                    int Index = Random.Range(0, Position.Count);

                    Object.transform.position = Position[Index].position;
                }
            }
        }
    }

    private void ObjRemove ()
    {
        for (int i = Obj.Count - 1; i >= 0; i--)
        {
            if (Obj[i].TryGetComponent(out GrabbableObject grabbable))
            {
                if (grabbable.IsAssigned)
                {
                    Debug.Log("Object " + Obj[i].name + " Fue asignado a una posicion y eliminado de la lista.");
                    Obj.RemoveAt(i); // Elimina el objeto de la lista Obj en base a su índice
                }
            }
        }
    }
}
