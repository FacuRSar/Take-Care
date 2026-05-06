using System.Collections.Generic;
using UnityEngine;



public class RandomObjectPositioner : MonoBehaviour
{
    [Header("Position Settings")]

    [SerializeField] private List<Transform> Position;
    [SerializeField] private List<GameObject> Obj;


    [Header("Weight Settings")]

    [SerializeField] private List<float> Weights; // Lista de probabilidades para cada posición, debe tener la misma cantidad de elementos que la lista Position

    [SerializeField] private float WeightMin;// Valor mínimo para la probabilidad de una posición, para evitar que se vuelva completamente imposible seleccionar esa posición
    [SerializeField] private float NewWeight; // Valor para reducir la probabilidad de una posición después de ser seleccionada

    private int Index;


    [Header("Repeat Settings")]

    [SerializeField] private bool CantRepeatPositions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        MatchList();
        objValidator();        
        ObjRemove();
    }
    private void objValidator()
    {
        foreach (GameObject Object in Obj)
        {
            Debug.Log("Validando el objeto: " + Object.name);

            if (Object.TryGetComponent(out GrabbableObject grabbable))
            {
                if (CantRepeatPositions) // Si no se pueden repetir posiciones, asigna una posición aleatoria y elimina esa posición de la lista
                {
                    Index = Random.Range(0, Position.Count);

                    Instantiate(Object, Position[Index].position, Position[Index].rotation);

                    Position.RemoveAt(Index); // Elimina la posición asignada de la lista Position
                }
                else // Si se pueden repetir posiciones, asigna una posición aleatoria y reduce la probabilidad de esa posición para futuras asignaciones
                {
                    int GetRandomIndex()  // Variable para almacenar el índice de la posición seleccionada
                    {
                        float WeightTotal = 0;

                        // Suma las probabilidades para obtener el total
                        foreach (float weight in Weights)
                        {
                            WeightTotal += weight;
                        }

                        // Genera un número aleatorio entre 0 y el total de probabilidades
                        float randomValue = Random.Range(0, WeightTotal);

                        float cumulativeWeight = 0; // Variable para acumular las probabilidades

                        // Recorre las posiciones y sus probabilidades
                        for (int i = 0; i < Weights.Count; i++)
                        {
                            cumulativeWeight += Weights[i]; // Acumula la probabilidad actual

                            if (cumulativeWeight >  randomValue) // Verifica si el número aleatorio es menor que la probabilidad acumulada
                            {
                                return i; // Asigna el índice de la posición seleccionada
                            }
                        }

                        Debug.Log("Nota: Asegúrate de que las probabilidades en la lista Weights estén configuradas correctamente para evitar problemas en la selección de posiciones.");
                        return 0; // Devuelve un índice predeterminado en caso de que no se seleccione ninguna posición (esto no debería ocurrir si las probabilidades son correctas)
                        
                    }

                    Index = GetRandomIndex(); // Obtiene un nuevo índice de posición para el siguiente objeto

                    Instantiate(Object, Position[Index].position, Position[Index].rotation);

                    Weights[Index] = Mathf.Max(WeightMin, Weights[Index] * NewWeight); // Reduce la probabilidad de la posición seleccionada
                }
            }
            else
                Debug.LogWarning("El objeto " + Object.name + " no tiene el componente GrabbableObject y no se asignará a ninguna posición.");
        }
    }

    private void MatchList()
    {
        while (Weights.Count < Position.Count && !CantRepeatPositions)
        {
            Weights.Add(1f); // Agrega un valor predeterminado a la lista de probabilidades si es necesario
        }
    }

    private void ObjRemove ()
    {
        for (int i = Obj.Count - 1; i >= 0; i--)
        {
            if (Obj[i].TryGetComponent(out GrabbableObject grabbable) && CantRepeatPositions)
            {
                Debug.Log("Object " + Obj[i].name + " Fue asignado a una posicion y eliminado de la lista.");
                Obj.RemoveAt(i); // Elimina el objeto de la lista Obj en base a su índice

            }
        }
    }
}
