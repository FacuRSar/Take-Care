using System;
using Unity.VisualScripting;
using UnityEngine;

public class Happy : DollEmotion
{
    DollEmotionSystem dollEmotionSystem;

    public event Action AddHappyBar;

    bool IsQuestHappyCompleted;
    private InGameSequenceController flow;

    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
        bars = GetComponent<Bars>();
        lowInteraction = "DollHappyLow";
        highInteraction = "DollHappyHigh";
        mediumInteraction = "DollHappyMid";
        flow = FindFirstObjectByType<InGameSequenceController>();
    }
    public void FixedUpdate()
    {
        setCurrentBar();
        timerRestar++;
        if (timerRestar >= 120)
        {
            flow.OnMissionCompleted(1);
            timerRestar = 0;
            CheckInteraction();
        }
    }
    public override void CheckInteraction()
    {

        if (currentBar - lastInteraction > 10)
        {
            if (currentBar >= 75)
            {
                HighInteraction();
            }
            else if (currentBar >= 50)
            {
                MediumInteraction();
            }
            else if (currentBar >= 25)
            {
                LowInteraction();
            }
            lastInteraction = currentBar;
        }
    }
    // Update is called once per frame
    private void OnEnable()
    {
        screenController.SetVignetteIntensity("fatigue", 0);
    }

    private void OnDisable()
    {
        // La muñeca deja de estar feliz
    }
    public override void setCurrentBar()
    {
        currentBar = bars._CurrentHappyBar;
    }
}
