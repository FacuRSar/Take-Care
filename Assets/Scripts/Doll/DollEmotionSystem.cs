using UnityEditor.Rendering;
using UnityEngine;

public class DollEmotionSystem : MonoBehaviour
{
    #region Current State

    private DollState Currentstate;
    private DollEmotion currentEmotion;

    public DollState _CurrentState { get { return Currentstate; } set { Currentstate = value; } }
    #endregion
    private string currentStateName = "DollIdle";
    private bool isQuestActive = false;
    private int idleStateCounter = 0;

    #region States

    Idle idleState;
    Watching watchingState;
    Happy happyState;
    Angry angryState;
    Cry cryState;

    #endregion

    [Header("Player Reference")]

    [SerializeField] private Camera player;
    [SerializeField] PlayerSanity playerSanity;

    [Header("Doll Reference")]
    [SerializeField] private Transform doll;
    public Transform Doll { get => doll; set => doll = value; }
    [SerializeField] private AudioSource audioSource;

    public Camera Player { get => player; set => player = value; }

    [Header("GameState Reference")]
    private GameStateController gameStateController;
    private Bars bars;
    private DollState currentMaxBar;
    private ScreenEffectController screenEffectController;
    private bool isEffectActive = false;
    private QuestController questController;

    void Awake()
    {
        questController = FindAnyObjectByType<QuestController>();
        screenEffectController = FindAnyObjectByType<ScreenEffectController>();
        idleState = GetComponent<Idle>();
        watchingState = GetComponent<Watching>();
        happyState = GetComponent<Happy>();
        angryState = GetComponent<Angry>();
        cryState = GetComponent<Cry>();
        currentEmotion = idleState;
        gameStateController = FindAnyObjectByType<GameStateController>();
        bars = GetComponent<Bars>();
        ChangeState(DollState.Idle);
    }
    private void Start()
    {
        InitiallizeFlags();
    }
    public void ChangeState(DollState newState)
    {
        if (Currentstate == newState) // Si el nuevo estado es el mismo que el actual, no hacemos nada
            return;

        DisableAllState();
        gameStateController.SetFlag(currentStateName, false);
        Currentstate = newState;
        currentEmotion = GetEmotionByState(newState);
        currentStateName = "Doll" + Currentstate.ToString();
        gameStateController.SetFlag(currentStateName, true);

        Debug.Log("Cambiaste al estado: " + Currentstate);

        switch (Currentstate)
        {
            case DollState.Idle:
                idleState.enabled = true;
                break;
            case DollState.Angry:
                angryState.enabled = true;
                break;
            case DollState.Happy:
                happyState.enabled = true;
                break;
            case DollState.Cry:
                cryState.enabled = true;
                break;
            case DollState.Watching:
                watchingState.enabled = true;
                break;  

        }
    }
    private void FixedUpdate()
    {
        currentMaxBar = GetStateByName(bars.getTopBar());
       /* if (currentEmotion == idleState)
        {
            idleState.checkWatching(player.transform.position, doll.transform.position);
            if(currentMaxBar != Currentstate && !isQuestActive)
                ChangeState(currentMaxBar);

        }
        else if (currentEmotion != watchingState)
        {
            currentEmotion.CheckInteraction(audioSource);
            if (currentMaxBar != Currentstate && !isQuestActive)
                ChangeState(currentMaxBar);
        }*/
       if(isQuestActive)
        {
            currentEmotion.CheckInteraction(audioSource);
            if(!isEffectActive)
            {
                screenEffectController.PlayEffect("fatigue");
                isEffectActive = true;
                if (currentMaxBar != Currentstate)
                    ChangeState(currentMaxBar);
            }
            else if (currentMaxBar != DollState.Happy)
            {

                screenEffectController.SetVignetteIntensity("fatigue",currentEmotion.getCurrentBar());
                isEffectActive = false;
                if (currentMaxBar != Currentstate)
                    ChangeState(currentMaxBar);
            }

        }
       else
        {
            if (false)
            {
                screenEffectController.StopEffect("fatigue");
                isEffectActive = false;
            }
            else
            {
                if (currentEmotion != idleState)
                {
                    idleStateCounter = 0;
                    ChangeState(DollState.Idle);
                }
                else
                {
                    if (idleStateCounter < 5)
                    {
                        idleStateCounter++;
                        idleState.checkWatching(player.transform.position, doll.transform.position);
                    }
                    else
                    {
                        bars.InvokeQuest();
                    }
                }
            }
        }
    }
    private DollEmotion GetEmotionByState(DollState state)
    {
        return state switch
        {
            DollState.Idle => idleState,
            DollState.Watching => watchingState,
            DollState.Happy => happyState,
            DollState.Angry => angryState,
            DollState.Cry => cryState,
            _ => null,
        };
    }
    private DollState GetStateByName(string stateName)
    {
        return stateName switch
        {
            "DollIdle" => DollState.Idle,
            "DollWatching" => DollState.Watching,
            "DollHappy" => DollState.Happy,
            "DollAngry" => DollState.Angry,
            "DollCry" => DollState.Cry,
            _ => DollState.Idle,
        };
    }
    private void DisableAllState()
    {
        idleState.enabled = false;
        watchingState.enabled = false;
        happyState.enabled = false;
        angryState.enabled = false;
        cryState.enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Doll.transform.position, Doll.transform.position + Doll.transform.forward * 2f);
    }
    public void InitiallizeFlags()
    {
        gameStateController.SetFlag("DollIdle", true);
        gameStateController.SetFlag("DollAngry", false);
        gameStateController.SetFlag("DollHappy", false);
        gameStateController.SetFlag("DollCry", false);
        gameStateController.SetFlag("DollWatching", false);
    }
    public void SetQuestActive(bool isActive)
    { 
        isQuestActive = isActive;
    }
}
