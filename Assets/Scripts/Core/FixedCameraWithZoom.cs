using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{
    private static FixedCameraWithZoom currentOwner;
    private static int focusVersion;
    // hay varios focos en escena. este currentowner evita que uno inactivo desbloquee la camara del otro,
    // lo agregue porque estaba rompiendome y la camara se movia como wachin mandibuleando

    [Header("Componentes")]

    PlayerCamera playerCamera;

    PlayerMovement playerMovement;

    [SerializeField] private float Speed;


    [Header("FixedCamera")]

    [SerializeField] private Transform Player;

    [SerializeField] private float minAngle;

    [SerializeField] private bool restoreViewOnEnd = false;
    // si esta activo, cuando termina el focus vuelve a mirar como estaba antes
    // por defecto queda apagado porque a veces queremos que el susto te deje mirando para alla

    bool canzoomed = false;

    private Quaternion savedPlayerRotation;
    private Quaternion savedCameraRotation;
    // guardo la rotacion previa por si este focus tiene que devolver la mirada al terminar

    [Header("CameraZoom")]

    [SerializeField] private Camera cam;

    [SerializeField] private float zoomFov;

    [SerializeField] private float nomalFov;

    private float targetFov;


    [Header("Timer")]

    private float timer = 0f;
    private int myFocusVersion;
    private bool wasCancelled;

    [SerializeField] private float duration;

    [SerializeField] public bool active;



    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").transform;
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        playerCamera = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCamera>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (active && (currentOwner != this || myFocusVersion != focusVersion))
        {
            CancelFocusWithoutUnlock();
        }

        if (active)
        {
            timer += Time.deltaTime;
            // Debug.Log("Camara fija: timer " + timer);

            LockPlayerControl();

            if (timer < duration)
            {
                FixedCamera();
            }
            else
            {
                active = false;
            }
        }
        else
        {
            timer = 0f;

            canzoomed = false;

            if (currentOwner == this && playerMovement != null)
            {
                playerMovement.CantMove = false;
            }

            if (currentOwner == this && playerCamera != null)
            {
                playerCamera.CantMoveCamera = false;
            }

            if (currentOwner == this)
            {
                if (!wasCancelled)
                {
                    RestoreViewIfNeeded();
                }

                currentOwner = null;
            }
        }

        //if (input.getkeydown(keycode.z)) active = true;

        CameraZoom();
    }

    private void CameraZoom()
    {
        if (canzoomed) targetFov = zoomFov;
        else targetFov = nomalFov;

        if (cam == null)
        {
            return;
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * Speed);
    }

    private void FixedCamera()
    {
        if (Player == null || cam == null)
        {
            return;
        }

        // el player gira solo en horizontal, la camara si mira al punto completo
        // asi no termina el cuerpo mirando al techo como pose rarasa
        Vector3 directionPlayer = transform.position - Player.position;
        directionPlayer.y = 0;

        Vector3 directionCam = transform.position - cam.transform.position;

        Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer);
        Quaternion CamRotation = Quaternion.LookRotation(directionCam);

        float angle = Quaternion.Angle(Player.rotation, PlayerRotation);

        if (angle > minAngle)
        {
            Player.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, Speed * Time.deltaTime);
        }

        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, Speed * Time.deltaTime);
    }

    private void LockPlayerControl()
    {
        // bloqueo movimiento y mouse mientras dura el foco. se hace cada frame para ganarle al input
        if (playerMovement != null)
        {
            playerMovement.CantMove = true;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = true;
        }
    }

    public bool Active
    {
        get { return active; }
        private set { active = value; }
    }

    public void ActivateForDuration(float newDuration)
    {
        if (currentOwner != null && currentOwner != this)
        {
            currentOwner.CancelFocusWithoutUnlock();
            // si otro focus estaba vivo, lo corto sin desbloquear input
            // el nuevo focus se encarga de tomar control
        }

        if (newDuration > 0f)
        {
            duration = newDuration;
        }

        SaveCurrentView();
        // guardo la mirada justo antes de mover la camara

        focusVersion++;
        myFocusVersion = focusVersion;
        wasCancelled = false;

        timer = 0f;
        currentOwner = this;
        canzoomed = true;
        active = true;
    }

    private void CancelFocusWithoutUnlock()
    {
        active = false;
        timer = 0f;
        canzoomed = false;
        wasCancelled = true;
        // corto este focus pero no libero movimiento aca
        // eso lo maneja el focus que queda como dueno real
    }

    private void SaveCurrentView()
    {
        // esto solo importa si restoreviewonend esta activo
        // igual guardarlo no cuesta nada y evita ramificar de mas
        if (Player != null)
        {
            savedPlayerRotation = Player.rotation;
        }

        if (cam != null)
        {
            savedCameraRotation = cam.transform.rotation;
        }
    }

    private void RestoreViewIfNeeded()
    {
        if (!restoreViewOnEnd)
        {
            return;
        }

        // vuelvo la mirada original solo en focos que lo pidan
        // si no, dejamos el encuadre dramatico donde quedo
        if (Player != null)
        {
            Player.rotation = savedPlayerRotation;
        }

        if (cam != null)
        {
            cam.transform.rotation = savedCameraRotation;
        }
    }
}
