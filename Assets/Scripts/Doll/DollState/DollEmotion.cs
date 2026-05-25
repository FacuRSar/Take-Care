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


    public void LowInteraction(AudioSource dollVoice)
    {
        dollVoice.PlayOneShot(lowInteraction);
    }
    public void MediumInteraction(AudioSource dollVoice)
    {
        dollVoice.PlayOneShot(mediumInteraction);
    }
    public void HighInteraction(AudioSource dollVoice)
    {
        dollVoice.PlayOneShot(highInteraction);
    }
    public void CheckInteraction(AudioSource dollVoice)
    {

        if (currentBar >= bars._MaxBar * 0.75f)
        {
            // Llanto extremo o distorsionado
            // Sonido de llanto fuerte, Lágrimas visibles, Agitación intensa
            //Posible trigger de ataque si no se calma
            // Si el jugador no hace nada, la muñeca podría entrar en un estado de Angry o agresión después de cierto tiempo en este estado
            HighInteraction(dollVoice);
            return;
        }
        else if (currentBar >= bars._MaxBar * 0.5f)
        {
            //Lágrimas ocasionales, Sonido de llanto suave,Cabeza baja a ratos
            MediumInteraction(dollVoice);
            return;
        }
        else if (currentBar >= bars._MaxBar * 0.25f)
        {
            //Micro sonidos (sniffling leve)
            LowInteraction(dollVoice);
            return;
        }
    }
}
