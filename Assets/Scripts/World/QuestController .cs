
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static StructureQuest;

public class QuestController : MonoBehaviour
{
   private Quest quest;

    [SerializeField] private Quest allQuest;
    [SerializeField] private Bars bars;

    public void Initialize(List<Quest> quests)
    {
        foreach (Quest quest in quests)
        {
            bars.InitializeQuestPools(quest);
        }
    }
    public void ActivateQuest(Quest selectedQuest)
    {
        selectedQuest.setActive(true);

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
}
