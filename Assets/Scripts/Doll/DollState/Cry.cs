using System;
using UnityEngine;

public class Cry : DollEmotion
{
    DollEmotionSystem dollEmotionSystem;
    public event Action AddCryBar;


    bool IsQuestCryCompleted;
    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
        bars = GetComponent<Bars>();
        lowInteraction = "DollCryLow";
        highInteraction = "DollCryHigh";
        mediumInteraction = "DollCryMid";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Update is called once per frame

    private void OnEnable()
    {
        Debug.Log("No llora");
    }

    private void OnDisable()
    {
        Debug.Log("La muñeca deja de llorar");
    }
    public override void setCurrentBar()
    {
        currentBar = bars._CurrentCryBar;
    }
    public void FixedUpdate()
    {
        setCurrentBar();
        timerRestar++;

        CheckInteraction();

        if (timerRestar >= 120)
        {
            timerRestar = 0;
            bars.sumCryBar(1);
        }
    }
}
