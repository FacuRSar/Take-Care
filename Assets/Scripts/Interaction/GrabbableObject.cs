using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Configuracion compartida del agarre. Vive una sola vez en PlayerInteraction y se le
// pasa a cada objeto al agarrarlo, asi no hay que repetir los valores en cada prop.
[System.Serializable]
public class GrabSettings
{
    public float minDistance = 0.6f;
    public float maxDistance = 2.5f;
    public float defaultDistance = 1f;
    public float scrollStep = 0.15f;
    public float rotateSensitivity = 0.4f;
}

/*  esta logica mantiene la mecanica original de agarrar/soltar objetos
*   pero reorganizada para separar responsabilidades y facilitar mantenimiento
*/
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour
{
    [SerializeField] private string _objectName;
    [SerializeField] private int _objectID;
    public string objectName => _objectName;
    public int objectID => _objectID;

    public Sprite itemIcon;

    [Header("Configuraciones de objeto agarrado")]
    [SerializeField] private Vector3 heldLocalPosition = Vector3.zero;
    //posicion local que va a tener el objeto mientras esta agarrado

    [SerializeField] private Vector3 heldLocalRotation = Vector3.zero;
    //rotacion local en grados mientras esta agarrado

    [Header("Configuraciones de seguimiento")]
    [SerializeField] private float followPositionSpeed = 20f;
    //velocidad con la que el objeto persigue la posicion del HandPoint

    [SerializeField] private float followRotationSpeed = 20f;
    //velocidad con la que el objeto persigue la rotacion del HandPoint

    [SerializeField] private float maxFollowVelocity = 15f;
    //limite de velocidad para que no salga disparado si se desacomoda

    [Header("Opciones de este objeto")]
    [SerializeField] private bool canBeGrabbed = true;
    //si esta destildado el objeto no se puede agarrar (ej: algo muy pesado) y muestra el mensaje de abajo

    [TextArea]
    [SerializeField] private string cannotGrabMessage = "Esto pesa demasiado.";
    //mensaje que aparece al intentar agarrar algo que no es agarrable

    [SerializeField] private bool lockRotation = false;
    //si esta tildado, no se puede rotar con click derecho

    [SerializeField] private bool useFixedGrabRotation = false;
    //si esta tildado, al agarrarlo siempre toma la rotacion de "heldLocalRotation"

    [SerializeField] private bool useCustomDistance = false;
    //si esta tildado, usa "customHoldDistance" como distancia inicial en vez de la default

    [SerializeField] private float customHoldDistance = 1f;
    //distancia inicial propia de este objeto (solo se usa si useCustomDistance esta tildado)

    [Header("Sonidos (opcionales)")]
    [SerializeField] private string pickupSfxId;
    //sonido al agarrar (id del SFXManager). vacio = sin sonido

    [SerializeField] private string dropSfxId;
    //sonido al soltar. vacio = sin sonido

    [SerializeField] private string impactSfxId;
    //sonido al chocar fuerte (golpe contra el piso, paredes, etc.). vacio = sin sonido

    [SerializeField] private float minImpactVelocity = 1.5f;
    //velocidad minima del choque para que suene el impacto

    [SerializeField] private float impactCooldown = 0.15f;
    //tiempo minimo entre golpes para que no spamee al rebotar

    [SerializeField] private bool impactSoundOnce = true;
    //si esta tildado, el golpe suena una sola vez (ideal para la caida). se resetea al volver a agarrarlo

    private float lastImpactTime = -999f;
    private bool impactConsumed;

    public bool CanBeGrabbed => canBeGrabbed;
    public string CannotGrabMessage => cannotGrabMessage;

    // config compartida que llega desde PlayerInteraction al agarrar
    private GrabSettings settings;
    private float currentHoldDistance;
    private PlayerCamera playerCamera;
    private bool cameraFrozenByMe;

    private Rigidbody rb;
    private Collider objectCollider;
    private Collider[] objectColliders;
    private readonly List<Collider> ignoredPlayerColliders = new List<Collider>();
    private Vector3 originalScale;
    private int originalLayer;

    private bool isHeld;
    //me guarda si el objeto esta siendo sostenido

    private Transform currentHandPoint;
    [SerializeField] private PlayerInteraction playerInteraction;
    //referencia al HandPoint actual

    private Vector3 targetLocalPosition;
    private Quaternion targetLocalRotation;
    //offset relativo al HandPoint para sostener el objeto como queremos

    int Slot;

    private void Awake()
    {
        
        rb = GetComponent<Rigidbody>();
        // Lo dejo porque creo que puede servir, si no les gusta borren esta declaracion porque no se usa.
        objectCollider = GetComponent<Collider>();
        objectColliders = GetComponentsInChildren<Collider>();

        originalScale = transform.localScale;
        originalLayer = gameObject.layer;
    }

    private void Start()
    {
        playerInteraction = FindAnyObjectByType<PlayerInteraction>();
    }

    private void Update()
    {
        if (!isHeld)
        {
            return;
        }

        HandleHoldDistance();
        HandleHoldRotation();
    }

    private void FixedUpdate()
    {
        if (!isHeld || currentHandPoint == null)
        {
            return;
        }

        FollowHandPoint();
    }


    public void PickUp(Transform handPoint, int heldLayer, GrabSettings grabSettings)
    {
        //cuando agarraro:
        // - apaga gravedad
        // - mantiene rigidbody dinamico para que siga interactuando
        // - guardo el handPoint como referencia
        // - lo paso a una capa especial para jugar con eso mas adelante
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.detectCollisions = true;

        gameObject.layer = heldLayer;

        currentHandPoint = handPoint;
        isHeld = true;
        settings = grabSettings;
        IgnorePlayerCollisions(true);

        // referencia a la camara del jugador para poder frenarla mientras roto el objeto
        if (playerCamera == null)
        {
            playerCamera = handPoint.GetComponentInParent<PlayerCamera>();
        }

        // distancia inicial: la custom del objeto o la default compartida
        float startDistance = useCustomDistance ? customHoldDistance : settings.defaultDistance;
        currentHoldDistance = Mathf.Clamp(startDistance, settings.minDistance, settings.maxDistance);
        targetLocalPosition = new Vector3(heldLocalPosition.x, heldLocalPosition.y, currentHoldDistance);

        // rotacion inicial: fija configurada, o mantener la orientacion actual del objeto
        if (useFixedGrabRotation)
        {
            targetLocalRotation = Quaternion.Euler(heldLocalRotation);
        }
        else
        {
            targetLocalRotation = Quaternion.Inverse(handPoint.rotation) * rb.rotation;
        }

        //restauro escala original para evitar deformaciones por jerarquia (Tuve un error cambiando las escalas de algunas cosas)
        transform.localScale = originalScale;

        //reseteo velocidades por si venia cayendo o girando raro (porque me hizo concha todo algunas veces)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // al agarrarlo de nuevo permitimos que la proxima caida vuelva a sonar una vez
        impactConsumed = false;

        PlaySfx(pickupSfxId);
    }

    public void Drop()
    {
        //cuando lo suelto:
        // - meto gravedad otra vez
        // - dejo de seguir el HandPoint
        // - el objeto vuelve a su layer original
        isHeld = false;
        IgnorePlayerCollisions(false);
        FreezeCamera(false);
        currentHandPoint = null;

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.detectCollisions = true;

        gameObject.layer = originalLayer;
        transform.localScale = originalScale;
        rb.angularVelocity = Vector3.zero;
        // para la linear revisar si queda bien, si se ve medio mal solo se comenta y listo, no rompe nada, solo esta para reiniciar por si se suelta con inercia rara
        rb.linearVelocity = Vector3.zero;

        PlaySfx(dropSfxId);
    }

    public bool EnablePhysicsFromAmbient(Vector3 impulse)
    {
        // para eventos de ambiente: si no hay collider real, no lo suelto porque se va al vacio
        if (!HasSolidCollider())
        {
            Debug.LogWarning("[GrabbableObject] " + name + " no tiene collider solido. No activo fisica.");
            return false;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        // la caida disparada por el evento puede sonar una vez
        impactConsumed = false;

        if (impulse != Vector3.zero)
        {
            rb.AddForce(impulse, ForceMode.Impulse);
        }

        return true;
    }

    private bool HasSolidCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null && col.enabled && !col.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // mientras el jugador lo sostiene no suena: el roce contra cosas no es un golpe real
        if (isHeld)
        {
            return;
        }

        // si ya sono una vez y esta configurado para sonar solo una vez, no repite (evita el ruido al arrastrar)
        if (impactSoundOnce && impactConsumed)
        {
            return;
        }

        // golpe del objeto contra algo: suena si pega lo bastante fuerte (el objeto es el que se mueve, asi que el sonido vive aca)
        if (string.IsNullOrEmpty(impactSfxId) || SFXManager.Instance == null || collision.contactCount == 0)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactVelocity)
        {
            return;
        }

        if (Time.time - lastImpactTime < impactCooldown)
        {
            return;
        }

        lastImpactTime = Time.time;
        impactConsumed = true;

        ContactPoint contact = collision.GetContact(0);
        SFXManager.Instance.Play3D(impactSfxId, contact.point);
    }

    private void PlaySfx(string id)
    {
        if (string.IsNullOrEmpty(id) || SFXManager.Instance == null)
        {
            return;
        }

        SFXManager.Instance.Play3D(id, transform.position);
    }

    private void IgnorePlayerCollisions(bool ignore)
    {
        if (!ignore)
        {
            foreach (Collider playerCollider in ignoredPlayerColliders)
            {
                SetCollisionWithPlayer(playerCollider, false);
            }

            ignoredPlayerColliders.Clear();
            return;
        }

        PlayerInteraction targetPlayer = playerInteraction;

        if (targetPlayer == null && currentHandPoint != null)
        {
            targetPlayer = currentHandPoint.GetComponentInParent<PlayerInteraction>();
        }

        if (targetPlayer == null)
        {
            return;
        }

        Collider[] playerColliders = targetPlayer.GetComponentsInParent<Collider>();

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null || ignoredPlayerColliders.Contains(playerCollider))
            {
                continue;
            }

            SetCollisionWithPlayer(playerCollider, true);
            ignoredPlayerColliders.Add(playerCollider);
        }
    }

    private void SetCollisionWithPlayer(Collider playerCollider, bool ignore)
    {
        if (objectColliders == null || playerCollider == null)
        {
            return;
        }

        foreach (Collider heldCollider in objectColliders)
        {
            if (heldCollider != null)
            {
                Physics.IgnoreCollision(heldCollider, playerCollider, ignore);
            }
        }
    }

    private void HandleHoldDistance()
    {
        if (Mouse.current == null || settings == null)
        {
            return;
        }

        // la rueda acerca (positivo) o aleja (negativo) el objeto, siempre dentro del rango
        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (scrollY > 0f)
        {
            currentHoldDistance += settings.scrollStep;
        }
        else if (scrollY < 0f)
        {
            currentHoldDistance -= settings.scrollStep;
        }

        currentHoldDistance = Mathf.Clamp(currentHoldDistance, settings.minDistance, settings.maxDistance);
        targetLocalPosition = new Vector3(heldLocalPosition.x, heldLocalPosition.y, currentHoldDistance);
    }

    private void HandleHoldRotation()
    {
        // este objeto no se puede rotar
        if (lockRotation || settings == null)
        {
            FreezeCamera(false);
            return;
        }

        bool rightHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;

        // mientras roto el objeto freno la camara para no marear; al soltar el click la libero
        if (!rightHeld)
        {
            FreezeCamera(false);
            return;
        }

        FreezeCamera(true);

        Vector2 delta = Mouse.current.delta.ReadValue();

        Quaternion yaw = Quaternion.AngleAxis(delta.x * settings.rotateSensitivity, Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(-delta.y * settings.rotateSensitivity, Vector3.right);

        // acumulo el giro sobre la pose que ya tenia (relativo al HandPoint)
        targetLocalRotation = yaw * pitch * targetLocalRotation;
    }

    private void FreezeCamera(bool freeze)
    {
        // solo toco la camara si fui yo quien la freno, asi no piso otros bloqueos
        if (playerCamera == null || cameraFrozenByMe == freeze)
        {
            return;
        }

        playerCamera._MoveCamera(freeze);
        cameraFrozenByMe = freeze;
    }

    private void OnDisable()
    {
        // por si el objeto se manda al inventario o se desactiva mientras lo rotaba
        FreezeCamera(false);
    }

    private void FollowHandPoint()
    {
        //calculo la posicion y rotacion objetivo en espacio global, usando el offset local
        Vector3 targetWorldPosition = currentHandPoint.TransformPoint(targetLocalPosition);
        Quaternion targetWorldRotation = currentHandPoint.rotation * targetLocalRotation;

        //sigo la posicion por velocidad fisica
        Vector3 toTarget = targetWorldPosition - rb.position;
        Vector3 desiredVelocity = toTarget * followPositionSpeed;

        //limito la velocidad para que no meta latigazos raros (Lo hago medio smooth pa que sea mas realista)
        if (desiredVelocity.magnitude > maxFollowVelocity)
        {
            desiredVelocity = desiredVelocity.normalized * maxFollowVelocity;
        }

        rb.linearVelocity = desiredVelocity;

        //seguimiento de rotacion
        Quaternion rotationDelta = targetWorldRotation * Quaternion.Inverse(rb.rotation);
        rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
        {
            angle -= 360f;
        }

        //si el eje sale invalido, no hace nada asi no rompe
        if (float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
        {
            rb.angularVelocity = Vector3.zero;
            return;
        }

        //convierto el angular en velocidad angular
        Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad * followRotationSpeed;
        rb.angularVelocity = angularVelocity;
    }
}