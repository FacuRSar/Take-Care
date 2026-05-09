using System;
using UnityEngine;

public class Angry : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;

    public event Action AddAngryBar;

    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
    }

  // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void OnEnable()
    {
        // La muñeca se siente enojada y agresiva (gritos, movimientos bruscos);
    }

    private void OnDisable()
    {
        // La muñeca deja de estar enojada
    }

    private void Update()
    {
        // La lógica para el estado de Angry podría incluir un temporizador que, si el jugador no hace nada para calmar a la muñeca, podría desencadenar un ataque o una acción agresiva
    }
}
