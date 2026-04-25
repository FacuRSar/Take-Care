using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{

    [Header("Components")]

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
    private void Update()
    {


        if (active)
        {
            timer += Time.deltaTime;
            Debug.Log(timer);

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

            playerMovement.CantMove = false; 

            playerCamera.CantMoveCamera = false;
        }

        //if (Input.GetKeyDown(KeyCode.Z)) active = true;

        CameraZoom();

    }

    private void CameraZoom()
    {
        if (canzoomed) targetFov = zoomFov;
        else targetFov = nomalFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * Speed);
    }

    private void FixedCamera()
    {
        Vector3 directionPlayer = transform.position - Player.position;
        directionPlayer.y = 0;

        Vector3 directionCam = transform.position - cam.transform.position;

        Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer);
        Quaternion CamRotation = Quaternion.LookRotation(directionCam);

        float angle = Quaternion.Angle(Player.rotation, PlayerRotation);

        if (angle > minAngle)
        {
            Player.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, Speed * Time.deltaTime);

            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, Speed * Time.deltaTime);

            AngelMetodo();
        }
    }

    private void AngelMetodo()
    {
        canzoomed = true;

        playerMovement.CantMove = true;
        playerCamera.CantMoveCamera = true;
    }

    public bool Active 
    {
        get { return active; }
        private set { active = value; }
    }
}
