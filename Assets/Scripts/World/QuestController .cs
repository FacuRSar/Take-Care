using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class QuestController : MonoBehaviour
{
   private Quest currentQuest;

    [SerializeField] private List<Quest> allQuest = new List<Quest>();
    [SerializeField] private Bars bars;
    // [SerializeField]
    //private int questIndex;
    private void Awake()
    {
       allQuest = new List<Quest>(FindObjectsByType<Quest>(FindObjectsSortMode.None));
    }
    private void Start()
    {
        bars.InitializeQuestPools(allQuest);
        //giveQuest();
    }
    public void ActivateQuest(Quest selectedQuest)
    {
        if (currentQuest != null && currentQuest.getIsActive())
        { 
            Debug.LogWarning("Ya hay una misión en curso: " + currentQuest.name);
            return;
        }
        currentQuest = selectedQuest;
        currentQuest.setActive(true);

        Debug.Log("QuestController: Ejecutando misión: " + currentQuest.name);
    }
    private void Update()
    {
        if (currentQuest == null) return;

        if (currentQuest.getIsCompleted())
        {
            Debug.Log("Misión completada con éxito");
            FinalizeCurrentQuest();
        }
        else if (currentQuest.checkTimer())
        {
            Debug.Log("Misión fallida por tiempo");
            currentQuest.failQuest();
            FinalizeCurrentQuest();
        }
        else if (currentQuest.getTimer() >= currentQuest.getTimerDuration() * 0.75f)
        {
            currentQuest.markObjective();
        }

        /*

        if (checkQuestStatus(questIndex) == 0)
        {
            quests[questIndex].setActive(false);
            giveQuest();
        }
        else if (checkQuestStatus(questIndex) == 1)
        {
            quests[questIndex].failQuest();
            giveQuest();
        }
        else if (checkQuestStatus(questIndex) == 2)
        {
            if (quests[questIndex].getTimer() >= quests[questIndex].getTimerDuration()*0.75)
            {
                quests[questIndex].markObjective();
            }
        }
        */
    }

    private void FinalizeCurrentQuest()
    {
        currentQuest.setActive(false);
        currentQuest = null; // Queda libre para la siguiente misión que mande Bars
    }


  /*  public int checkQuestStatus(int questIndex)
    {
        if (questIndex >= 0 && questIndex < quests.Count)
        {
            if(quests[questIndex].getIsCompleted())
            {
                return 0;
            }
            else if(!quests[questIndex].getIsCompleted() && quests[questIndex].checkTimer())
            {
                return 1;
            }
        }
        return 2;
    }
    public void failQuest()
    {
        
    }
    public void giveQuest()
    {
        int random = Random.Range(0, quests.Count);
        if (random >= 0 && random < quests.Count)
        {
            quests[random].setActive(true);
            questIndex = random;
        }
    }*/
}
