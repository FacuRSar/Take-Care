using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

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
    public float TimerDuration;
    public string SubtitlesForQuest;
    public string SubtitlesForQuestComplete;
    public string SubtitlesForQuestFail;
    public string SubtitlesForQuestHalf;

    public float failPenaltyPoints;
    public float AddPoints;
    public float RemovePoints;

    // puntos que suma al progreso de escape al completarse
    public int CompletePoints;
    // para no disparar el aviso de "mitad" mas de una vez por mision
    public bool halfNotified;

    public String TpDollID;

    public String roomID;

    // Constructor para inicializar de forma limpia
    public QuestData(StructureQuest.QuestGeneric questConfig)
    {
        config = questConfig;
        isActive = false;
        isComplete = false;
        QuestID = config.id;
        QuestName = config.Name;
        timer = 0;
        TimerDuration = config.timer;
        TpDollID = config.TpDollID;
        SubtitlesForQuest = config.SubtitlesForQuest;
        SubtitlesForQuestComplete = config.SubtitlesForQuestComplete;
        SubtitlesForQuestFail = config.SubtitlesForQuestFail;
        SubtitlesForQuestHalf = config.SubtitlesForQuestHalf;
        failPenaltyPoints = config.addPoints * -1.5f;
        AddPoints = config.addPoints;
        RemovePoints = config.removePoints;
        CompletePoints = config.completePoints;
        halfNotified = false;
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
    [Header("Refence")]
    [SerializeField] private StructureQuest questDatabase;
    [SerializeField] private RandomObjectPositioner randomObjectPositioner;
    private QuestController controller;
    private DialogueController dialogue;
    private QuestData activeQuest;
    private PlayerInteraction playerInteraction;
    private PlayerMovement playerMovement;
    private InGameSequenceController flow;


    [Header("Runtime Data")]
    [SerializeField] private List<QuestData> allQuests = new();
    public List<QuestData> allQuests_ { get { return allQuests; } set { allQuests = value; } }

    [Header("Settings")]
    [SerializeField] private float distanceMin;
    [SerializeField] private Material transparentMaterial;

    Bars bars;

    [SerializeField] private Transform player;
    private Renderer rend;
    private Material originalMaterial;


    Transform Room;

    [SerializeField] private Transform Doll;
    [SerializeField] private GameObject Doll_;

    List<Transform> Obj_ = new List<Transform>();
    List<int> ObjId = new List<int>();
    List<float> distanceList = new List<float>();

    public bool isActive;

    int ObjInventory;

    // cuantas quests se completaron hasta ahora (para flags de progreso)
    private int completedCount;

    [SerializeField] private GameObject Humo;
    private bool Quest_0;
    private bool Quest_1;
    private bool Quest_2;
    private bool Quest_3;
    private bool Quest_4;
    private bool Quest_5;

    float Timer;
    private void Awake()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        dialogue = FindAnyObjectByType<DialogueController>();
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        bars = FindFirstObjectByType<Bars>();
        controller = FindFirstObjectByType<QuestController>();
        flow = FindFirstObjectByType<InGameSequenceController>();
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            originalMaterial = rend.material;
        }
        
    }
    private void OnEnable()
    {
        SpawnObj();
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

        Timer = 0;
    }

    private void FixedUpdate()
    {
        if (activeQuest == null || !activeQuest.isActive) return;

        activeQuest.timer += Time.fixedDeltaTime;

        switch (activeQuest.GetQuestType())
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

        if (Quest_0)
        {
            Humo.SetActive(true);

            float elapsed = activeQuest.timer;
            float duration = activeQuest.TimerDuration;

            if (duration <= 0f)
            {
                SFXManager.Instance.ResetVolume2D("kettle", 1f);
            }
            else if (elapsed >= duration * 0.85f)
            {
                SFXManager.Instance.ResetVolume2D("kettle", 0f);
            }
            else if (elapsed >= duration * 0.75f)
            {
                SFXManager.Instance.ResetVolume2D("kettle", 0.25f);
            }
            else if (elapsed >= duration * 0.50f)
            {
                SFXManager.Instance.ResetVolume2D("kettle", 0.5f);
            }
            else if (elapsed >= duration * 0.25f)
            {
                SFXManager.Instance.ResetVolume2D("kettle", 0.75f);
            }
            else
            {
                SFXManager.Instance.ResetVolume2D("kettle", 1f);
            }
        }
        else if (Quest_1)
        {
            float Distnace = Vector3.Distance(player.transform.position, Doll.transform.position);
            GameStateController.Instance.SetFlag("energy_restored", false);
            GameStateController.Instance.SetFlag("power_on", false);

            Timer += Time.fixedDeltaTime;

            if (Timer > 10)
            {
                GameStateController.Instance.SetFlag("quest_1_Dialogue_Hot", false);
                GameStateController.Instance.SetFlag("quest_1_Dialogue_Warm", false);
                GameStateController.Instance.SetFlag("quest_1_Dialogue_Cold", false);

                if (Distnace > 30)
                {
                    GameStateController.Instance.SetFlag("quest_1_Dialogue_Cold", true);
                    Timer = 0;
                }
                else if (Distnace > 20)
                {
                    GameStateController.Instance.SetFlag("quest_1_Dialogue_Warm", true);
                    Timer = 0;
                }
                else
                {
                    GameStateController.Instance.SetFlag("quest_1_Dialogue_Hot", true);
                    Timer = 0;
                }

                
            }
        }
        else if (Quest_2)
        {
            Timer += Time.fixedDeltaTime;

            if (Timer > 10)
            {
                playerMovement.InvertedControls = !playerMovement.InvertedControls;

                Timer = 0;

                return;
            }
        }
        else if (Quest_3)
        {
            GameStateController.Instance.SetFlag("laughter", false);

            Timer += Time.fixedDeltaTime;

            if (Timer > 5)
            {
                GameStateController.Instance.SetFlag("laughter", true);
                Timer = 0;
            }
        }
        else if (Quest_4)
        {

            Timer += Time.fixedDeltaTime;

            if (Timer > 10)
            {
                GameStateController.Instance.SetFlag("energy_restored", false);
                GameStateController.Instance.SetFlag("power_on", false);
                
                Timer = 0;
            }
        }
        else if (Quest_5)
        {
            
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
            IDQuest(index);

            activeQuest.isActive = true;
            activeQuest.halfNotified = false;
            dialogue.PlayDialogue(activeQuest.SubtitlesForQuest);
            isActive = activeQuest.isActive;
            activeQuest.timer = 0f;

            // flag para que los HintDialogue sepan que mision esta en curso
            SetQuestFlag($"quest_{activeQuest.QuestID}_active", true);

            Debug.LogWarning($"Quest Activated: ID = {activeQuest.QuestID}, Name = {activeQuest.QuestName}");

            Rooms();
            Obj();

            if (index != 1)
                TpDoll();
        }
    }

    private void IDQuest(int i)
    {
        float Timer = activeQuest.timer;
        switch (i)
        {
            case 0:
                {
                    SFXManager.Instance.PlayLoop2D("kettle", 1f);
                    SetQuestFlag($"quest_{activeQuest.QuestID}_Door", true);
                    Quest_0 = true;
                }
                break;
            case 1:
                {
                    randomObjectPositioner.ObjRandomAdd(Doll_);
                    Quest_1 = true;
                }
                break;
            case 2:
                {
                    Quest_2 = true;
                }
                break;
            case 3:
                {
                    Quest_3 = true;
                }
                break;
            case 4:
                {
                    Quest_4 = true;
                }
                break;
            case 5:
                {
                    Quest_5 = true;
                }
                break;
        }
    }
    public void ActivateQuest(int index)
    {
        _ActivateQuest(index);
    }
    private bool getIsCompleted() => activeQuest != null && activeQuest.isComplete;

    public bool _getIsCompleted()
    {
        return getIsCompleted();
    }
    private void FailQuest()
    {
        if (activeQuest == null) return;

        dialogue.PlayDialogue(activeQuest.SubtitlesForQuestFail);
        activeQuest.isActive = false;
        isActive = activeQuest.isActive;
        Debug.Log("Quest fallida:" + activeQuest.config.Name);

        SetQuestFlag($"quest_{activeQuest.QuestID}_active", false);
        SetQuestFlag($"quest_{activeQuest.QuestID}_active_half", false);
        SetQuestFlag($"quest_{activeQuest.QuestID}_failed", true);

        if (flow != null) flow.OnMissionFailed();

        if (Quest_0)
        {
            SFXManager.Instance.StopLoop("kettle");
        }
        else if (Quest_2)
        {
            playerMovement.InvertedControls = true;
        }
        else if (Quest_5)
        {
            playerMovement.SpeedPlayer(0.75f);
        }

        DisableQuestBool();
    }

    private void DisableQuestBool()
    {
        Quest_0 = false;
        Quest_1 = false;
        Quest_2 = false;
        Quest_3 = false;
        Quest_4 = false;
        Quest_5 = false;
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

    private bool checkTimer() => activeQuest != null && activeQuest.TimerDuration > 0f && activeQuest.timer >= activeQuest.TimerDuration;
    public bool _checkTimer()
    {
        return checkTimer();
    }

    private float getTimerDuration() => activeQuest != null ? activeQuest.TimerDuration : 0f;
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
        var requiredItems = activeQuest.ItemsToPick();// Lista de items requeridos por la quest
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

        CheckHalfProgress(collectedCount, totalRequired);

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
        Debug.Log(activeQuest.GetQuestType());

        distanceList = new List<float>(new float[Obj_.Count]); // Reiniciar la lista de distancias para cada objeto
        int ObjInRoom = 0;

        for (int i = 0; i < Obj_.Count; i++)
        {
            float distance = Vector3.Distance(Obj_[i].gameObject.transform.position, Room.position);
            Debug.Log($"Distancia entre {Obj_[i].gameObject.name} y destino: {distance}");

            distanceList[i] = distance;

            if (distanceList[i] <= distanceMin)
            {
                ObjInRoom++;
            }
        }


        CheckHalfProgress(ObjInRoom, activeQuest.ItemsToPick().Count);

        if (ObjInRoom >= activeQuest.ItemsToPick().Count)
        {
            CompleteActiveQuest();
            ObjInRoom = 0;
            return;
        }
    }

    // Avisa una sola vez cuando la mision pasa la mitad. Solo tiene sentido si requiere
    // 2 o mas objetos (ToCollect / ToDelivery); con 1 solo, la mitad seria completarla.
    private void CheckHalfProgress(int current, int total)
    {
        if (activeQuest == null || activeQuest.halfNotified) return;
        if (total < 2) return;

        if (current >= Mathf.CeilToInt(total / 2f))
        {
            activeQuest.halfNotified = true;

            if (!string.IsNullOrEmpty(activeQuest.SubtitlesForQuestHalf))
            {
                dialogue.PlayDialogue(activeQuest.SubtitlesForQuestHalf);
            }

            SetQuestFlag($"quest_{activeQuest.QuestID}_active_half", true);
        }
    }

    private void CompleteActiveQuest()
    {
        if (activeQuest == null) return;


        activeQuest.isActive = false;
        isActive = activeQuest.isActive;
        activeQuest.isComplete = true;
        dialogue.PlayDialogue(activeQuest.SubtitlesForQuestComplete);
        Debug.LogWarning("Quest completada: " + activeQuest.config.Name);

        bars.QuestFinished(activeQuest.GetEmotionIDType_Add(), (int)activeQuest.AddPoints);
        bars.QuestFinished(activeQuest.GetEmotionIDType_Remove(), -(int)activeQuest.RemovePoints);

        Debug.LogWarning($"Se completo la quest se le sumo {activeQuest.AddPoints} a {activeQuest.GetEmotionIDType_Add()} y se le resto {activeQuest.RemovePoints} a {activeQuest.GetEmotionIDType_Remove()}");

        // al completarse se sacan las flags de "activo" y queda solo la de "done"
        SetQuestFlag($"quest_{activeQuest.QuestID}_active", false);
        SetQuestFlag($"quest_{activeQuest.QuestID}_active_half", false);
        SetQuestFlag($"quest_{activeQuest.QuestID}_done", true);
        completedCount++;
        SetQuestFlag($"missions_completed_{completedCount}", true);

        if (flow != null) flow.OnMissionCompleted(activeQuest.CompletePoints);

        if (Quest_0)
        {
            SFXManager.Instance.StopLoop("kettle");
        }
        else if (Quest_2)
        {
            playerMovement.InvertedControls = false;
        }

        DisableQuestBool();
    }

    // setea una flag en el estado global, si existe el controlador
    private void SetQuestFlag(string flagName, bool value)
    {
        if (GameStateController.Instance != null)
        {
            GameStateController.Instance.SetFlag(flagName, value);
        }
    }

    private void TpDoll()
    {
        if (activeQuest == null) return;

        TpsDoll[] allTp = FindObjectsByType<TpsDoll>(FindObjectsSortMode.None);
        TpsDoll Tp = allTp.FirstOrDefault(p => p.IdTP == activeQuest.TpDollID);

        Doll.position = Tp.transform.position;
    }

    private void Rooms()
    {
        if (activeQuest == null)
        {
            //Debug.LogWarning("Rooms(): questData es null, no se puede asignar Room.");
            return;
        }

        Piece[] allPieces = FindObjectsByType<Piece>(FindObjectsSortMode.None);
        Piece piece = allPieces.FirstOrDefault(p => p.Id == activeQuest.roomID);

        if (piece == null)
        {
            //Debug.LogWarning($"Rooms(): no encontré ninguna pieza con ID {activeQuest.roomID}");
            return;
        }

        Room = piece.transform;
    }
    private void Obj()
    {
        if (activeQuest == null)
        {
            //Debug.LogWarning("Obj(): questData es null, no se puede asignar Obj.");
            return;
        }


        Obj_.Clear();
        ObjId.Clear();

        List<StructureQuest.QuestGeneric.itemsToPick> listItems = activeQuest.config.itemsToPickData;
        GrabbableObject[] allObj = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);


        for (int i = 0; i < activeQuest.ItemsToPick().Count; i++) 
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
                    //Debug.LogWarning($"metodo incorrecto, no se deberia llamar a este metodo en este tipo de Quest");
                    return;
                }

            }
            else if (objScene != null)
            {
                //Debug.Log($"Objeto encontrado para el ID {itemID}: {objScene.name}");
                Obj_.Add(objScene.transform);
                ObjId.Add(itemID);
            }
            else
            {
                // Si no lo encuentra, avisa en consola pero el juego Sigue corriendo bien
                //Debug.LogWarning($"No se encontró en la escena ni en el inventario el objeto con el ID: {itemID}");
            }
        }

    }

    private void ObjScena(int itemID, GrabbableObject objInventory)
    {
        //Debug.Log($"Objeto encontrado para el ID {itemID}: {objInventory.name}");
        Obj_.Add(objInventory.transform);
    }
    private void ObjCollect(int itemID, GrabbableObject objInventory)
    {
        //Debug.Log($"Objeto encontrado para el ID {itemID}: {objInventory.name}");
        Obj_.Add(objInventory.transform);
        ObjId.Add(itemID);
        ObjInventory++;
    }

    private void SpawnObj()
    {
        List<StructureQuest.QuestGeneric.itemsToPick> listItems = new();

        for (int i = 0; i < questDatabase.quests.Length; i++)
        {
            listItems.AddRange(questDatabase.quests[i].itemsToPickData);
        }

        for (int j = 0; j < listItems.Count; j++)
        {
            //Debug.Log("Item actual [" + j + "]: " + listItems[j]);
            randomObjectPositioner._ObjAdd(listItems[j]);
            //Debug.Log($"Spawned {listItems[j].quantity} item ID {listItems[j].itemID}");
        }   
    }
}