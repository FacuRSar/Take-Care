using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private StructureQuest.Quest data; // Datos serializados de la misión (ID, nombre, descripción, etc.)

    [Header("Runtime Status")] // estado en tiempo de ejecución
    private bool isActive;   // true si la misión está activa
    private bool isComplete; // true si la misión se ha completado
    private float timer;     // temporizador interno para la misión

    [SerializeField] private int timerDuration = 60; // duración en segundos antes de que el temporizador considere la misión fallida o expirada

    [Header("Visual Settings")]
    private Renderer rend;             // renderer del objeto para cambiar materiales
    private Material originalMaterial; // material original para restaurar
    [SerializeField] private Material transparentMaterial; // material temporal para destacar la misión

    [Header("References")]
    [SerializeField] private Player_Health playerHealth;

    [Header("Scene Objects")]
    [SerializeField] GameObject Destiny; // objeto destino/marker en la escena relacionado con la misión
    [SerializeField] GameObject Room;    // referencia a la sala de la misión

    private void Awake()
    {
        // Obtener el renderer y guardar el material original para poder restaurarlo después
        rend = GetComponent<MeshRenderer>();
        if (rend != null) originalMaterial = rend.material;

        // inicializar estado
        isComplete = false;
        isActive = false;
    }

    // Inicializa los datos de la misión desde una estructura externa
    public void Initialize(StructureQuest.Quest questData)
    {
        this.data = questData;
    }

    // Devuelve el nombre de la quest guardado en data
    public string getName() => data.Name;

    // Devuelve el ID de la quest guardado en data
    public int getID() => data.id;

    // Devuelve la descripción de la quest.
    // Si está vacía o es null, devuelve "No description"
    public string getDescription() => !string.IsNullOrEmpty(data.description) ? data.description : "No description";

    // Devuelve si la quest está completada o no
    public bool getIsCompleted() => isComplete;

    // Cambia el estado de completado de la quest
    public void setIsCompleted(bool value) => isComplete = value;

    private void Update()
    {
        // Si la misión está activa, avanzar el temporizador
        if (isActive)
        {
            timer += Time.deltaTime;
        }
    }

    // Activar/desactivar la misión. Al activarla se resetea el temporizador y el estado de completado.
    public void setActive(bool newIsActive)
    {
        setIsCompleted(false);
        isActive = newIsActive;
        if (isActive)
        {
            setTimer();
        }
    }

    public bool getIsActive() => isActive;
    public void setTimer() => timer = 0; // reinicia el temporizador
    public float getTimer() => timer;

    // Comprueba si el temporizador ha alcanzado o superado la duración configurada
    public bool checkTimer()
    {
        if (getTimer() >= timerDuration)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public float getTimerDuration() => timerDuration;

    // Coroutine que aplica un material transparente temporalmente para marcar el objetivo
    IEnumerator TempVisibility()
    {
        if (rend == null || transparentMaterial == null || originalMaterial == null)
            yield break;

        rend.material = transparentMaterial;
        yield return new WaitForSeconds(2);
        rend.material = originalMaterial;
    }

    // Inicia la animación/efecto visual para marcar el objetivo de la misión
    public void markObjective()
    {
        StartCoroutine(TempVisibility());
    }

    // Punto de extensión para manejar la lógica cuando la misión falla (ej. notificar, resetear, penalizar al jugador)
    public void failQuest()
    {
        // Implementar comportamiento de fallo de misión aquí
    }
}