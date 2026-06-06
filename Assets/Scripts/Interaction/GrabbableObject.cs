using System.Collections.Generic;
using UnityEngine;

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

    private void FixedUpdate()
    {
        if (!isHeld || currentHandPoint == null)
        {
            return;
        }

        FollowHandPoint();
    }


    public void PickUp(Transform handPoint, int heldLayer)
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
        IgnorePlayerCollisions(true);

        // guardo la pose local
        targetLocalPosition = heldLocalPosition;
        targetLocalRotation = Quaternion.Euler(heldLocalRotation);

        //restauro escala original para evitar deformaciones por jerarquia (Tuve un error cambiando las escalas de algunas cosas)
        transform.localScale = originalScale;

        //reseteo velocidades por si venia cayendo o girando raro (porque me hizo concha todo algunas veces)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Drop()
    {
        //cuando lo suelto:
        // - meto gravedad otra vez
        // - dejo de seguir el HandPoint
        // - el objeto vuelve a su layer original
        isHeld = false;
        IgnorePlayerCollisions(false);
        currentHandPoint = null;

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.detectCollisions = true;

        gameObject.layer = originalLayer;
        transform.localScale = originalScale;
        rb.angularVelocity = Vector3.zero;
        // para la linear revisar si queda bien, si se ve medio mal solo se comenta y listo, no rompe nada, solo esta para reiniciar por si se suelta con inercia rara
        rb.linearVelocity = Vector3.zero;
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