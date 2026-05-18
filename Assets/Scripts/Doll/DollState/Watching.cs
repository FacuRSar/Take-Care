using System;
using System.Threading;
using UnityEngine;

public class Watching : MonoBehaviour
{
    DollEmotionSystem dollEmotionSystem;

    [SerializeField]float DistanceToDollMin;
    [SerializeField]float timer = 10f;
    float timerCurrent;

    public event Action DollLookAtYou;
    public event Action AddHappyBar;
    public event Action AddCryBar;
    public event Action AddAngryBar;
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
        //Debug.Log("La muñeca te mira fijamente y te sigue con la cabeza");

        timerCurrent = timer;
    }

    private void OnDisable()
    {
        //Debug.Log("La muñeca deja de mirarte");
    }
    void Update()
    {
        bool PlayerIsFacingAway = IsPlayerIsFacingAway();

        timerCurrent -= Time.deltaTime;

        if (PlayerIsFacingAway)
        {
            Debug.Log("Sentis que la muñeca te sigue mirando aunque no puedas verla");

            Vector3 DirectionToPlayer = (dollEmotionSystem.Player.transform.position - dollEmotionSystem.Doll.transform.position);
            dollEmotionSystem.transform.rotation = Quaternion.LookRotation(DirectionToPlayer.normalized);



            if (timerCurrent < 0 && DirectionToPlayer.magnitude > DistanceToDollMin)
            {
                Debug.Log("La muñeca se esta poniendo enojada porque no le prestas atención");
                
                dollEmotionSystem.ChangeState(DollState.Angry);
            }
            else if(timerCurrent < 0)
            {
               // bool IsQuestCazadoraCompleted = QuestCazdora();  aca deberías implementar la lógica para verificar si la quest de la cazadora está completada

                Debug.Log("Dialogos");
                Debug.Log("Dialogos papa no me quiere");

                
                dollEmotionSystem.ChangeState(DollState.Cry);
            }
            else if(DirectionToPlayer.magnitude < DistanceToDollMin)
            {
                Debug.Log("No estas mirando a la muñeca y se esta poniendo triste");
                AddCryBar?.Invoke();
                
            }
            else
            {
                Debug.Log("La muñeca se esta enojando porque no le prestas atención");
                AddAngryBar?.Invoke();
            }


        }
        else
        {
            if (timerCurrent < 0)
            {
                // Si el jugador no está de espaldas, puedes implementar la lógica para que la muñeca lo siga mirando
                Debug.Log("La muñeca esta feliz porque la estas mirando");
                
                dollEmotionSystem.ChangeState(DollState.Happy);
            }
            else
            {      
                Debug.Log("La muñeca siente que le das atencion, le gusta");
                AddHappyBar?.Invoke();
            }
          
        }


    }

    bool IsPlayerIsFacingAway()
    {
        Vector3 directionToDoll = (dollEmotionSystem.Doll.transform.position - dollEmotionSystem.Player.transform.position).normalized;

        float dot = Vector3.Dot(dollEmotionSystem.Player.transform.forward, directionToDoll); // El valor del dot product estará entre -1 y 1. Si es cercano a 1, el jugador está mirando hacia la muñeca; si es cercano a -1, el jugador está mirando en la dirección opuesta

        return dot < 0; // Si el dot product es menor que 0, el jugador está dado vuelta hacia la muñeca
    }

    bool QuestCazdora()
    {
        // Aquí deberías implementar la lógica para verificar si la quest de la cazadora está completada
        return false;
    }

    private void OnDrawGizmos()
    {
        if (dollEmotionSystem == null)
        {
            return;
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(dollEmotionSystem.Doll.transform.position, DistanceToDollMin);
        }
    }
}