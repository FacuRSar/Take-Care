using System;
using UnityEngine;

public class Happy : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;

    public event Action AddHappyBar;
    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
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
        if (BarHappyCurrent >= BarHappyMax * 0.75f)
        {

            bool IsQuestHappyCompleted = PoolQuestHappy(); // Aquí deberías implementar la lógica para verificar si la quest de la felicidad está completada
            // Felicidad extrema o intensa
            // Sonido de risa fuerte, Movimientos visibles, Agitación intensa
            //Posible trigger de interacción si no se calma
            // Si el jugador no hace nada, la muñeca podría entrar en un estado de Neutral o calma después de cierto tiempo en este estado
        }
        else if (BarHappyCurrent >= BarHappyMax * 0.5f)
        {
            bool IsQuestHappyCompleted = PoolQuestHappy(); // Aquí deberías implementar la lógica para verificar si la quest de la felicidad está completada
            //Felicidad moderada, Sonido de risa suave, Movimientos visibles pero menos intensos
        }
        else if (BarHappyCurrent >= BarHappyMax * 0.25f)
        {
            bool IsQuestHappyCompleted = PoolQuestHappy(); // Aquí deberías implementar la lógica para verificar si la quest de la felicidad está completada
            //Micro sonidos (sniffling leve)
        }
        else
        {

            //Respiración normal o casi inexistente
            // si pasa x tiempo vuelve a idle
        }
    }

    private bool PoolQuestHappy()
    {
        // Aquí deberías implementar la lógica para verificar si la quest de la felicidad está completada
        // Esto podría incluir revisar el estado del jugador, los objetos que tiene, las acciones que ha realizado, etc.
        // Devuelve true si la quest está completada, false en caso contrario
        return false; // Placeholder, reemplaza con tu lógica real
    }
}
