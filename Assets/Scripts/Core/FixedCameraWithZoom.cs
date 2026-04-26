using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{
    private static FixedCameraWithZoom currentOwner;
    // hay varios focos en escena. este currentowner evita que uno inactivo desbloquee la camara del otro,
    // lo agregue porque estaba rompiendome y la camara se movia como wachin mandibuleando

    [Header("Componentes")]

    PlayerCamera playerCamera;

    PlayerMovement playerMovement;

    [SerializeField] private float Speed;


    [Header("FixedCamera")]

    [SerializeField] private Transform Player;

    [SerializeField] private float minAngle;

    bool canzoomed = false;


    [Header("CameraZoom")]

    [SerializeField] private Camera cam;

    [SerializeField] private float zoomFov;

    [SerializeField] private float nomalFov;

    private float targetFov;


    [Header("Timer")]

    private float timer = 0f;

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
        if (active)
        {
            timer += Time.deltaTime;
            // Debug.Log("Camara fija: timer " + timer);

            LockPlayerControl();
            canzoomed = true;
            currentOwner = this;

            if (timer < duration)
            {
                FixedCamera();
            }
            else active = false;
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
        if (newDuration > 0f)
        {
            duration = newDuration;
        }

        timer = 0f;
        currentOwner = this;
        active = true;
    }
}
