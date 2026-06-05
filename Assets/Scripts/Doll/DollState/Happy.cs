using System;
using UnityEngine;

public class Happy : DollEmotion
{
    DollEmotionSystem dollEmotionSystem;

    public event Action AddHappyBar;

    bool IsQuestHappyCompleted;

    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
        bars = GetComponent<Bars>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        setCurrentBar();
    }

    // Update is called once per frame
    private void OnEnable()
    {
        // La muñeca se siente feliz y  alegre (risas);
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
