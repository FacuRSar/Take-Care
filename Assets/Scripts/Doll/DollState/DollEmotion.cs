using System;
using System.Threading;
using UnityEngine;

public class DollEmotion : MonoBehaviour
{
    [SerializeField] protected float DistanceToDollMin;
    [SerializeField] protected float timer = 10f;
    protected float timerCurrent;
    protected float currentBar;
    protected Bars bars;
    [Header("Interaction Resources")]
    protected String lowInteraction;
    protected String mediumInteraction;
    protected String highInteraction;
    protected ScreenEffectController screenController;
    protected int timerRestar;
    [SerializeField] protected SFXManager sfxManager;
    protected float lastInteraction;
    void Start()
    {
        screenController = FindAnyObjectByType<ScreenEffectController>();
        screenController.PlayEffect("fatigue");
        setCurrentBar();
    }


    public void LowInteraction()
    {
        if(lowInteraction != null)
            sfxManager.Play2D(lowInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public void MediumInteraction()
    {
        if(mediumInteraction != null)
           sfxManager.Play2D(mediumInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public void HighInteraction()
    {
        if(highInteraction != null)
            sfxManager.Play2D(highInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public virtual void CheckInteraction()
    {
        Debug.Log(currentBar);

        if (currentBar - lastInteraction > 10)
        {
            if (currentBar >= 75)
            {
                screenController.SetVignetteIntensity("fatigue", 0.75f);
                HighInteraction();
            }
            else if (currentBar >= 50)
            {
                screenController.SetVignetteIntensity("fatigue", 0.5f);
                MediumInteraction();
            }
            else if (currentBar >= 25)
            {
                screenController.SetVignetteIntensity("fatigue", 0.25f);
                LowInteraction();
            }
            lastInteraction = currentBar;
        }
    }
    public float getCurrentBar()
    {
        return currentBar;
    }
    public virtual void setCurrentBar() { }
}
