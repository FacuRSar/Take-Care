
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestController : MonoBehaviour
{
   private Quest AllQuest;

    [SerializeField] private List<Quest> allQuest = new List<Quest>();

    [SerializeField] private Bars bars;
    // [SerializeField]
    //private int questIndex;
    private void Start()
    {
        bars.InitializeQuestPools(allQuest);
        //giveQuest();
    }
    public void Initialize(List<StructureQuest.QuestGeneric> questGenerics)
    {
        allQuest.Clear();

        foreach (var questGeneric in questGenerics)
        {
            Quest newQuest = new Quest();

            // Inicializamos el nuevo Quest (Tipo B) con los datos de questGeneric
            newQuest.Initialize(new StructureQuest.QuestGeneric[] { questGeneric }); // sirve para que las dos partes del codigo se entiendan

            allQuest.Add(newQuest);
        }
    }
    public void ActivateQuest(Quest selectedQuest)
    {
        if (AllQuest != null && AllQuest.getIsActive())
        { 
            Debug.LogWarning("Ya hay una misión en curso: " + AllQuest.name);
            return;
        }
        AllQuest = selectedQuest;
        AllQuest.setActive(true);

        Debug.Log("QuestController: Ejecutando misión: " + AllQuest.name);
    }
    private void Update()
    {
        if (AllQuest == null) return;

        if (AllQuest.getIsCompleted())
        {
            Debug.Log("Misión completada con éxito");
            FinalizeCurrentQuest();
        }
        else if (AllQuest.checkTimer())
        {
            Debug.Log("Misión fallida por tiempo");
            AllQuest.failQuest();
            FinalizeCurrentQuest();
        }
        else if (AllQuest.getTimer() >= AllQuest.getTimerDuration() * 0.75f)
        {
            AllQuest.markObjective();
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
        AllQuest.setActive(false);
        AllQuest = null; // Queda libre para la siguiente misión que mande Bars
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
