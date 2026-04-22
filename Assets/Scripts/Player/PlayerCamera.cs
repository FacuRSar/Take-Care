using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camara")]
    public float mouseSensitivity = 0.4f;// sensibilidad del mouse para rotar la camara
    public Transform cam;// referencia a la camara del jugador

    [Header("Proteccion de input")]
    [SerializeField] private float maxLookDelta = 30f;
    // limite maximo permitido para delta del mouse por frame porque soy un boludo y rompia algunas cosas, ademas queda mas smooth
    // con esto si el delta supera el valor, se considera un pico raro y se ignora

    private float xRotation = 0f;//rotacion en el eje x para limitar la rotacion de la camara
    private float yRotation = 0f;//rotacion acumulada del jugador en eje Y

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; //bloquear el cursor al iniciar el juego en el centro de la pantalla
        Cursor.visible = false; //ocultar el cursor

        // Inicializo el yaw con la rotacion actual del player por si arranca girado
        yRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleMouseCam(); //llamada a la funcion que maneja la rotacion de la camara con el mouse
    }

    void HandleMouseCam()
    {
        //leo el delta real del mouse directamente desde el Input System nuevo
        Vector2 mouseDelta = Vector2.zero;

        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }

        //si entra un pico demasiado grande, lo ignoro completamente
        if (Mathf.Abs(mouseDelta.x) > maxLookDelta || Mathf.Abs(mouseDelta.y) > maxLookDelta)
        {
            mouseDelta = Vector2.zero;
        }

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        //rotacion vertical de la camar
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //rotacion horizontal del player
        yRotation += mouseX;

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}