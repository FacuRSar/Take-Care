using System.Collections.Generic;
using UnityEngine;


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
        int index = quest.allQuests_.IndexOf(selectedQuest);
        quest.ActivateQuest(index);   
        Debug.Log("QuestController: Ejecutando misión: " + quest.allQuests_.IndexOf(selectedQuest));
    }
    private void Update()
    {
        if (quest == null || !quest.isActive) return;

        if (quest._getIsCompleted())
        {
            Debug.Log("Misión completada con éxito");
        }
        else if (quest._checkTimer())
        {
            Debug.Log("Misión fallida por tiempo");
            quest._FailQuest();
        }
        else if (quest._getTimer() >= quest._getTimerDuration() * 0.75f)
        {
            quest._MarkObjective();
        }
    }
}