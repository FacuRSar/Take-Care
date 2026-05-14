using System;
using UnityEngine;

public class Happy : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;

    Bars bars;

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
    void Update()
    {



        //Pool de quest para cada nivel de felicidad, dependiendo del tiempo que el jugador mantenga la muñeca feliz, se van a ir activando diferentes eventos o triggers relacionados a la felicidad de la muñeca
        // si completas una quest muy rapido la muñeca se enoja, si la completas a tiempo se mantiene feliz, si no haces nada se pasa cry
        if (bars._CurrentHappyBar >= bars._MaxBar * 0.75f)
        {

            // Aquí deberías implementar la lógica para verificar si la quest de la felicidad está completada
            // Felicidad extrema o intensa
            // Sonido de risa fuerte, Movimientos visibles, Agitación intensa
            //Posible trigger de interacción si no se calma
            // Si el jugador no hace nada, la muñeca podría entrar en un estado de Neutral o calma después de cierto tiempo en este estado


        }
        else if (bars._CurrentHappyBar >= bars._MaxBar * 0.5f)
        {

        }
        else if (bars._CurrentHappyBar >= bars._MaxBar * 0.25f)
        {

            //Micro sonidos (sniffling leve)
        }
        else
        {

            dollEmotionSystem.ChangeState(DollState.Idle);



            //Respiración normal o casi inexistente
            // si pasa x tiempo vuelve a idle
        }
    }

}
