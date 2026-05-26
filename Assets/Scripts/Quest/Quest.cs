using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
    public float AddPoints;
    public float RemovePoints;

    // Constructor para inicializar de forma limpia
    public QuestData(StructureQuest.QuestGeneric questConfig)
    {
        config = questConfig;
        isActive = false;
        isComplete = false;
        timer = 0f;
        failPenaltyPoints = config.addPoints * -1.5f;
        AddPoints = config.addPoints;
        RemovePoints = config.removePoints;
    }
    public questEmotionType GetStateType() => config.State;
    public questEmotionType GetEmotionIDType_Add() => config.EmotionID;
    public questEmotionType GetEmotionIDType_Remove() => config.EmotionID_;
}

public class Quest : MonoBehaviour
{
    [Header("Quest Asset")]
    [SerializeField] private StructureQuest questDatabase;

    [Header("Runtime Data")]
    [SerializeField] private List<QuestData> allQuests = new();
    public List<QuestData> allQuests_ { get { return allQuests; } set { allQuests = value; } }
    private QuestData activeQuest; // Mantiene registro de la misión activa actual

    [Header("Settings")]
    [SerializeField] private int timerDuration = 60;
    [SerializeField] private Material transparentMaterial;

    Bars bars;
    private QuestController controller;
    private Renderer rend;
    private Material originalMaterial;
    internal QuestData questData;

    private void Awake()
    {
        bars = FindFirstObjectByType<Bars>();
        controller = FindFirstObjectByType<QuestController>();
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            originalMaterial = rend.material;
        }
    }

    private void Start()
    {
        if (questDatabase != null)
        {

            for (int i = 0; i < questDatabase.quests.Length; i++)
            {
                AddQuest(new QuestData(questDatabase.quests[i]));
            }
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
            Debug.Log(activeQuest.timer);

            if (checkTimer())
            {
                FailQuest();
            }
        }
    }

    public void AddQuest(QuestData questData)
    {
        allQuests.Add(questData);

        if (controller != null)
        {
            List<Quest> quest = allQuests.Select(datoA =>{return this;}).ToList();

            controller.Initialize(quest);
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
    public bool getIsActive()
    {
        if (activeQuest != null && !activeQuest.isActive)
        {
            return activeQuest.isActive;
        }
        else return !activeQuest.isActive;
    }

    public void setActive(bool value)
    {
        if (activeQuest == null) return;

        activeQuest.isActive = value;
        if (activeQuest.isActive)
        {
            setTimer();
        }
    }
    public bool getIsCompleted() => activeQuest != null && activeQuest.isComplete;

    public void setIsCompleted(bool value)
    {
        if (activeQuest != null) activeQuest.isComplete = value;
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



    public void setTimer()
    {
        if (activeQuest != null)
        {   
            if (checkTimer())
            {
                if (activeQuest.isActive)
                {
                    bars.QuestFinished(questData.GetEmotionIDType_Add(), (int)questData.failPenaltyPoints);
                    FailQuest();
                }
                else
                {
                    setIsCompleted(true);

                    bars.QuestFinished(questData.GetEmotionIDType_Add(), (int)questData.AddPoints);
                    bars.QuestFinished(questData.GetEmotionIDType_Remove(), -(int)questData.RemovePoints);

                }
            }
        }
    }

    public float getTimer() => activeQuest != null ? activeQuest.timer : 0f;

    public bool checkTimer() => activeQuest != null && activeQuest.timer >= timerDuration;

    public float getTimerDuration() => timerDuration;

}