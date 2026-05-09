using System;
using System.Collections.Generic;
using UnityEngine;

public class FixedCameraWithZoom : MonoBehaviour
{

    [Header("Components")]

    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform Player;
    [SerializeField] private Camera cam;


    [Header("FixedCamera")]

    [SerializeField] private List<Transform> TargetsMrBeast;
    [SerializeField] private List<float>  TransitionDuration;
    [SerializeField] private List<float> SpeedCamera;

    [SerializeField] private float minAngle;
    [SerializeField] private float SpeedZoom;
    [SerializeField] public bool active;

    private float targetTimer;
    private int currentTargetIndex;

    bool canzoomed;


    [Header("CameraZoom")]

    [SerializeField] private float zoomFov;
    [SerializeField] private float nomalFov;

    private float targetFov;


    [Header("Timer")]

    [SerializeField] private float DurationTotal;
    private float timer = 0f;


    private void Start()
    {
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
            else if (timer >= DurationTotal)
            {
                active = false; enabled = false;
            }
            else Debug.LogWarning("Error en el Timer");

        }
        else 
        {
            canzoomed = false;

            playerMovement.CantMove = false; 

            playerCamera.CantMoveCamera = false;

            ResetCameraSequence();
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
