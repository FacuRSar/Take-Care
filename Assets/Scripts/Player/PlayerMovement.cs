using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Agacharse")]
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float standingCameraY = 1.6f;
    [SerializeField] private float crouchLerpSpeed = 10f;
    // Cuanto encoge el collider al agacharse (0.6 = queda al 60% de su altura parada).
    [SerializeField] private float crouchHeightFactor = 0.6f;
    // Cuanto baja la camara al agacharse, en unidades locales.
    [SerializeField] private float cameraCrouchDrop = 0.45f;

    private Rigidbody rb;
    private PlayerCamera cameraController;
    private Vector2 moveInput;
    // private PlayerInput playerInput; // Saco esto porque no es necesario, unity ya hace eso automatico. pa emprolijar nomas
    private bool isCrouching;
    private bool isSprinting;

    public float CurrentSpeed { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;

    private float targetHeight;
    private float capsuleBaseY;
    private float cameraStandY;
    private float cameraCrouchY;
    private float cameraTargetY;
    private float cameraBaseLocalY;

    // Altura local de la camara que maneja el agacharse. El HeadBob la usa como base
    // para sumarle el balanceo, asi un solo script escribe la posicion de la camara.
    public float CameraBaseLocalY => cameraBaseLocalY;

    private bool _CantMove;

    private float movementFeelMultiplier = 1f;

    void Start()
    {
        //asignar el rigibody y player input al iniciar el juego
        rb = GetComponent<Rigidbody>();
        cameraController = GetComponent<PlayerCamera>();

        if (cameraController == null)
            cameraController = GetComponentInParent<PlayerCamera>();

        if (capsule == null) // Me fijo que el componente no este asignado antes para asignarlo en el start
            capsule = GetComponent<CapsuleCollider>();

        // Parto de la altura real del collider en la escena. El agacharse lo encoge desde aca,
        // nunca lo agranda (eso trababa al jugador contra el techo ni lo hunde).
        standingHeight = capsule.height;
        capsuleBaseY = capsule.center.y - capsule.height * 0.5f;
        crouchHeight = Mathf.Max(0.1f, standingHeight * crouchHeightFactor);

        // La camara parada queda donde esta en la escena; agachado baja un poco.
        cameraStandY = playerCamera != null ? playerCamera.localPosition.y : standingCameraY;
        cameraCrouchY = cameraStandY - cameraCrouchDrop;
        cameraBaseLocalY = cameraStandY;
        cameraTargetY = cameraStandY;

        targetHeight = standingHeight;
        // playerInput = new PlayerInput(); // Lo mismo, no se necesita, por eso lo saco. Si queremos agregar algun boton por codigo tambien se puede, pero no necesita playerInput
    }

    private void Update()
    {
        if (!_CantMove)
        {
            HandleSprintInput(); // mira si Shift esta apretado
            HandleCrouchInput(); // mira si Ctrl esta apretado
            HandleCrouchVisuals(); // llamo a la funcion para ver si se esta agachado (para actualizar la vista y todo)      
        }
    }

    private void FixedUpdate()
    {
        if (!_CantMove) MovePlayer(); //llamada a funcion movimiento en fixed update asi no choca con la fisica del juego
    }

    public void CantMove(bool value)
    {
        _CantMove = value;
    }

    //callback del input system, se llama cada vez que el jugador clickea teclas de movimiento
    public void OnMovement(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    //funcion que se encarga de mover al jugador con los valores que pasa OnMovement, se llama en fixed update
    private void MovePlayer()
    {
        // uso el yaw real de la camara en vez de transform.forward: el Rigidbody interpolado
        // desfasa la rotacion del transform al leerla en FixedUpdate y el jugador seguia
        // caminando hacia donde miraba antes de girar la camara.
        float yaw = cameraController != null ? cameraController.Yaw : transform.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        Vector3 direction = forward * moveInput.y + right * moveInput.x;
        direction.Normalize();

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }

        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }

        float feel = Mathf.Clamp(movementFeelMultiplier, 0.25f, 3f);
        CurrentSpeed = currentSpeed * feel;
        IsMoving = direction.magnitude > 0.1f;

        rb.linearVelocity = new Vector3(
            direction.x * currentSpeed * feel,
            rb.linearVelocity.y,
            direction.z * currentSpeed * feel
        );
    }

    public void SetMovementFeelMultiplier(float multiplier)
    {
        movementFeelMultiplier = Mathf.Clamp(multiplier, 0.25f, 3f);
    }

    // En vez de usar callback para sprint, lo leo directamente por teclado con Input System
    private void HandleSprintInput()
    {
        if (Keyboard.current != null)
        {
            isSprinting = Keyboard.current.leftShiftKey.isPressed;
        }
    }

    // Igual que sprint, para agacharse leo el estado real del Left Ctrl en cada frame
    private void HandleCrouchInput()
    {
        if (Keyboard.current != null)
        {
            isCrouching = Keyboard.current.leftCtrlKey.isPressed;
        }

        targetHeight = isCrouching ? crouchHeight : standingHeight;
        cameraTargetY = isCrouching ? cameraCrouchY : cameraStandY;
    }

    private void HandleCrouchVisuals()
    {
        float newHeight = Mathf.Lerp(capsule.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
        capsule.height = newHeight;

        // Mantiene la base de la capsula en el mismo lugar para que el jugador no se hunda.
        capsule.center = new Vector3(capsule.center.x, capsuleBaseY + newHeight * 0.5f, capsule.center.z);

        // Solo actualizo la base de la camara. Quien la escribe es el HeadBob (CameraBaseLocalY).
        cameraBaseLocalY = Mathf.Lerp(cameraBaseLocalY, cameraTargetY, crouchLerpSpeed * Time.deltaTime);
    }

    /////////////////////////////////////////////////////// Codigo muerto?
    private void markObject(bool testbool, GameObject testObject)
    {
        if(testObject != null && !testbool)
        {
            
        }
    }
    /////////////////////////////////////////////////////// 

}