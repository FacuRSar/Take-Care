using UnityEditor.Rendering;
using UnityEngine;

public class DollEmotionSystem : MonoBehaviour
{
    #region Current State

    private DollState Currentstate;

    public DollState _CurrentState { get { return Currentstate; } set { Currentstate = value; } }
    #endregion

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

    public Camera Player { get => player; set => player = value; }


    void Awake()
    {
        idleState = GetComponent<Idle>();
        watchingState = GetComponent<Watching>();
        happyState = GetComponent<Happy>();
        angryState = GetComponent<Angry>();
        cryState = GetComponent<Cry>();


        ChangeState(DollState.Idle);
    }

    public void ChangeState(DollState newState)
    {
        if (Currentstate == newState) // Si el nuevo estado es el mismo que el actual, no hacemos nada
            return;

        DisableAllState();

        Currentstate = newState;


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

    private void Update()
    {
        if (playerSanity.barSanityCurrent >= playerSanity.barSanityMax * 0.75f)
        {
            // Terror extremo o intenso
        }
        else if (playerSanity.barSanityCurrent >= playerSanity.barSanityMax * 0.5f)
        {
            //Se intensifican los susurros, movimientos más visibles, sensación de ser observado aumenta
        }
        else if (playerSanity.barSanityCurrent >= playerSanity.barSanityMax * 0.25f)
        {

            //Empieza a escuchar susurros, movimientos leves
        }
        else
        {
            // nada
        }
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
}
