using UnityEngine;

public class Bars : MonoBehaviour
{
    private DollEmotionSystem dollEmotionSystem;

    #region HappyBar
    float HappyBar;
    float CurrentHappyBar;
    public float _CurrHappyBar { get { return CurrentHappyBar; } private set { CurrentHappyBar = value; } }
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

    [SerializeField]float SpeedBar = 1f;

    float MaxBar = 100f;
    float MinBar = 0f;

    public float _MaxBar { get { return MaxBar; } private set { MaxBar = value; } }

    public int Points;
    int AddPoints;

    bool ActiveAddPointsForQuest;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
       dollEmotionSystem = GetComponent<DollEmotionSystem>();

       Watching watching = GetComponent<Watching>();
       Angry angry = GetComponent<Angry>();
       Cry cry = GetComponent<Cry>();
       Happy happy = GetComponent<Happy>();

        watching.AddHappyBar += _AddHappyBar;
        watching.AddCryBar += _AddCryBar;
        watching.AddAngryBar += _AddAngryBar;

        angry.AddAngryBar += _AddAngryBar;
        cry.AddCryBar += _AddCryBar;
        happy.AddHappyBar += _AddHappyBar;
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

        if(ActiveAddPointsForQuest) AddPointsForQuest();
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
    void AddPointsForQuest()
    {
        switch (Points)
        {
            case 1:
                AddPoints = 5;
                break;
            case 2:
                AddPoints = 10;
                break;
            case 3:
                AddPoints = 15;
                break;
            case 4:
                AddPoints = 25;
                break;
        }

         switch(dollEmotionSystem._CurrentState)
        {
            case DollState.Happy:
                HappyBar += AddPoints;
                break;
            case DollState.Cry:
                CryBar += AddPoints;
                break;
            case DollState.Angry:
                AngryBar += AddPoints;
                break;
        }

        Points = 0;
        ActiveAddPointsForQuest = false;
    }
}
