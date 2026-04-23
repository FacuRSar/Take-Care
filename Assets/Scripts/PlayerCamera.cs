using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerCamera : MonoBehaviour
{
    public float mouseSensitivity = 200f;//sensibilidad del mouse para rotar la camara
    public Transform cam;//referencia a la camara del jugador
    private float xRotation = 0f;//rotacion en el eje x para limitar la rotacion de la camara
    private Vector2 mouseInput;//almacena el input del mouse para rotar la camara

    public bool CantMoveCamera;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; //bloquear el cursor al iniciar el juego en el centro de la pantalla
        Cursor.visible = false; //ocultar el cursor
    }

    void Update()
    {
        if (CantMoveCamera) return;
        HandleMouseCam(); //llamada a la funcion que maneja la rotacion de la camara con el mouse
    }

    public void OnLook(InputValue value)
    {
        mouseInput = value.Get<Vector2>(); //obtener el input del mouse
    }
    void HandleMouseCam()
    {
        float mouseX = mouseInput.x * mouseSensitivity * Time.deltaTime; //calcular la rotacion en el eje x y y multiplicar por la sensibilidad y el delta time para que sea suave
        float mouseY = mouseInput.y * mouseSensitivity * Time.deltaTime; //calcular la rotacion en el eje y y multiplicar por la sensibilidad y el delta time para que sea suave
        xRotation -= mouseY; //restar la rotacion en el eje y para que la camara gire hacia arriba y hacia abajo
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); //limitar la rotacion en el eje y para que no gire completamente
        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f); //rotar la camara en el eje x
        transform.Rotate(Vector3.up * mouseX); //rotar el jugador en el eje y
    }
}
