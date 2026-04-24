using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class QuestController : MonoBehaviour
{
    [SerializeField] private List<Quest> quests = new List<Quest>();
    private int questIndex;
    private void Start()
    {
        giveQuest();
    }
    private void Update()
    {
        if (checkQuestStatus(questIndex) == 0)
        {
            quests[questIndex].setActive(false);
            giveQuest();
        }
        else if (checkQuestStatus(questIndex) == 1)
        {
            //perder vida o algo asi
        }
    }
    public int checkQuestStatus(int questIndex)
    {
        if (questIndex >= 0 && questIndex < quests.Count)
        {
            if(quests[questIndex].getIsCompleted())
            {
                return 0;
            }
            else if(!quests[questIndex].getIsCompleted() && quests[questIndex].getTimer()<=0)
            {
                return 1;
            }
        }
        return 2;
    }
    public void giveQuest()
    {
        int random = Random.Range(0, quests.Count);
        if (random >= 0 && random < quests.Count)
        {
            quests[random].setActive(true);
            questIndex = random;
        }
    }
}
