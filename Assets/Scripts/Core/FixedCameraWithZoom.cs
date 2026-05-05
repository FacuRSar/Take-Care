using System;
using System.Collections.Generic;
using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{

    [Header("Components")]

    PlayerCamera playerCamera;
    PlayerMovement playerMovement;


    [SerializeField] private float SpeedZoom;

    public event Action SyncRotation;


    [Header("FixedCamera")]

    [SerializeField] private Transform Player;
    [SerializeField] private List<Transform> TargetsMrBeast;
    [SerializeField] private List<float>  TransitionDuration;
    [SerializeField] private List<float> SpeedCamera;

    private int currentTargetIndex = 0;

    [SerializeField] private float minAngle;
    private float targetTimer = 0f;

    bool canzoomed = false;


    [Header("CameraZoom")]

    [SerializeField] private Camera cam;

    [SerializeField] private float zoomFov;
    [SerializeField] private float nomalFov;

    private float targetFov;


    [Header("Timer")]

    private float timer = 0f;

    [SerializeField] private float DurationTotal;

    [SerializeField] public bool active;



    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").transform;
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        playerCamera = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCamera>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

        MatchList();
        DurationTotalScene();
    }
    private void Update()
    {
        if (active)
        {
            timer += Time.deltaTime;
            Debug.Log(timer);

            if (timer < DurationTotal)
            {
                FixedCamera();
            }
            else active = false;
        }
        else 
        {
            ResetCameraSequence();

            canzoomed = false;

            playerMovement.CantMove = false; 

            playerCamera.CantMoveCamera = false;

            SyncRotation?.Invoke(); // sincronizo la rotacion del playerCamera con la del player para evitar que se quede mirando a un lado cuando termina el efecto
            playerCamera.SyncRotation();
        }

        //if (Input.GetKeyDown(KeyCode.Z)) active = true;

        CameraZoom();


    }

    private void CameraZoom()
    {
        if (canzoomed) targetFov = zoomFov;
        else targetFov = nomalFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * SpeedZoom);
    }

    /*private void FixedCamera()
     {
         Vector3 directionPlayer = transform.position - Player.position;
         directionPlayer.y = 0;

         Vector3 directionCam = transform.position - cam.transform.position;

         Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer);
         Quaternion CamRotation = Quaternion.LookRotation(directionCam);

         float angle = Quaternion.Angle(Player.rotation, PlayerRotation);

         if (angle > minAngle)
         {
             Player.transform.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, Speed * Time.deltaTime);

             cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, Speed * Time.deltaTime);

             AngelMetodo();
         }
     } */

    private void FixedCamera()
    {
        

       if (currentTargetIndex >= TargetsMrBeast.Count) return; // si se paso del ultimo target, no hago nada

        Transform target = TargetsMrBeast[currentTargetIndex].transform;

        Vector3 directionPlayer = target.position - Player.position;
        directionPlayer.y = 0;

        Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer); 
        Player.transform.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, SpeedCamera[currentTargetIndex] * Time.deltaTime);

        Vector3 directionCam = target.position - cam.transform.position;

        Quaternion CamRotation = Quaternion.LookRotation(directionCam);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, SpeedCamera[currentTargetIndex] * Time.deltaTime);

        float transitionTime = TransitionDuration[currentTargetIndex];

        targetTimer += Time.deltaTime;

        AngelMetodo();

        if (targetTimer >= transitionTime)
        {
            targetTimer = 0f;

            if (currentTargetIndex < TargetsMrBeast.Count - 1)
            {
                currentTargetIndex++;
                Debug.Log("Cambiando al Target: " + currentTargetIndex);
            }
        }
    } 



    private void AngelMetodo()
    {
        canzoomed = true;

        playerMovement.CantMove = true;
        playerCamera.CantMoveCamera = true;
    }

    private void MatchList()
    {
        while (TransitionDuration.Count < TargetsMrBeast.Count)
        {
            TransitionDuration.Add(1f);
        }
        while (SpeedCamera.Count < TargetsMrBeast.Count)
        {
            SpeedCamera.Add(1f);
        }

    }
    private void ResetCameraSequence()
    {
        currentTargetIndex = 0;
        targetTimer = 0f;
        timer = 0f;
    }

    private void DurationTotalScene()
    {
        DurationTotal = 0f;
    
            foreach (float duration in TransitionDuration)
            {
                DurationTotal += duration;
            }
    }

    public bool Active 
    {
        get { return active; }
        private set { active = value; }
    }
}
