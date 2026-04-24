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
    [SerializeField] private float crouchCameraY = 1.0f;
    [SerializeField] private float crouchLerpSpeed = 10f;

    private Rigidbody rb;
    private Vector2 moveInput;
    // private PlayerInput playerInput; // Saco esto porque no es necesario, unity ya hace eso automatico. pa emprolijar nomas
    private bool isSprinting;
    private bool isCrouching;

    private float targetHeight;
    private float targetCameraY;

    public bool CantMove;

    void Start()
    {
        //asignar el rigibody y player input al iniciar el juego
        rb = GetComponent<Rigidbody>();

        if (capsule == null) // Me fijo que el componente no este asignado antes para asignarlo en el start
            capsule = GetComponent<CapsuleCollider>();

        // defino los target de ambos ejes para que no arranque feo
        targetHeight = standingHeight;
        targetCameraY = standingCameraY;
        // playerInput = new PlayerInput(); // Lo mismo, no se necesita, por eso lo saco. Si queremos agregar algun boton por codigo tambien se puede, pero no necesita playerInput
    }

    private void Update()
    {
        if (!CantMove)
        {
            HandleSprintInput(); // mira si Shift esta apretado
            HandleCrouchInput(); // mira si Ctrl esta apretado
            HandleCrouchVisuals(); // llamo a la funcion para ver si se esta agachado (para actualizar la vista y todo)      
        }
    }

    private void FixedUpdate()
    {
        if (!CantMove) MovePlayer(); //llamada a funcion movimiento en fixed update asi no choca con la fisica del juego
    }
    //callback del input system, se llama cada vez que el jugador clickea teclas de movimiento
    public void OnMovement(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    //funcion que se encarga de mover al jugador con los valores que pasa OnMovement, se llama en fixed update
    private void MovePlayer()
    {
        Vector3 direction = transform.forward * moveInput.y + transform.right * moveInput.x;
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

        rb.linearVelocity = new Vector3(
            direction.x * currentSpeed,
            rb.linearVelocity.y,
            direction.z * currentSpeed
        );
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
        targetCameraY = isCrouching ? crouchCameraY : standingCameraY;
    }

    private void HandleCrouchVisuals()
    {
        float newHeight = Mathf.Lerp(capsule.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
        capsule.height = newHeight;
        capsule.center = new Vector3(0f, newHeight / 2f, 0f);

        Vector3 camPos = playerCamera.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchLerpSpeed * Time.deltaTime);
        playerCamera.localPosition = camPos;
    }
    private void markObject(bool testbool, GameObject testObject)
    {
        if(testObject != null && !testbool)
        {
            
        }
    }
}