
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static StructureQuest;

public class QuestController : MonoBehaviour
{
   private Quest quest;

    [SerializeField] private List<QuestData> allQuest = new List<QuestData>();

    [SerializeField] private Bars bars;

    public void Initialize(List<QuestData> quests)
    {
        allQuest.Clear();

        allQuest.AddRange(quests);

        // Se lo mandas a las barras
        bars.InitializeQuestPools(allQuest);
    }
    public void ActivateQuest(Quest selectedQuest)
    {
        if (quest != null && quest.getIsActive())
        { 
            Debug.LogWarning("Ya hay una misión en curso: " + quest.name);
            return;
        }
        quest = selectedQuest;
        quest.setActive(true);

        Debug.Log("QuestController: Ejecutando misión: " + quest.name);
    }
    private void Update()
    {
        if (quest == null) return;

        if (quest.getIsCompleted())
        {
            Debug.Log("Misión completada con éxito");
            FinalizeCurrentQuest();
        }
        else if (quest.checkTimer())
        {
            Debug.Log("Misión fallida por tiempo");
            quest.FailQuest();
            FinalizeCurrentQuest();
        }
        else if (quest.getTimer() >= quest.getTimerDuration() * 0.75f)
        {
            quest.MarkObjective();
        }

    }

    private void FinalizeCurrentQuest()
    {
        quest.setActive(false);
        quest = null; // Queda libre para la siguiente misión que mande Bars
    }

    internal void ActivateQuest(QuestData selectedQuest)
    {
        if (!quest.getIsCompleted())
        {
            Debug.Log("No nos vamos nada QUest");
        }
        else
        {
            selectedQuest = null;
        }
    }
}
