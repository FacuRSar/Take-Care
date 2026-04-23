using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class QuestController : MonoBehaviour
{
    [SerializeField] private List<Quest> quests = new List<Quest>();

    public bool checkQuestStatus(int questIndex)
    {
        if (questIndex >= 0 && questIndex < quests.Count)
        {
            return quests[questIndex].getIsCompleted();
        }
        return false;
    }
    public void giveQuest()
    {
        int random = Random.Range(0, quests.Count);
        if (random >= 0 && random < quests.Count)
        {
            quests[random].setActive(true);
        }
    }
}
