using System.Collections;
using UnityEngine;

/* evento de ambiente generico.
*  define el cuando (trigger) y el que (lista de acciones).
*  la idea es armar efectos de ambientacion desde el inspector sin escribir un script por cada cosa:
*  objetos que caen, cosas que aparecen, sonidos, dialogos, shake, etc.
*/
public class AmbientEvent : MonoBehaviour
{
    public enum TriggerMode
    {
        PlayerEnter,
        OnFlag,
        Timed,
        Manual,
        PlayerLook,
        OnInteract
    }

    // id opcional para dispararlo a mano desde el AmbientEventManager
    [SerializeField] private string eventId;

    [Header("Disparo")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.PlayerEnter;

    // solo para OnFlag: cuando esta flag pasa a true, dispara
    [SerializeField] private string requiredFlag;

    // solo para Timed: espera esto al iniciar la escena antes de disparar
    [SerializeField] private float startDelay = 0f;

    // solo para PlayerEnter: filtra para que solo lo dispare el jugador
    [SerializeField] private bool onlyPlayer = true;

    [Header("PlayerLook (mirar un objeto a cierta distancia)")]
    // objeto que el jugador tiene que estar mirando. si lo dejas vacio usa este mismo transform
    [SerializeField] private Transform lookTarget;
    // distancia maxima para que cuente
    [SerializeField] private float lookDistance = 3f;
    // tolerancia de angulo entre la mirada y el objeto (mas chico = hay que apuntar mas justo)
    [SerializeField] private float lookAngle = 15f;
    // cuanto tiempo seguido tiene que mirarlo antes de disparar (0 = al instante)
    [SerializeField] private float lookHoldTime = 0.3f;
    // si esta activo, tira un raycast para confirmar que no haya una pared/objeto tapando
    [SerializeField] private bool requireLineOfSight = true;
    // capas que puede chocar el raycast de linea de vista
    [SerializeField] private LayerMask lookMask = ~0;

    [Header("OnInteract (el jugador interactua con el objeto)")]
    // prompt que se muestra al mirar el objeto. Necesita un Collider en la layer de interaccion.
    [SerializeField] private string interactPrompt = "E - Interactuar";

    [Header("Configuracion")]
    [SerializeField] private bool triggerOnce = true;

    // si esta activo, escribe en consola cuando dispara y que accion corre. para depurar
    [SerializeField] private bool debugLog = false;

    [Header("Acciones")]
    [SerializeField] private AmbientEventAction[] actions;

    private bool fired;
    private float lookTimer;
    private Camera playerCam;

    public string EventId => eventId;

    private void OnEnable()
    {
        if (triggerMode == TriggerMode.OnFlag)
        {
            GameStateController.OnFlagChanged += HandleFlagChanged;
        }

        // en modo interaccion, se asegura de tener el puente Interactable que recibe el "E"
        if (triggerMode == TriggerMode.OnInteract)
        {
            EnsureInteractableBridge();
        }

        // si tiene id queda disponible para dispararlo a mano desde el manager
        if (!string.IsNullOrEmpty(eventId) && AmbientEventManager.Instance != null)
        {
            AmbientEventManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (triggerMode == TriggerMode.OnFlag)
        {
            GameStateController.OnFlagChanged -= HandleFlagChanged;
        }

        if (!string.IsNullOrEmpty(eventId) && AmbientEventManager.Instance != null)
        {
            AmbientEventManager.Instance.Unregister(this);
        }
    }

    private void Start()
    {
        // si la flag ya estaba puesta antes de que este evento existiera, lo disparamos igual
        if (triggerMode == TriggerMode.OnFlag &&
            GameStateController.Instance != null &&
            !string.IsNullOrEmpty(requiredFlag) &&
            GameStateController.Instance.GetFlag(requiredFlag))
        {
            Fire();
            return;
        }

        if (triggerMode == TriggerMode.Timed)
        {
            StartCoroutine(TimedRoutine());
        }
    }

    private IEnumerator TimedRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        Fire();
    }

    private void Update()
    {
        if (triggerMode != TriggerMode.PlayerLook || (triggerOnce && fired))
        {
            return;
        }

        if (IsPlayerLookingAtTarget())
        {
            lookTimer += Time.deltaTime;

            if (lookTimer >= lookHoldTime)
            {
                Fire();
            }
        }
        else
        {
            // si deja de mirar, el contador se reinicia
            lookTimer = 0f;
        }
    }

    private bool IsPlayerLookingAtTarget()
    {
        Transform target = lookTarget != null ? lookTarget : transform;

        Camera cam = GetPlayerCamera();
        if (cam == null)
        {
            return false;
        }

        Vector3 toTarget = target.position - cam.transform.position;
        float distance = toTarget.magnitude;

        if (distance > lookDistance)
        {
            return false;
        }

        // angulo entre hacia donde mira la camara y hacia donde esta el objeto
        float angle = Vector3.Angle(cam.transform.forward, toTarget);
        if (angle > lookAngle)
        {
            return false;
        }

        // confirmamos que no haya algo tapando al objeto
        if (requireLineOfSight && Physics.Raycast(cam.transform.position, toTarget.normalized, out RaycastHit hit, lookDistance, lookMask, QueryTriggerInteraction.Ignore))
        {
            bool hitIsTarget = hit.transform == target || hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform);

            if (!hitIsTarget)
            {
                return false;
            }
        }

        return true;
    }

    private Camera GetPlayerCamera()
    {
        if (playerCam == null)
        {
            playerCam = Camera.main;
        }

        return playerCam;
    }

    private void HandleFlagChanged(string flagName, bool value)
    {
        if (!value || flagName != requiredFlag)
        {
            return;
        }

        Fire();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.PlayerEnter)
        {
            return;
        }

        if (onlyPlayer && !other.CompareTag("Player"))
        {
            return;
        }

        Fire();
    }

    // disparo manual desde el manager
    public void TriggerManually()
    {
        Fire();
    }

    // lo llama el puente AmbientEventInteractable cuando el jugador interactua con el objeto
    public void FireFromInteraction()
    {
        Fire();
    }

    // se asegura de tener (o crea) el componente Interactable que recibe la interaccion del jugador
    private void EnsureInteractableBridge()
    {
        AmbientEventInteractable bridge = GetComponent<AmbientEventInteractable>();

        if (bridge == null)
        {
            bridge = gameObject.AddComponent<AmbientEventInteractable>();
        }

        bridge.Bind(this, interactPrompt);
    }

    private void Fire()
    {
        if (triggerOnce && fired)
        {
            return;
        }

        fired = true;

        if (debugLog)
        {
            int count = actions != null ? actions.Length : 0;
            Debug.Log("[AmbientEvent] '" + name + "' disparado. Acciones: " + count);
        }

        if (actions == null)
        {
            return;
        }

        foreach (AmbientEventAction action in actions)
        {
            if (action != null && action.delay > 0f)
            {
                StartCoroutine(RunActionDelayed(action));
            }
            else
            {
                RunAction(action);
            }
        }
    }

    private IEnumerator RunActionDelayed(AmbientEventAction action)
    {
        yield return new WaitForSeconds(action.delay);
        RunAction(action);
    }

    private void RunAction(AmbientEventAction action)
    {
        if (action == null)
        {
            return;
        }

        if (debugLog)
        {
            Debug.Log("[AmbientEvent] '" + name + "' ejecuta accion: " + action.type);
        }

        switch (action.type)
        {
            case AmbientActionType.EnablePhysics:
                DoEnablePhysics(action);
                break;

            case AmbientActionType.SetActive:
                if (action.target != null)
                {
                    action.target.SetActive(action.setActiveValue);
                }
                break;

            case AmbientActionType.MoveTo:
                if (action.target != null)
                {
                    StartCoroutine(MoveRoutine(action));
                }
                break;

            case AmbientActionType.PlaySfx:
                DoPlaySfx(action);
                break;

            case AmbientActionType.PlayDialogue:
                if (DialogueController.Instance != null && !string.IsNullOrEmpty(action.dialogueId))
                {
                    DialogueController.Instance.PlayDialogue(action.dialogueId);
                }
                break;

            case AmbientActionType.ShakeCamera:
                if (ScreenEffectController.Instance != null && !string.IsNullOrEmpty(action.shakeEffectId))
                {
                    ScreenEffectController.Instance.PlayEffect(action.shakeEffectId);
                }
                break;

            case AmbientActionType.ScreenEffect:
                DoScreenEffect(action);
                break;

            case AmbientActionType.SpawnPrefab:
                if (action.prefab != null)
                {
                    Vector3 pos = action.spawnPoint != null ? action.spawnPoint.position : transform.position;
                    Quaternion rot = action.spawnPoint != null ? action.spawnPoint.rotation : transform.rotation;
                    Instantiate(action.prefab, pos, rot);
                }
                break;

            case AmbientActionType.SetFlag:
                if (GameStateController.Instance != null && !string.IsNullOrEmpty(action.flagName))
                {
                    GameStateController.Instance.SetFlag(action.flagName, action.flagValue);
                }
                break;

            case AmbientActionType.SetText:
                if (action.textTarget != null)
                {
                    // pone el texto, o lo borra si textValue quedo vacio
                    action.textTarget.text = action.textValue;
                }
                break;
        }
    }

    private void DoEnablePhysics(AmbientEventAction action)
    {
        if (action.target == null)
        {
            return;
        }

        GrabbableObject grabbable = action.target.GetComponent<GrabbableObject>();

        if (grabbable != null)
        {
            grabbable.EnablePhysicsFromAmbient(action.physicsImpulse);
            return;
        }

        Rigidbody rb = action.target.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning("[AmbientEvent] EnablePhysics: el target no tiene Rigidbody: " + action.target.name);
            return;
        }

        if (!HasSolidCollider(action.target))
        {
            Debug.LogWarning("[AmbientEvent] EnablePhysics: el target no tiene collider solido: " + action.target.name);
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        if (action.physicsImpulse != Vector3.zero)
        {
            rb.AddForce(action.physicsImpulse, ForceMode.Impulse);
        }
    }

    private bool HasSolidCollider(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null && col.enabled && !col.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void DoScreenEffect(AmbientEventAction action)
    {
        if (ScreenEffectController.Instance == null || string.IsNullOrEmpty(action.screenEffectId))
        {
            Debug.LogWarning("[AmbientEvent] ScreenEffect: falta ScreenEffectController o screenEffectId en " + name + ".");
            return;
        }

        // si la accion es para apagar, lo apaga al instante y listo
        if (action.stopScreenEffect)
        {
            ScreenEffectController.Instance.StopEffect(action.screenEffectId, 0f);
            return;
        }

        ScreenEffectController.Instance.PlayEffect(action.screenEffectId);

        // flash: lo prendemos y lo apagamos solo despues de un toque (ej: 0.01s para un susto)
        if (action.screenEffectAutoStop > 0f)
        {
            StartCoroutine(AutoStopScreenEffect(action.screenEffectId, action.screenEffectAutoStop));
        }
    }

    private IEnumerator AutoStopScreenEffect(string effectId, float time)
    {
        yield return new WaitForSeconds(time);
        ScreenEffectController.Instance.StopEffect(effectId, 0f);
    }

    private void DoPlaySfx(AmbientEventAction action)
    {
        if (SFXManager.Instance == null)
        {
            Debug.LogWarning("[AmbientEvent] PlaySfx: no hay SFXManager en escena.");
            return;
        }

        if (string.IsNullOrEmpty(action.sfxId))
        {
            Debug.LogWarning("[AmbientEvent] PlaySfx: sfxId esta vacio en " + name + ".");
            return;
        }

        AudioClip playedClip;

        if (action.sfx3D)
        {
            Vector3 soundPosition = action.target != null ? action.target.transform.position : transform.position;
            playedClip = SFXManager.Instance.Play3D(action.sfxId, soundPosition);
        }
        else
        {
            playedClip = SFXManager.Instance.Play2D(action.sfxId);
        }

        if (playedClip == null)
        {
            Debug.LogWarning("[AmbientEvent] PlaySfx: no se pudo reproducir '" + action.sfxId +
                "'. Revisa que exista un pool con ese id en SFXManager y que tenga clips cargados.");
        }
    }

    private IEnumerator MoveRoutine(AmbientEventAction action)
    {
        Transform t = action.target.transform;

        Vector3 fromPos = action.moveLocal ? t.localPosition : t.position;
        Vector3 toPos = action.moveTarget;

        Quaternion fromRot = action.moveLocal ? t.localRotation : t.rotation;
        Quaternion toRot = Quaternion.Euler(action.rotateTarget);

        // si no hay duracion lo dejamos en el destino de una
        if (action.moveDuration <= 0f)
        {
            ApplyMove(t, action.moveLocal, toPos);
            if (action.alsoRotate)
            {
                ApplyRotation(t, action.moveLocal, toRot);
            }
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < action.moveDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / action.moveDuration);

            ApplyMove(t, action.moveLocal, Vector3.Lerp(fromPos, toPos, k));

            if (action.alsoRotate)
            {
                ApplyRotation(t, action.moveLocal, Quaternion.Slerp(fromRot, toRot, k));
            }

            yield return null;
        }

        ApplyMove(t, action.moveLocal, toPos);

        if (action.alsoRotate)
        {
            ApplyRotation(t, action.moveLocal, toRot);
        }
    }

    private void ApplyMove(Transform t, bool local, Vector3 value)
    {
        if (local)
        {
            t.localPosition = value;
        }
        else
        {
            t.position = value;
        }
    }

    private void ApplyRotation(Transform t, bool local, Quaternion value)
    {
        if (local)
        {
            t.localRotation = value;
        }
        else
        {
            t.rotation = value;
        }
    }
}
