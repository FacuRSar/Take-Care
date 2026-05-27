using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestData
{
    // Al guardar la referencia directa, ya tienes acceso a TODO (Items, Emociones, Destinos)
    public StructureQuest.QuestGeneric config;

    // Variables de estado dinámicas (cambian en tiempo de juego)
    public bool isActive;
    public bool isComplete;
    public float timer;
    public float failPenaltyPoints;

    // Constructor para inicializar de forma limpia
    public QuestData(StructureQuest.QuestGeneric questConfig)
    {
        config = questConfig;
        isActive = false;
        isComplete = false;
        timer = 0f;
        failPenaltyPoints = questConfig.addPoints * -1.5f;
    }
    public questEmotionType GetStateType() => config.State;
}

public class Quest : MonoBehaviour
{
    [Header("Quest Asset")]
    [SerializeField] private StructureQuest questDatabase;

    [Header("Runtime Data")]
    [SerializeField] private List<QuestData> allQuests = new();
    private QuestData activeQuest; // Mantiene registro de la misión activa actual

    [Header("Settings")]
    [SerializeField] private int timerDuration = 60;
    [SerializeField] private Material transparentMaterial;

    private QuestController controller;
    private Renderer rend;
    private Material originalMaterial;

    private void Awake()
    {
        controller = FindFirstObjectByType<QuestController>();
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            originalMaterial = rend.material;
        }
    }

    private void Start()
    {
        // El flujo correcto: La escena se inicializa a sí misma usando el asset
        if (questDatabase != null)
        {
            Initialize(questDatabase.quests);
        }
        else
        {
            Debug.LogWarning($"Falta asignar el ScriptableObject 'Quest Database' en {gameObject.name}");
        }
    }

    private void Update()
    {
        if (activeQuest != null && activeQuest.isActive)
        {
            activeQuest.timer += Time.deltaTime;

            if (activeQuest.timer >= timerDuration)
            {
                FailQuest();
            }
        }
    }

    public void Initialize(StructureQuest.QuestGeneric[] quests)
    {
        allQuests.Clear();

        for (int i = 0; i < quests.Length; i++)
        {
            allQuests.Add(new QuestData(quests[i]));
        }

        // Le paso la lista de misiones procesadas al controlador
        if (controller != null)
        {
            controller.Initialize(allQuests);
        }
    }

    public void ActivateQuest(int index)
    {
        if (index >= 0 && index < allQuests.Count)
        {
            activeQuest = allQuests[index];
            activeQuest.isActive = true;
            activeQuest.timer = 0f;
        }
    }

    public void FailQuest()
    {
        if (activeQuest != null)
        {
            activeQuest.isActive = false;
            Debug.Log($"Quest fallida: {activeQuest.config.Name}");
        }
    }

    public void MarkObjective()
    {
        StartCoroutine(TempVisibility());
    }

    private IEnumerator TempVisibility()
    {
        if (rend == null || transparentMaterial == null || originalMaterial == null)
            yield break;

        rend.material = transparentMaterial;
        yield return new WaitForSeconds(2);
        rend.material = originalMaterial;
    }

    // GETTERS, SETTERS Y CONTROL DE ESTADO
    public bool getIsCompleted() => activeQuest != null && activeQuest.isComplete;

    public void setIsCompleted(bool value)
    {
        if (activeQuest != null) activeQuest.isComplete = value;
    }

    public bool getIsActive() => activeQuest != null && activeQuest.isActive;

    public void setActive(bool value)
    {
        if (activeQuest == null) return;

        activeQuest.isActive = value;
        if (activeQuest.isActive)
        {
            setTimer();
        }
    }

    public void setTimer()
    {
        if (activeQuest != null) activeQuest.timer = 0f;
    }

    public float getTimer() => activeQuest != null ? activeQuest.timer : 0f;

    public bool checkTimer() => activeQuest != null && activeQuest.timer >= timerDuration;

    public float getTimerDuration() => timerDuration;

}