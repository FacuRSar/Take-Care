using System;
using UnityEngine;

public class Idle : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;

    [SerializeField] float distanceToPlayerMin;

    public event Action AddHappyBar;
    public event Action AddCryBar;
    public event Action AddAngryBar;

    bool PlayerIsFacingAway;

    float Timer;
    float RestTimer = 10f;

    void Awake()
    {
        dollEmotionSystem = GetComponent<DollEmotionSystem>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Timer = RestTimer;
    }

    // Update is called once per frame

    private void OnEnable()
    {

        Debug.Log("La muñeca respira y tiene un leve movimeinto en el pecho");

    }

    private void OnDisable()
    {

        Debug.Log("La muñeca se agita");

    }
    void Update()
    {
        if (dollEmotionSystem.Player == null) return;

        AddAngryBar?.Invoke();
        AddCryBar?.Invoke();
        AddHappyBar?.Invoke();

        Vector3 DistanceToPlayer = dollEmotionSystem.Player.transform.position - dollEmotionSystem.Doll.transform.position;

        float distance = DistanceToPlayer.magnitude;

        bool PlayerIsFacingAway = IsPlayerIsFacingAway();
        bool isFar = Vector3.Distance(dollEmotionSystem.Player.transform.position, dollEmotionSystem.Doll.transform.position) > distanceToPlayerMin;

        if (isFar || PlayerIsFacingAway) // Si el jugador está lejos O dado vuelta
        {
            Debug.Log("Sentis respiraciones en la nuca");

            Timer -= Time.deltaTime;
            Debug.Log("Timer: " + Timer);

            if (Timer <= 0)
            {   
                Debug.Log("Cambiaste al estado: Watching");
                dollEmotionSystem.ChangeState(DollState.Watching);
            }
            else
            {
                Debug.Log("El jugador se siente observado");
            }

        }
        else
        {
                Timer = RestTimer;
        }



    }

    bool IsPlayerIsFacingAway()
    {
        Vector3 directionToDoll = (dollEmotionSystem.Doll.transform.position - dollEmotionSystem.Player.transform.position).normalized;

        float dot = Vector3.Dot(dollEmotionSystem.Player.transform.forward, directionToDoll); // El valor del dot product estará entre -1 y 1. Si es cercano a 1, el jugador está mirando hacia la muñeca; si es cercano a -1, el jugador está mirando en la dirección opuesta

        return dot < 0; // Si el dot product es menor que 0, el jugador está dado vuelta hacia la muñeca
    }
}
