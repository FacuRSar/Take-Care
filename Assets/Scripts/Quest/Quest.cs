using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.Audio.ProcessorInstance;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class QuestData
{
    [SerializeField]Quest quest;

    // Al guardar la referencia directa, ya tienes acceso a TODO (Items, Emociones, Destinos)
    public StructureQuest.QuestGeneric config;

    // Variables de estado dinámicas (cambian en tiempo de juego)
    public int QuestID;

    public bool isActive;
    public bool isComplete;
    public float timer;

    public float failPenaltyPoints;
    public float AddPoints;
    public float RemovePoints;

    public String roomID;

    // Constructor para inicializar de forma limpia
    public QuestData(StructureQuest.QuestGeneric questConfig)
    {
        config = questConfig;
        isActive = false;
        isComplete = false;
        QuestID = config.id;
        timer = 0f;
        failPenaltyPoints = config.addPoints * -1.5f;
        AddPoints = config.addPoints;
        RemovePoints = config.removePoints;

        roomID = config.roomID;
    }
    public questType GetQuestType() => config.QuestType;
    public questEmotionType GetStateType() => config.State;
    public questEmotionType GetEmotionIDType_Add() => config.EmotionID;
    public questEmotionType GetEmotionIDType_Remove() => config.EmotionID_;

    public List<StructureQuest.QuestGeneric.itemsToPick> ItemsToPick() => config.itemsToPickData;

}

public class Quest : MonoBehaviour
{
    [Header("Quest Asset")]
    [SerializeField] private StructureQuest questDatabase;

    [Header("Runtime Data")]
    [SerializeField] private List<QuestData> allQuests = new();
    public List<QuestData> allQuests_ { get { return allQuests; } set { allQuests = value; } }
    private QuestData activeQuest;

    [Header("Settings")]
    [SerializeField] private float distanceMin = 10000f;
    [SerializeField] private int timerDuration = 60;
    [SerializeField] private Material transparentMaterial;

    Bars bars;

    [SerializeField] private Transform player;
    private QuestController controller;
    private Renderer rend;
    private Material originalMaterial;
    internal QuestData questData;

    Transform Room;


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
                allQuests.Add(new QuestData(questDatabase.quests[i]));
            }
            AddQuest();
        }

        
    }

    private void FixedUpdate()
    {
        if (Room != null) distanceRoom();
    }
    private void Update()
    {
        if (activeQuest != null && activeQuest.isActive)
        {
            activeQuest.timer += Time.deltaTime;

            if (checkTimer())
            {
                bars.QuestFinished(questData.GetEmotionIDType_Add(), (int)questData.failPenaltyPoints);
                FailQuest();
            }
        }
    }

    public void AddQuest()
    {
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
            questData = activeQuest;
            activeQuest.isActive = true;
            activeQuest.timer = 0f;

            Debug.LogWarning(activeQuest.QuestID);

            Rooms();
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
            //setTimer();
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
            Debug.Log("Quest fallida:" + activeQuest.config.Name);
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

    public float getTimer() => activeQuest != null ? activeQuest.timer : 0f;

    public bool checkTimer() => activeQuest != null && activeQuest.timer >= timerDuration;

    public float getTimerDuration() => timerDuration;


    public void TypeQuest()
    {
        switch(questData.GetQuestType())
        {
            case questType.ToCollect:
                LogicToCollect();       
                break;
            case questType.ToGo:
                LogicToGo();
                break;
            case questType.ToDelivery:
                LogicToDelivery();
                break;
        }
    }


    private void LogicToCollect()
    {
        bars.QuestFinished(questData.GetEmotionIDType_Add(), (int)questData.failPenaltyPoints);
        FailQuest();
    }
    private void LogicToGo()
    {
        if (activeQuest == null) return;
        if (Room == null)
        {
            Debug.LogWarning("No hay un desino asignado");
            return;
        }

        float distance = Vector3.Distance(player.transform.position, Room.transform.position);

        distanceRoom();

    }

    private void distanceRoom()
    {
        float distance = Vector3.Distance(player.transform.position, Room.transform.position);

        Debug.Log(distance);
        if (distance < distanceMin)
        {
            CompleteActiveQuest();
        }
    }
    private void LogicToDelivery()
    {
        if (activeQuest == null) return;
        if (activeQuest.GetQuestType() != questType.ToDelivery) return;

        // Si el destino está asignado, comprobamos proximidad del player

        if (Room == null)
        {
            Debug.LogWarning($"Delivery sin destino en {activeQuest.config.Name}");
            return;

        }

        List<StructureQuest.QuestGeneric.itemsToPick> listItems =
            new List<StructureQuest.QuestGeneric.itemsToPick>();

        listItems.AddRange(questData.ItemsToPick());

        for (int i = 0; i < listItems.Count; i++)
        {
            GameObject obj = listItems[i].gameObject;

            if (obj == null) continue;

            float distance = Vector3.Distance(obj.transform.position, Room.position);

            if (distance <= distanceMin)
            {
                CompleteActiveQuest();
                return;
            }
        }
    }

    private void CompleteActiveQuest()
    {
        if (activeQuest == null) return;

        activeQuest.isActive = false;
        activeQuest.isComplete = true;

        bars.QuestFinished(activeQuest.GetEmotionIDType_Add(), (int)activeQuest.AddPoints);
        bars.QuestFinished(activeQuest.GetEmotionIDType_Remove(), -(int)activeQuest.RemovePoints);
    }

    private void Rooms()
    {
        if (questData == null)
        {
            Debug.LogWarning("Rooms(): questData es null, no se puede asignar Room.");
            return;
        }

        Piece[] allPieces = FindObjectsByType<Piece>(FindObjectsSortMode.None);
        Piece piece = allPieces.FirstOrDefault(p => p.id == questData.roomID);

        if (piece == null)
        {
            Debug.LogWarning($"Rooms(): no encontré ninguna pieza con ID {questData.roomID}");
            return;
        }

        Room = piece.transform;
    }

}