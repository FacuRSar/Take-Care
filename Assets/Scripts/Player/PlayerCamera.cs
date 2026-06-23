using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camara")]
    public float mouseSensitivity = 0.4f;// sensibilidad del mouse para rotar la camara
    public Transform cam;// referencia a la camara del jugador

    [Header("Proteccion de input")]
    [SerializeField] private float maxLookDelta = 120f;
    // Ignora picos absurdos (alt-tab, foco perdido). Valores normales solo se recortan suavemente.
    [SerializeField] private float spikeLookDelta = 400f;

    private float xRotation = 0f;//rotacion en el eje x para limitar la rotacion de la camara
    private float yRotation = 0f;//rotacion acumulada del jugador en eje Y
    private Rigidbody rb;
    private CinemachineBrain cinemachineBrain;

    // yaw real acumulado. El movimiento lo usa para no depender de transform.forward,
    // que con el Rigidbody interpolado queda desfasado al leerse en FixedUpdate.
    public float Yaw => yRotation;

    [SerializeField] private bool _CantMoveCamera;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (cam != null)
        {
            cinemachineBrain = cam.GetComponent<CinemachineBrain>();
        }

        SetCinemachineBrainEnabled(false);

        Cursor.lockState = CursorLockMode.Locked; //bloquear el cursor al iniciar el juego en el centro de la pantalla
        Cursor.visible = false; //ocultar el cursor

        // Inicializo el yaw con la rotacion actual del player por si arranca girado
        yRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        if (!_CantMoveCamera)
        {
            ReadMouseInput();
        }
    }

    private void LateUpdate()
    {
        if (!_CantMoveCamera)
        {
            ApplyCameraRotation();
        }
    }

    public void SetCinemachineBrainEnabled(bool enabled)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = enabled;
        }
    }

    public void _MoveCamera(bool value)
    {
        _CantMoveCamera = value;
    }

    void ReadMouseInput()
    {
        //leo el delta real del mouse directamente desde el Input System nuevo
        Vector2 mouseDelta = Vector2.zero;

        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }

        mouseDelta = FilterMouseDelta(mouseDelta);

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        //rotacion vertical de la camara
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //rotacion horizontal del player
        yRotation += mouseX;
    }

    private void ApplyCameraRotation()
    {
        if (cam != null)
        {
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        Quaternion yawRotation = Quaternion.Euler(0f, yRotation, 0f);
        if (rb != null)
        {
            // rb.rotation en LateUpdate: evita que la interpolacion del Rigidbody
            // pise el yaw que seteamos en Update (comun en builds).
            rb.rotation = yawRotation;
        }
        else
        {
            transform.rotation = yawRotation;
        }
    }

    private Vector2 FilterMouseDelta(Vector2 mouseDelta)
    {
        if (Mathf.Abs(mouseDelta.x) > spikeLookDelta)
        {
            mouseDelta.x = 0f;
        }
        else
        {
            mouseDelta.x = Mathf.Clamp(mouseDelta.x, -maxLookDelta, maxLookDelta);
        }

        if (Mathf.Abs(mouseDelta.y) > spikeLookDelta)
        {
            mouseDelta.y = 0f;
        }
        else
        {
            mouseDelta.y = Mathf.Clamp(mouseDelta.y, -maxLookDelta, maxLookDelta);
        }

        return mouseDelta;
    }

    private void _SyncRotation()
    {
        yRotation = transform.eulerAngles.y;

        if (cam == null)
        {
            return;
        }

        xRotation = cam.localEulerAngles.x;
        if (xRotation > 180f) xRotation -= 360f; // para convertir la rotacion local de la camara a un rango de -180 a 180
    }
    public void SyncRotation()
    {
        _SyncRotation();
    }
}