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
        Manual
    }

    [Header("Identificacion")]
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

    [Header("Configuracion")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Acciones")]
    [SerializeField] private AmbientEventAction[] actions;

    private bool fired;

    public string EventId => eventId;

    private void OnEnable()
    {
        if (triggerMode == TriggerMode.OnFlag)
        {
            GameStateController.OnFlagChanged += HandleFlagChanged;
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

    private void Fire()
    {
        if (triggerOnce && fired)
        {
            return;
        }

        fired = true;

        if (actions == null)
        {
            return;
        }

        foreach (AmbientEventAction action in actions)
        {
            RunAction(action);
        }
    }

    private void RunAction(AmbientEventAction action)
    {
        if (action == null)
        {
            return;
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
        }
    }

    private void DoEnablePhysics(AmbientEventAction action)
    {
        if (action.target == null)
        {
            return;
        }

        Rigidbody rb = action.target.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning("[AmbientEvent] EnablePhysics: el target no tiene Rigidbody: " + action.target.name);
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        if (action.physicsImpulse != Vector3.zero)
        {
            rb.AddForce(action.physicsImpulse, ForceMode.Impulse);
        }
    }

    private void DoPlaySfx(AmbientEventAction action)
    {
        if (SFXManager.Instance == null || string.IsNullOrEmpty(action.sfxId))
        {
            return;
        }

        if (action.sfx3D)
        {
            SFXManager.Instance.Play3D(action.sfxId, transform.position);
        }
        else
        {
            SFXManager.Instance.Play2D(action.sfxId);
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
