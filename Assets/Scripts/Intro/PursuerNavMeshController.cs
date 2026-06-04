using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/* Controlador del stalker.
 * Persigue al jugador con NavMesh, reproduce pasos y puede intentar abrir puertas.
 */
[RequireComponent(typeof(NavMeshAgent))]
public class PursuerNavMeshController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    // si player queda sin asignar, lo busca por tag al activarse
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Animator animator;

    [Header("Movimiento")]
    [SerializeField] private float destinationUpdateRate = 0.25f;
    [SerializeField] private float grabDistance = 1.3f;

    [Header("Aumento progresivo de velocidad")]
    // Cada segundo la velocidad del agente se multiplica por este factor.
    // 1 = no acelera. 1.05 = +5% por segundo. 1.1 = +10% por segundo.
    [SerializeField] private float speedMultiplierPerSecond = 1f;
    // Tope maximo de velocidad. Conviene que sea mayor a la velocidad del jugador
    // para evitar que el jugador escape dando vueltas indefinidamente.
    [SerializeField] private float maxSpeed = 10f;
    // Segundos a esperar antes de empezar la rampa de velocidad.
    [SerializeField] private float speedRampDelay = 0f;

    [Header("Puertas")]
    [SerializeField] private float doorDetectionDistance = 1.2f;
    [SerializeField] private LayerMask doorMask;
    [SerializeField] private float doorInteractCooldown = 1.5f;

    [Header("Pasos")]
    [SerializeField] private AudioSource footstepLoopSource;
    [SerializeField] private float minSpeedForFootsteps = 0.15f;

    [Header("Captura")]
    [SerializeField] private float behindDotThreshold = -0.35f;
    [SerializeField] private string backGrabTriggerName = "BackGrab";
    [SerializeField] private string frontGrabTriggerName = "FrontGrab";
    [SerializeField] private bool logGrabDistance = false;

    [Header("Cierre al capturar")]
    // controlador del cierre (mismo final sin importar si agarra de frente o de espalda)
    [SerializeField] private DemoEndController captureEndController;
    // pequeña espera para que arranque la animacion de agarre antes del fade
    [SerializeField] private float captureEndDelay = 0.4f;

    private NavMeshAgent agent;
    private float nextDestinationUpdateTime;
    private float nextDoorInteractTime;
    private bool hasGrabbedPlayer;
    private float baseSpeed;
    private float rampStartTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (agent != null)
        {
            baseSpeed = agent.speed;
        }
    }

    private void OnEnable()
    {
        hasGrabbedPlayer = false;
        rampStartTime = Time.time;

        // si no se asigno el player en el inspector, lo busco por tag
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);

            if (found != null)
            {
                player = found.transform;
            }
            else
            {
                Debug.LogWarning("PursuerNavMeshController: no hay player asignado ni se encontro por tag '" + playerTag + "'.");
            }
        }

        if (agent != null)
        {
            agent.speed = baseSpeed;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        StopFootstepLoop();
    }

    private void OnDisable()
    {
        StopFootstepLoop();
    }

    private void Update()
    {
        if (player == null || hasGrabbedPlayer)
        {
            StopFootstepLoop();
            return;
        }

        UpdateSpeedRamp();
        UpdateDestination();
        TryOpenDoorAhead();
        HandleFootstepLoop();
        CheckGrabPlayer();
    }

    private void UpdateSpeedRamp()
    {
        if (agent == null || speedMultiplierPerSecond <= 1f)
        {
            return;
        }

        float elapsed = Time.time - rampStartTime - speedRampDelay;

        if (elapsed <= 0f)
        {
            agent.speed = baseSpeed;
            return;
        }

        float multiplier = Mathf.Pow(speedMultiplierPerSecond, elapsed);
        agent.speed = Mathf.Min(maxSpeed, baseSpeed * multiplier);
    }

    // Permite que sistemas externos resetee la rampa de velocidad
    // (por ej. cuando el jugador entra a una zona segura o reinicia la persecucion).
    public void ResetSpeedRamp()
    {
        rampStartTime = Time.time;

        if (agent != null)
        {
            agent.speed = baseSpeed;
        }
    }

    private void UpdateDestination()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        if (Time.time < nextDestinationUpdateTime)
        {
            return;
        }

        nextDestinationUpdateTime = Time.time + destinationUpdateRate;
        agent.SetDestination(player.position);
    }

    private void TryOpenDoorAhead()
    {
        if (Time.time < nextDoorInteractTime)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, doorDetectionDistance, doorMask))
        {
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();

            if (door != null)
            {
                nextDoorInteractTime = Time.time + doorInteractCooldown;

                // DoorInteractable actualmente no necesita PlayerInteraction para abrir,
                // por eso mandamos null.
                if (!door.IsOpen() && !door.IsMoving())
                {
                    door.OpenFromAI();
                }

                Debug.Log("PursuerNavMeshController: stalker intento abrir puerta: " + door.gameObject.name);
            }
        }
    }

    private void HandleFootstepLoop()
    {
        if (footstepLoopSource == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            StopFootstepLoop();
            return;
        }

        bool isMoving = agent.velocity.magnitude > minSpeedForFootsteps && !agent.isStopped;

        if (isMoving)
        {
            if (!footstepLoopSource.isPlaying)
            {
                footstepLoopSource.loop = true;
                footstepLoopSource.Play();
            }
        }
        else
        {
            StopFootstepLoop();
        }
    }

    private void StopFootstepLoop()
    {
        if (footstepLoopSource != null && footstepLoopSource.isPlaying)
        {
            footstepLoopSource.Stop();
        }
    }

    private void CheckGrabPlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (logGrabDistance)
        {
            Debug.Log("PursuerNavMeshController: distancia al player = " + distance.ToString("0.00") + " | grabDistance = " + grabDistance);
        }

        if (distance > grabDistance)
        {
            return;
        }

        hasGrabbedPlayer = true;

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        StopFootstepLoop();

        Vector3 playerToPursuer = (transform.position - player.position).normalized;
        float dot = Vector3.Dot(player.forward, playerToPursuer);

        if (dot < behindDotThreshold)
        {
            GrabFromBehind();
        }
        else
        {
            GrabFromFront();
        }

        // El cierre es el mismo en ambos casos.
        TriggerCaptureEnding();
    }

    private void TriggerCaptureEnding()
    {
        Debug.Log("PursuerNavMeshController: captura detectada, disparando cierre.");

        if (captureEndController == null)
        {
            Debug.LogWarning("PursuerNavMeshController: no hay captureEndController asignado para el cierre.");
            return;
        }

        if (captureEndDelay > 0f)
        {
            StartCoroutine(CaptureEndAfterDelay());
        }
        else
        {
            captureEndController.StartCaptureEnd();
        }
    }

    private IEnumerator CaptureEndAfterDelay()
    {
        yield return new WaitForSeconds(captureEndDelay);

        if (captureEndController != null)
        {
            captureEndController.StartCaptureEnd();
        }
    }

    private void GrabFromBehind()
    {
        Debug.Log("PursuerNavMeshController: captura de espalda.");

        if (animator != null)
        {
            animator.SetTrigger(backGrabTriggerName);
        }

        // Despues conectamos aca FixedCameraWithZoom.
    }

    private void GrabFromFront()
    {
        Debug.Log("PursuerNavMeshController: captura de frente.");

        if (animator != null)
        {
            animator.SetTrigger(frontGrabTriggerName);
        }

        // Despues conectamos aca animacion de manos tapando la vista.
    }
}