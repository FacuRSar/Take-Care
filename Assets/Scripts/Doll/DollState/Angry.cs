using System;
using UnityEngine;

public class Angry : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;
    Bars bars;

    public event Action AddAngryBar;

    bool IsQuestAngryCompleted;
    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
        bars = GetComponent<Bars>();

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
    void Update()
    {

        if (bars._CurrentAngryBar >= bars._MaxBar * 0.75f)
        {
            // Llanto extremo o distorsionado
            // Sonido de llanto fuerte, Lágrimas visibles, Agitación intensa
            //Posible trigger de ataque si no se calma
            // Si el jugador no hace nada, la muñeca podría entrar en un estado de Angry o agresión después de cierto tiempo en este estado
        }
        else if (bars._CurrentAngryBar >= bars._MaxBar * 0.5f)
        {
            //Lágrimas ocasionales, Sonido de llanto suave,Cabeza baja a ratos
        }
        else if (bars._CurrentAngryBar >= bars._MaxBar * 0.25f)
        {
            //Micro sonidos (sniffling leve)
        }
        else
        {
            //Respiración normal o casi inexistente
            // si pasa x tiempo vuelve a idle 
                dollEmotionSystem.ChangeState(DollState.Idle);

        }
    }
}