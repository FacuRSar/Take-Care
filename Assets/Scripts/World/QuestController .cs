
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static StructureQuest;

public class QuestController : MonoBehaviour
{
    [SerializeField] private Quest quest;
    [SerializeField] private Bars bars;

    public void Initialize(List<QuestData> quests)
    {
            bars.InitializeQuestPools(quests);
    }
    public void ActivateQuest(QuestData selectedQuest)
    {
        //selectedQuest.setActive(true);
        int index = quest.allQuests_.IndexOf(selectedQuest);
        quest.ActivateQuest(index);
        

        Debug.Log("QuestController: Ejecutando misión: " + quest.allQuests_.IndexOf(selectedQuest));
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
}