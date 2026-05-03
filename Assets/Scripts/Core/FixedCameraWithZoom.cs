using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{
    private static FixedCameraWithZoom currentOwner;
    // hay varios focos en escena. este currentowner evita que uno inactivo toque la camara del otro

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
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");

        if (Player == null && playerObject != null)
        {
            Player = playerObject.transform;
        }

        if (cam == null && cameraObject != null)
        {
            cam = cameraObject.GetComponent<Camera>();
        }

        if (playerObject != null)
        {
            playerCamera = playerObject.GetComponent<PlayerCamera>();
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }
    }

    private void LateUpdate()
    {
        if (active)
        {
            timer += Time.deltaTime;

            LockPlayerControl();
            canzoomed = true;
            currentOwner = this;

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

            if (currentOwner != this)
            {
                return;
            }

            UnlockPlayerControl();
        }

        CameraZoom();

        if (!active && currentOwner == this && cam != null && Mathf.Abs(cam.fieldOfView - nomalFov) < 0.1f)
        {
            currentOwner = null;
        }
    }

    private void CameraZoom()
    {
        if (currentOwner != this || cam == null)
        {
            return;
        }

        if (canzoomed) targetFov = zoomFov;
        else targetFov = nomalFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * Speed);
    }

    private void FixedCamera()
    {
        if (Player == null || cam == null)
        {
            return;
        }

        // el player gira solo en horizontal, la camara si mira al punto completo
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
        // bloqueo movimiento y mouse mientras dura el foco
        if (playerMovement != null)
        {
            playerMovement.CantMove = true;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = true;
        }
    }

    private void UnlockPlayerControl()
    {
        if (playerMovement != null)
        {
            playerMovement.CantMove = false;
        }

        if (playerCamera != null)
        {
            playerCamera.CantMoveCamera = false;
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