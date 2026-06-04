using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TreeEditor;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.Audio.ProcessorInstance;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.STP;

[System.Serializable]
public class QuestData
{
    [SerializeField]Quest quest;

    // Al guardar la referencia directa, ya tienes acceso a TODO (Items, Emociones, Destinos)
    public StructureQuest.QuestGeneric config;

    // Variables de estado dinámicas (cambian en tiempo de juego)
    public int QuestID;
    public String QuestName;

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
        QuestName = config.Name;
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
    [SerializeField] private RandomObjectPositioner randomObjectPositioner;

    [Header("Runtime Data")]
    [SerializeField] private List<QuestData> allQuests = new();
    public List<QuestData> allQuests_ { get { return allQuests; } set { allQuests = value; } }
    private QuestData activeQuest;

    [Header("Settings")]
    [SerializeField] private float distanceMin;
    [SerializeField] private int timerDuration = 60;
    [SerializeField] private Material transparentMaterial;

    Bars bars;
    PlayerInteraction playerInteraction;

    [SerializeField] private Transform player;
    private QuestController controller;
    private Renderer rend;
    private Material originalMaterial;
    internal QuestData questData;

    Transform Room;
    Transform Player;

    List<Transform> Obj_ = new List<Transform>();
    List<int> ObjId = new List<int>();
    List<float> distanceList = new List<float>();

    int ObjInventory;
    private void OnEnable()
    {
        SpawnObj();
    }
    private void Awake()
    {
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();
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
        Player = transform;

        if (activeQuest == null || !activeQuest.isActive) return;

        switch (questData.GetQuestType())
        {
            case questType.ToCollect:
                LogicToCollect(); // Si llegamos la agragamos
                break;
            case questType.ToGo:
                LogicToGo();
                break;
            case questType.ToDelivery:
                LogicToDelivery();
                break;
        }
    }
    private void Update()
    {
        if (activeQuest == null || !activeQuest.isActive) return;
        
        activeQuest.timer += Time.deltaTime;
        if (checkTimer())
        {
            bars.QuestFinished(questData.GetEmotionIDType_Add(), (int)questData.failPenaltyPoints);
            FailQuest();
        }
        
    }


    private void _AddQuest()
    {
        if (controller == null) return;

        controller.Initialize(allQuests);
    }
    public void AddQuest() 
    {
        _AddQuest();
    }

    private void _ActivateQuest(int index)
    {
        if (index >= 0 && index < allQuests.Count)
        {
            activeQuest = allQuests[index];
            questData = activeQuest;
            activeQuest.isActive = true;
            activeQuest.timer = 0f;

            Debug.LogWarning($"Quest Activated: ID = {activeQuest.QuestID}, Name = {activeQuest.QuestName}");

            Rooms();
            Obj();
        }
    }
    public void ActivateQuest(int index)
    {
        _ActivateQuest(index);
    }

    private bool _getIsActive()
    {
        if (activeQuest != null && !activeQuest.isActive)
        {
            return activeQuest.isActive;
        }
        else return !activeQuest.isActive;
    }
    public void getIsActive() 
    {
        _getIsActive();
    }

    private void setActive(bool value)
    {
        _setActive(value);
    }
    public void _setActive(bool value)
    {
        if (activeQuest == null) return;

        activeQuest.isActive = value;

        if (activeQuest.isActive)
        {
            //setTimer();
        }
    }


    private bool getIsCompleted() => activeQuest != null && activeQuest.isComplete;

    public bool _getIsCompleted()
    {
        return getIsCompleted();
    }

    private void setIsCompleted(bool value)
    {
        if (activeQuest != null) activeQuest.isComplete = value;
    }
    private void FailQuest()
    {
        if (activeQuest == null) return;

        activeQuest.isActive = false;
        Debug.Log("Quest fallida:" + activeQuest.config.Name);
    }
    
    public void _FailQuest()
    {
        FailQuest();
    }

    private void MarkObjective()
    {
        StartCoroutine(TempVisibility());
    }
    public void _MarkObjective()
    {
        MarkObjective();
    }

    private IEnumerator TempVisibility()
    {
        if (rend == null || transparentMaterial == null || originalMaterial == null)
            yield break;

        rend.material = transparentMaterial;
        yield return new WaitForSeconds(2);
        rend.material = originalMaterial;
    }

    private float getTimer() => activeQuest != null ? activeQuest.timer : 0f;
    public float _getTimer()
    {
        return getTimer();
    }

    private bool checkTimer() => activeQuest != null && activeQuest.timer >= timerDuration;
    public bool _checkTimer()
    {
        return checkTimer();
    }

    private float getTimerDuration() => timerDuration;
    public float _getTimerDuration()
    {
        return getTimerDuration();
    }

    private void LogicToCollect()
    {
        if (activeQuest == null) return;
        if (activeQuest.GetQuestType() != questType.ToCollect) return;
        if (playerInteraction == null) return;

        // Recalcular cuántos items requeridos están en el inventario del jugador
        var requiredItems = questData.ItemsToPick();// Lista de items requeridos por la quest
        int totalRequired = requiredItems.Sum(it => it.quantity);// Cantidad total de items requeridos por la quest
        int collectedCount = 0;

        foreach (var req in requiredItems)
        {
            int itemID = req.itemID;
            int requiredQty = req.quantity;
            int inInventory = playerInteraction.Slots.Count(p => p != null && p.objectID == itemID);
            collectedCount += Math.Min(inInventory, requiredQty);
        }

        Debug.Log($"Collected {collectedCount} / {totalRequired} for quest '{activeQuest.QuestName}'");

        if (collectedCount >= totalRequired)
        {
            CompleteActiveQuest();
        }
    }
    private void LogicToGo()
    {
        if (activeQuest == null) return;
        if (activeQuest.GetQuestType() != questType.ToGo) return;
        if (Room == null)
        {
            Debug.LogWarning("No hay un desino asignado");
            return;
        }

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

        distanceList = new List<float>(new float[Obj_.Count]); // Reiniciar la lista de distancias para cada objeto
        int ObjInRoom = 0;

        for (int i = 0; i < Obj_.Count; i++)
        {
            float distance = Vector3.Distance(Obj_[i].gameObject.transform.position, Room.position);
            //Debug.Log($"Distancia entre {Obj_[i].gameObject.name} y destino: {distance}");

            distanceList[i] = distance;

            if (distanceList[i] <= distanceMin)
            {
                ObjInRoom++;
            }
        }


        if (ObjInRoom >= questData.ItemsToPick().Count)
        {
            CompleteActiveQuest();
            ObjInRoom = 0;
            return;
        }
    }

    private void CompleteActiveQuest()
    {
        if (activeQuest == null) return;


        activeQuest.isActive = false;
        activeQuest.isComplete = true;

        Debug.Log("Quest completada: " + activeQuest.config.Name);

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
        Piece piece = allPieces.FirstOrDefault(p => p.Id == questData.roomID);

        if (piece == null)
        {
            Debug.LogWarning($"Rooms(): no encontré ninguna pieza con ID {questData.roomID}");
            return;
        }

        Room = piece.transform;
    }
    private void Obj()
    {
        if (questData == null)
        {
            Debug.LogWarning("Obj(): questData es null, no se puede asignar Obj.");
            return;
        }
        Obj_.Clear();
        ObjId.Clear();

        List<StructureQuest.QuestGeneric.itemsToPick> listItems = questData.config.itemsToPickData;
        GrabbableObject[] allObj = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);


        for (int i = 0; i < questData.ItemsToPick().Count; i++) 
        {

            int itemID = listItems[i].itemID;

            GrabbableObject objInventory = playerInteraction.Slots.FirstOrDefault(p => p != null && itemID == p.objectID);
            GrabbableObject objScene = allObj.FirstOrDefault(p => itemID == p.objectID);

            if (objInventory != null)
            {
                if (activeQuest.GetQuestType() == questType.ToGo)
                {
                    ObjScena(itemID, objInventory);
                }
                else if (activeQuest.GetQuestType() == questType.ToCollect)
                {
                    ObjCollect(itemID, objInventory);
                }
                else
                {
                    Debug.LogWarning($"metodo incorrecto, no se deberia llamar a este metodo en este tipo de Quest");
                    return;
                }

            }
            else if (objScene != null)
            {
                Debug.Log($"Objeto encontrado para el ID {itemID}: {objScene.name}");
                Obj_.Add(objScene.transform);
                ObjId.Add(itemID);
            }
            else
            {
                // Si no lo encuentra, avisa en consola pero el juego Sigue corriendo bien
                Debug.LogWarning($"No se encontró en la escena ni en el inventario el objeto con el ID: {itemID}");
            }
        }

    }

    private void ObjScena(int itemID, GrabbableObject objInventory)
    {
        Debug.Log($"Objeto encontrado para el ID {itemID}: {objInventory.name}");
        Obj_.Add(objInventory.transform);
    }
    private void ObjCollect(int itemID, GrabbableObject objInventory)
    {
        Debug.Log($"Objeto encontrado para el ID {itemID}: {objInventory.name}");
        Obj_.Add(objInventory.transform);
        ObjId.Add(itemID);
        ObjInventory++;
    }

    private void SpawnObj()
    {
        List<StructureQuest.QuestGeneric.itemsToPick> listItems = new();

        for (int i = 0; i < questDatabase.quests.Length; i++)
        {
            listItems.AddRange(questDatabase.quests[i].itemsToPickData); // Tengo que verlo todavia
        }

        for (int j = 0; j < listItems.Count; j++)
        {
            //Debug.Log ($"Spawned {listItems[j].quantity} item ID {listItems[j].itemID}");
            Debug.Log("Item actual [" + j + "]: " + listItems[j]);
            randomObjectPositioner._ObjAdd(listItems[j]);
            Debug.Log($"Spawned {listItems[j].quantity} item ID {listItems[j].itemID}");
        }   
    }
}