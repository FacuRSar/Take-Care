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
    [SerializeField] AudioClip lowInteraction;
    [SerializeField] AudioClip mediumInteraction;
    [SerializeField] AudioClip highInteraction;
    protected ScreenEffectController screenController;
    protected int timerRestar;
    [SerializeField] protected AudioSource dollVoice;
    void Start()
    {
        screenController = FindAnyObjectByType<ScreenEffectController>();
        screenController.PlayEffect("fatigue");
    }


    public void LowInteraction(AudioSource dollVoice)
    {
        if(lowInteraction != null)
            dollVoice.PlayOneShot(lowInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public void MediumInteraction(AudioSource dollVoice)
    {
        if(mediumInteraction != null)
           dollVoice.PlayOneShot(mediumInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public void HighInteraction(AudioSource dollVoice)
    {
        if(highInteraction != null)
            dollVoice.PlayOneShot(highInteraction);
        //funcion de angel para generar un circulo que tapa parte de la camara en base a la barra de emocion
    }
    public void CheckInteraction()
    {

        if (currentBar >= bars._MaxBar)
        {
            if (dollVoice == null)
                return;
            // Llanto extremo o distorsionado
            // Sonido de llanto fuerte, Lágrimas visibles, Agitación intensa
            //Posible trigger de ataque si no se calma
            // Si el jugador no hace nada, la muñeca podría entrar en un estado de Angry o agresión después de cierto tiempo en este estado
            HighInteraction(dollVoice);
            return;
        }
        else if (currentBar >= bars._MaxBar)
        {
            if (dollVoice == null)
                return;
            //Lágrimas ocasionales, Sonido de llanto suave,Cabeza baja a ratos
            MediumInteraction(dollVoice);
            return;
        }
        else if (currentBar >= bars._MaxBar)
        {
            if (dollVoice == null)
                return;
            //Micro sonidos (sniffling leve)
            LowInteraction(dollVoice);
            return;
        }
    }
    public float getCurrentBar()
    {
        return currentBar;
    }
    public virtual void setCurrentBar() { }
}
