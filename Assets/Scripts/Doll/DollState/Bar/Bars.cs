using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Bars : MonoBehaviour
{
    private DollEmotionSystem dollEmotionSystem;

    #region HappyBar
    float HappyBar;
    float CurrentHappyBar;
    public float _CurrentHappyBar { get { return CurrentHappyBar; } private set { CurrentHappyBar = value; } }
    #endregion

    #region CryBar
    float CryBar;
    float CurrentCryBar;
    public float _CurrentCryBar { get { return CurrentCryBar; } private set { CurrentCryBar = value; } }
    #endregion

    #region AngryBar
    float AngryBar;
    float CurrentAngryBar;
    public float _CurrentAngryBar { get { return CurrentAngryBar; } private set { CurrentAngryBar = value; } }
    #endregion

    [Header("Bar Settings")]

    float TotalBar;

    [SerializeField]float SpeedBar = 1f;

    float MaxBar = 100f;
    float MinBar = 0f;

    public float _MaxBar { get { return MaxBar; } private set { MaxBar = value; } }

    int IndexQuest;

    int AddPoints;

    bool ActiveAddPointsForQuest;

    bool ToCallQuest;

    [SerializeField] private List<QuestData> PoolHappyQuest = new List<QuestData>();
    [SerializeField] private List<QuestData> PoolCryQuest = new List<QuestData>();
    [SerializeField] private List<QuestData> PoolAngryQuest = new List<QuestData>();

    private List<float> Weights = new List<float>();
    private List<float> HappyQuestWeights = new List<float>();
    private List<float> CryQuestWeights = new List<float>();
    private List<float> AngryQuestWeights = new List<float>();

    [SerializeField] QuestData selectedQuest;
    [SerializeField] QuestController questController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
       dollEmotionSystem = GetComponent<DollEmotionSystem>();

        Watching watching = GetComponent<Watching>();
        Angry angry = GetComponent<Angry>();
        Cry cry = GetComponent<Cry>();
        Happy happy = GetComponent<Happy>();
        Idle idle = GetComponent<Idle>();

        idle.AddAngryBar += _AddAngryBar;
        idle.AddHappyBar += _AddHappyBar;
        idle.AddCryBar += _AddCryBar;

        watching.AddHappyBar += _AddHappyBar;
        watching.AddCryBar += _AddCryBar;
        watching.AddAngryBar += _AddAngryBar;

        angry.AddAngryBar += _AddAngryBar;
        cry.AddCryBar += _AddCryBar;
        happy.AddHappyBar += _AddHappyBar;



        MatchList();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        HappyBar = Mathf.Clamp(HappyBar, MinBar, MaxBar);
        CurrentHappyBar = HappyBar;

        CryBar = Mathf.Clamp(CryBar, MinBar, MaxBar);
        CurrentCryBar = CryBar;

        AngryBar = Mathf.Clamp(AngryBar, MinBar, MaxBar);
        CurrentAngryBar = AngryBar;

        TotalBar = CurrentHappyBar + CurrentAngryBar + CurrentCryBar;

        Weights[0] = (CurrentHappyBar / TotalBar);
        Weights[1] = (CurrentCryBar / TotalBar);
        Weights[2] = (CurrentAngryBar / TotalBar);

        

      //  if (ActiveAddPointsForQuest) AddPointsForQuest();
    }
    /* public void InitializeQuestPools(List<QuestData> allQuest)
     {
         foreach (QuestData quest in allQuest)
         {
             switch (quest.GetStateType())
             {
                 case questEmotionType.Happy:
                     PoolHappyQuest.Add(quest);
                     HappyQuestWeights.Add(1f);
                     break;

                 case questEmotionType.Cry:
                     PoolCryQuest.Add(quest);
                     CryQuestWeights.Add(1f);
                     break;

                 case questEmotionType.Angry:
                     PoolAngryQuest.Add(quest);
                     AngryQuestWeights.Add(1f);
                     break;
             }
         }
     }*/

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            InvokeQuest();
        }
    }
    public void InitializeQuestPools(List<QuestData> allQuests)
    {
        foreach (var Quest_ in allQuests)
        {
            switch (Quest_.GetStateType())
            {
                case questEmotionType.Happy:
                    PoolHappyQuest.Add(Quest_);
                    HappyQuestWeights.Add(1f);
                    break;
                case questEmotionType.Cry:
                    PoolCryQuest.Add(Quest_);
                    CryQuestWeights.Add(1f);
                    break;
                case questEmotionType.Angry:
                    PoolAngryQuest.Add(Quest_);
                    AngryQuestWeights.Add(1f);
                    break;
            }
        }
    }

        int GetRandomIndex(List<float> weights)
    {
        float WeightTotal = 0f;

        foreach (float weight in weights)
        {
            WeightTotal += weight;
        }

        float randomValue = Random.Range(0, WeightTotal);

        float cumulativeWeight = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulativeWeight += weights[i];

            if (cumulativeWeight > randomValue)
            {
                return i;
            }
        }


        return 0;
    }


    public void InvokeQuest()
    {
        float Index = GetRandomIndex(Weights);

            switch (Index)
            {
                case 0:
                    int IndexHappyQuest = GetRandomIndex(HappyQuestWeights);
                    selectedQuest = PoolHappyQuest[IndexHappyQuest];

                    questController.ActivateQuest(selectedQuest);

                    HappyQuestWeights[IndexHappyQuest] = 0;
                    break;

                case 1:
                    int IndexCryQuest = GetRandomIndex(CryQuestWeights);
                    selectedQuest = PoolCryQuest[IndexCryQuest];

                    questController.ActivateQuest(selectedQuest);

                    CryQuestWeights[IndexCryQuest] = 0;
                    break;

                case 2:
                    int IndexAngryQuest = GetRandomIndex(AngryQuestWeights);
                    selectedQuest = PoolAngryQuest[IndexAngryQuest];

                    questController.ActivateQuest(selectedQuest);

                    AngryQuestWeights[IndexAngryQuest] = 0;
                    break;
            }
        


        Debug.Log("Toco Quest" + selectedQuest);
        selectedQuest = null;
    }

    void MatchList()
    {
        Weights.Add(1f);
        Weights.Add(1f);
        Weights.Add(1f);
    }

    void _AddHappyBar()
    {
        HappyBar += SpeedBar * Time.deltaTime;
    }
    void _AddCryBar()
    {
        CryBar += SpeedBar * Time.deltaTime;
    }
    void _AddAngryBar()
    {
        AngryBar += SpeedBar * Time.deltaTime;
    }

    public void QuestFinished(questEmotionType value, int _value)
    {
        switch (value)
        {
            case questEmotionType.Cry:
                CryBar += _value;
                break;
            case questEmotionType.Happy:
                HappyBar += _value;
                break;
            case questEmotionType.Angry:
                AngryBar += _value;
                break;
        }
    }
    public string getTopBar()
    {
        if (CurrentHappyBar >= CurrentCryBar && CurrentHappyBar >= CurrentAngryBar)
        {
            return "DollHappy";
        }
        else if (CurrentCryBar >= CurrentAngryBar)
        {
            return "DollCry";
        }
        else
        {
            return "DollAngry";
        }
    }
}
