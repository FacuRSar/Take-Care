using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* IA de patrulla del Pursuer (distinta a la del final).
*  No persigue de entrada: aparece y vaga buscando al jugador.
*
*  Espacio chico (PursuerPatrolPoint con spaciousArea = false):
*    vaga; si cruza una puerta cerrada la abre, mira un par de segundos y la cierra.
*    Si no encuentra al jugador, el spawner lo guarda al cumplirse su duracion.
*
*  Espacio grande (spaciousArea = true):
*    vaga durante su duracion y ademas va a abrir deliberadamente entre 1 y 3 puertas.
*    Con cierta probabilidad va directo a la puerta del cuarto donde esta el jugador
*    (lo sabe por las RoomZone).
*
*  En cualquier momento, si ve o escucha al jugador, lo persigue. Si lo pierde, va al
*  ultimo lugar visto y despues vuelve a vagar.
*/
[RequireComponent(typeof(NavMeshAgent))]
public class PursuerPatrolController : MonoBehaviour
{
    private enum State { Wander, GoToDoor, Chase, GoToLastSeen }

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Velocidades")]
    [SerializeField] private float wanderSpeed = 2.2f;
    [SerializeField] private float chaseSpeed = 5.5f;

    [Header("Vagar")]
    // cuanto se queda quieto en cada punto antes de elegir el proximo (pausa, da sensacion de buscar)
    [SerializeField] private float wanderPointInterval = 1.5f;
    // que tan lejos busca el proximo punto, medido desde donde esta parado (no desde el spawn).
    // asi recorre el lugar en vez de orbitar el punto de aparicion.
    [SerializeField] private float roamRadius = 10f;
    // distancia minima del proximo punto: evita que elija puntos pegados y de vueltas en el lugar
    [SerializeField] private float minWanderDistance = 4f;
    // a que distancia considera que llego al punto de paseo
    [SerializeField] private float wanderArrivalDistance = 0.8f;

    [Header("Vision")]
    // distancia maxima a la que puede ver al jugador
    [SerializeField] private float viewDistance = 9f;
    // angulo del cono de vision (mitad para cada lado)
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private LayerMask sightBlockMask = ~0;
    // desde donde "mira" (altura de los ojos aprox)
    [SerializeField] private float eyeHeight = 1.5f;

    [Header("Deteccion por movimiento")]
    // si el jugador esta mas cerca que esto y se mueve fuerte, lo detecta aunque no lo vea de frente
    [SerializeField] private float noiseDetectRadius = 4f;
    // velocidad del jugador a partir de la cual se considera que hace ruido
    [SerializeField] private float loudMoveSpeed = 3f;

    [Header("Perdida de vista")]
    // cuanto sigue yendo al ultimo punto visto antes de rendirse y volver a vagar
    [SerializeField] private float giveUpTime = 4f;

    [Header("Puertas")]
    // raycast para abrir puertas que cruza de frente mientras vaga
    [SerializeField] private float doorDetectionDistance = 1.2f;
    [SerializeField] private LayerMask doorMask;
    // cuanto se queda mirando con la puerta abierta antes de cerrarla y seguir
    [SerializeField] private float doorPeekTime = 2f;
    // distancia a la que considera que "llego" a una puerta a la que iba a proposito
    [SerializeField] private float doorReachDistance = 1.8f;
    // si no llega a una puerta planificada en este tiempo, la abandona
    [SerializeField] private float doorTravelMaxTime = 8f;
    // mientras persigue, abre (sin frenarse a espiar) las puertas cerradas que tiene a esta distancia
    [SerializeField] private float chaseDoorOpenDistance = 1.6f;

    [Header("Puertas planificadas (solo espacio grande)")]
    // cuantas puertas va a abrir a proposito en un espacio grande
    [SerializeField] private int minPlannedDoors = 1;
    [SerializeField] private int maxPlannedDoors = 3;
    // probabilidad de que una de esas puertas sea la del cuarto donde esta el jugador
    [SerializeField, Range(0f, 1f)] private float chanceTargetPlayerRoom = 0.3f;
    // radio para juntar puertas candidatas alrededor del punto de aparicion
    [SerializeField] private float doorSearchRadius = 25f;

    [Header("Captura")]
    [SerializeField] private float grabDistance = 1.3f;
    [SerializeField] private DemoEndController captureEndController;
    [SerializeField] private float captureEndDelay = 0.4f;

    [Header("Animator")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Pasos")]
    [SerializeField] private AudioSource footstepLoopSource;
    [SerializeField] private float minSpeedForFootsteps = 0.15f;

    [Header("Debug deteccion")]
    // dibuja los radios de vision/oido/captura al seleccionar el objeto
    [SerializeField] private bool debugDetection = false;

    private NavMeshAgent agent;
    private State state;
    private float nextWanderTime;
    private bool waitingAtPoint;
    private float lostTimer;
    private Vector3 lastSeenPosition;
    private bool hasGrabbedPlayer;

    // puertas que va a visitar a proposito (espacio grande)
    private readonly Queue<DoorInteractable> plannedDoors = new Queue<DoorInteractable>();
    private DoorInteractable currentDoorTarget;
    private float doorTravelDeadline;

    // manejo del momento en que mira por una puerta abierta y la cierra
    private DoorInteractable peekingDoor;
    private float doorPeekEndTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        // seguridad: si por error comparte objeto con la IA del final, apago esa para que no peleen por el agente
        PursuerNavMeshController chaser = GetComponent<PursuerNavMeshController>();

        if (chaser != null && chaser.enabled)
        {
            chaser.enabled = false;
            Debug.LogWarning("PursuerPatrolController: habia un PursuerNavMeshController activo en el mismo objeto. Lo desactive para que no se superpongan.");
        }

        hasGrabbedPlayer = false;
        ResolvePlayer();
        StopFootstepLoop();
    }

    private void OnDisable()
    {
        StopFootstepLoop();
        peekingDoor = null;
        currentDoorTarget = null;
        plannedDoors.Clear();
    }

    private void ResolvePlayer()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);

            if (found != null)
            {
                player = found.transform;
            }
        }

        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    // El spawner llama esto al aparecer. En espacio grande, planifica las puertas a abrir.
    public void BeginPatrol(PursuerPatrolPoint point)
    {
        plannedDoors.Clear();
        currentDoorTarget = null;
        peekingDoor = null;

        roamRadius = point.WanderRadius;

        if (point.SpaciousArea)
        {
            PlanDoors(point.transform.position);
        }

        SetState(State.Wander);
        PickNewWanderPoint();
    }

    // Junta puertas candidatas cerca del punto, elige entre min y max, y con cierta
    // probabilidad mete adelante la puerta del cuarto donde esta el jugador.
    private void PlanDoors(Vector3 center)
    {
        int target = Random.Range(Mathf.Max(0, minPlannedDoors), Mathf.Max(minPlannedDoors, maxPlannedDoors) + 1);

        if (target <= 0)
        {
            return;
        }

        DoorInteractable[] all = FindObjectsOfType<DoorInteractable>();
        List<DoorInteractable> candidates = new List<DoorInteractable>();

        foreach (DoorInteractable door in all)
        {
            if (door == null)
            {
                continue;
            }

            if (door.ExcludeFromAIPatrol)
            {
                continue;
            }

            if (Vector3.Distance(door.transform.position, center) <= doorSearchRadius)
            {
                candidates.Add(door);
            }
        }

        // shuffle simple
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            DoorInteractable tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        for (int i = 0; i < target && i < candidates.Count; i++)
        {
            plannedDoors.Enqueue(candidates[i]);
        }

        // chance de ir directo al cuarto del jugador: la metemos primera en la cola
        if (Random.value <= chanceTargetPlayerRoom
            && RoomZone.CurrentPlayerRoom != null
            && RoomZone.CurrentPlayerRoom.EntranceDoor != null
            && !RoomZone.CurrentPlayerRoom.EntranceDoor.ExcludeFromAIPatrol)
        {
            Queue<DoorInteractable> reordered = new Queue<DoorInteractable>();
            reordered.Enqueue(RoomZone.CurrentPlayerRoom.EntranceDoor);

            while (plannedDoors.Count > 0)
            {
                DoorInteractable d = plannedDoors.Dequeue();

                if (d != RoomZone.CurrentPlayerRoom.EntranceDoor)
                {
                    reordered.Enqueue(d);
                }
            }

            plannedDoors.Clear();

            foreach (DoorInteractable d in reordered)
            {
                plannedDoors.Enqueue(d);
            }
        }
    }

    private void Update()
    {
        if (player == null || hasGrabbedPlayer || agent == null || !agent.isOnNavMesh)
        {
            UpdateAnimatorSpeed(0f);
            StopFootstepLoop();
            return;
        }

        // si esta mirando por una puerta abierta, se queda quieto hasta que termina de espiar
        if (peekingDoor != null)
        {
            HandleDoorPeek();
            UpdateAnimatorSpeed(GetCurrentMoveSpeed());
            HandleFootstepLoop();
            return;
        }

        bool sees = CanSeePlayer() || HearsPlayer();

        if (sees && state != State.Chase)
        {
            SetState(State.Chase);
        }

        switch (state)
        {
            case State.Wander:
                TickWander();
                TryPeekDoorAhead();
                break;

            case State.GoToDoor:
                TickGoToDoor();
                break;

            case State.Chase:
                if (sees)
                {
                    lastSeenPosition = player.position;
                    agent.SetDestination(player.position);
                }
                else
                {
                    lostTimer = giveUpTime;
                    SetState(State.GoToLastSeen);
                }
                break;

            case State.GoToLastSeen:
                if (sees)
                {
                    SetState(State.Chase);
                }
                else
                {
                    TickGoToLastSeen();
                }
                break;
        }

        // mientras persigue, abre las puertas que cruza en vez de atravesarlas
        if (state == State.Chase || state == State.GoToLastSeen)
        {
            OpenDoorAheadWhileChasing();
        }

        UpdateAnimatorSpeed(GetCurrentMoveSpeed());
        HandleFootstepLoop();
        CheckGrabPlayer();
    }

    private void SetState(State next)
    {
        state = next;

        switch (next)
        {
            case State.Wander:
                agent.speed = wanderSpeed;
                agent.isStopped = false;
                nextWanderTime = 0f;
                break;

            case State.GoToDoor:
                agent.speed = wanderSpeed;
                agent.isStopped = false;
                doorTravelDeadline = Time.time + doorTravelMaxTime;

                if (currentDoorTarget != null)
                {
                    agent.SetDestination(currentDoorTarget.transform.position);
                }
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                currentDoorTarget = null;
                lastSeenPosition = player.position;
                agent.SetDestination(player.position);
                break;

            case State.GoToLastSeen:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                agent.SetDestination(lastSeenPosition);
                break;
        }
    }

    private void TickWander()
    {
        // si hay puertas planificadas, va a la proxima en vez de seguir vagando al azar
        if (TryStartNextPlannedDoor())
        {
            return;
        }

        // todavia esta calculando o yendo hacia el punto elegido: lo dejamos llegar (no cambia de rumbo)
        if (agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance > wanderArrivalDistance)
        {
            waitingAtPoint = false;
            return;
        }

        // llego al punto: hace una pausa corta y recien despues elige otro punto concreto
        if (!waitingAtPoint)
        {
            waitingAtPoint = true;
            nextWanderTime = Time.time + wanderPointInterval;
            return;
        }

        if (Time.time >= nextWanderTime)
        {
            waitingAtPoint = false;
            PickNewWanderPoint();
        }
    }

    private bool TryStartNextPlannedDoor()
    {
        while (plannedDoors.Count > 0)
        {
            DoorInteractable next = plannedDoors.Dequeue();

            if (next == null || next.IsOpen())
            {
                continue;
            }

            currentDoorTarget = next;
            SetState(State.GoToDoor);
            return true;
        }

        return false;
    }

    private void TickGoToDoor()
    {
        if (currentDoorTarget == null)
        {
            SetState(State.Wander);
            return;
        }

        // si no llega en el tiempo previsto, abandona esa puerta
        if (Time.time >= doorTravelDeadline)
        {
            currentDoorTarget = null;
            SetState(State.Wander);
            PickNewWanderPoint();
            return;
        }

        agent.SetDestination(currentDoorTarget.transform.position);

        float distance = Vector3.Distance(transform.position, currentDoorTarget.transform.position);

        if (distance <= doorReachDistance)
        {
            StartPeek(currentDoorTarget);
            currentDoorTarget = null;
        }
    }

    private void PickNewWanderPoint()
    {
        waitingAtPoint = false;

        // busca un punto concreto a una distancia minima, en una direccion al azar, sobre el navmesh.
        // varios intentos por si la primera direccion da contra una pared.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minWanderDistance, Mathf.Max(minWanderDistance, roamRadius));
            Vector3 candidate = transform.position + new Vector3(dir.x, 0f, dir.y) * dist;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void TickGoToLastSeen()
    {
        lostTimer -= Time.deltaTime;

        // llego al ultimo punto visto o se quedo sin tiempo: vuelve a vagar
        if (lostTimer <= 0f || (!agent.pathPending && agent.remainingDistance <= 0.8f))
        {
            SetState(State.Wander);
            PickNewWanderPoint();
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = (player.position + Vector3.up * 1f) - eye;
        float distance = toPlayer.magnitude;

        if (distance > viewDistance)
        {
            return false;
        }

        float angle = Vector3.Angle(transform.forward, toPlayer);

        if (angle > viewAngle)
        {
            return false;
        }

        // si hay algo solido entre la vieja y el jugador, no lo ve (escondido detras de objetos)
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, viewDistance, sightBlockMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.CompareTag(playerTag) && !hit.transform.IsChildOf(player))
            {
                return false;
            }
        }

        return true;
    }

    private bool HearsPlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > noiseDetectRadius)
        {
            return false;
        }

        if (playerMovement == null)
        {
            return false;
        }

        // lo escucha si se mueve por encima del umbral de ruido
        return playerMovement.IsMoving && playerMovement.CurrentSpeed >= loudMoveSpeed;
    }

    private void TryPeekDoorAhead()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, doorDetectionDistance, doorMask))
        {
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();

            if (door != null && !door.ExcludeFromAIPatrol && !door.IsOpen() && !door.IsMoving())
            {
                StartPeek(door);
            }
        }
    }

    // Durante la persecucion: si tiene una puerta cerrada justo delante, la abre y sigue (no se frena).
    private void OpenDoorAheadWhileChasing()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, chaseDoorOpenDistance, doorMask))
        {
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();

            if (door != null && !door.ExcludeFromAIPatrol && !door.IsOpen() && !door.IsMoving())
            {
                door.OpenFromAI();
            }
        }
    }

    // Abre la puerta, mira un par de segundos y se queda quieto hasta que HandleDoorPeek la cierra.
    private void StartPeek(DoorInteractable door)
    {
        door.OpenFromAI();
        TriggerAttackAnimation();

        peekingDoor = door;
        doorPeekEndTime = Time.time + doorPeekTime;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private void HandleDoorPeek()
    {
        // si mientras espia ve al jugador, deja la puerta y lo persigue
        if (CanSeePlayer() || HearsPlayer())
        {
            agent.isStopped = false;
            peekingDoor = null;
            SetState(State.Chase);
            return;
        }

        if (Time.time >= doorPeekEndTime)
        {
            peekingDoor.ForceClose();
            peekingDoor = null;
            agent.isStopped = false;
            SetState(State.Wander);
            PickNewWanderPoint();
        }
    }

    private float GetCurrentMoveSpeed()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped)
        {
            return 0f;
        }

        return agent.velocity.magnitude;
    }

    private void UpdateAnimatorSpeed(float speed)
    {
        if (animator == null || string.IsNullOrEmpty(speedParameterName))
        {
            return;
        }

        if (speedDampTime > 0f)
        {
            animator.SetFloat(speedParameterName, speed, speedDampTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    private void TriggerAttackAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }
    }

    private void HandleFootstepLoop()
    {
        if (footstepLoopSource == null)
        {
            return;
        }

        bool moving = GetCurrentMoveSpeed() > minSpeedForFootsteps;

        if (moving)
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
        if (Vector3.Distance(transform.position, player.position) > grabDistance)
        {
            return;
        }

        hasGrabbedPlayer = true;

        // se frena en seco
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        StopFootstepLoop();

        // queda mirando al jugador de frente
        FacePlayer();

        // y lo deja sin poder moverse
        FreezePlayer();

        TriggerAttackAnimation();
        TriggerCaptureEnding();
    }

    private void FacePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(toPlayer);
        }
    }

    private void FreezePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.CantMove(true);
        }
    }

    private void TriggerCaptureEnding()
    {
        if (captureEndController == null)
        {
            Debug.LogWarning("PursuerPatrolController: no hay captureEndController asignado.");
            return;
        }

        if (captureEndDelay > 0f)
        {
            Invoke(nameof(StartCaptureEndInvoke), captureEndDelay);
        }
        else
        {
            captureEndController.StartCaptureEnd();
        }
    }

    private void StartCaptureEndInvoke()
    {
        if (captureEndController != null)
        {
            captureEndController.StartCaptureEnd();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDetection)
        {
            return;
        }

        // radio de oido (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noiseDetectRadius);

        // radio y cono de vision (cyan)
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eye, viewDistance);

        Vector3 left = Quaternion.Euler(0f, -viewAngle, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle, 0f) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * viewDistance);
        Gizmos.DrawLine(eye, eye + right * viewDistance);

        // grab distance (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, grabDistance);

        // linea al jugador, verde si lo escucharia por cercania
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Gizmos.color = distance <= noiseDetectRadius ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
