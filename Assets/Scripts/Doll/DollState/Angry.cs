using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class Angry : DollEmotion
{
    DollEmotionSystem dollEmotionSystem;

    public event Action AddAngryBar;

    bool IsQuestAngryCompleted;
    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
        bars = GetComponent<Bars>();
        currentBar = bars._CurrentAngryBar;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame

    private void OnEnable()
    {
        Debug.Log("No llora");
    }

    private void OnDisable()
    {
        Debug.Log("La muñeca deja de llorar");
    }
}