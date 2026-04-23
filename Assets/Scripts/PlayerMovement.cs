using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    public bool CantMove;
    void Start()
    {
        //asignar el rigibody y player input al iniciar el juego
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInput();
    }
    private void FixedUpdate()
    {
        if (CantMove) return;
        MovePlayer(); //llamada a funcion movimiento en fixed update asi no choca con la fisica del juego
    }
    //callback del input system, se llama cada vez que el jugador clickea teclas de movimiento
    void OnMovement(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    //funcion que se encarga de mover al jugador con los valores que pasa OnMovement, se llama en fixed update
    void MovePlayer()
    {
        Vector3 direction = transform.forward * moveInput.y + transform.right * moveInput.x;
        direction.Normalize();
        rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
    }

    public Rigidbody Rb
    { get { return rb; }
        private set { rb = value; }     
    }
}
