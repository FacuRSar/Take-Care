using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


[Serializable]
public class ObjectWithFocus
{
    [Tooltip("Objetivo de la cámara")]
    public Transform TargetsMrBeast;
    public float TransitionDuration;
    public float SpeedCamera;
}

[Serializable]
public class CameraSequence
{
    public List<ObjectWithFocus> objectives;
}

public class FixedCameraWithZoom : MonoBehaviour
{

    [Header("Components")]

    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform Player;
    [SerializeField] private Camera cam;


    [Header("Objetivos")]
    [SerializeField] private CameraSequence[] sequences;

    [SerializeField] private float minAngle;
    [SerializeField] private float SpeedZoom;
    [SerializeField] public bool isPlayingSequence;

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


    private int currentSequenceIndex;


    private void Start()
    {
        DurationTotalScene();
    }
    private void Update()
    {
        if (isPlayingSequence)
        {
            timer += Time.deltaTime;
            Debug.Log(timer);
            Debug.Log(DurationTotal);

            if (timer < DurationTotal)
            {
                FixedCamera();
            }
            else if (timer >= DurationTotal)
            {
                isPlayingSequence = false;
            }
            else Debug.LogWarning("Error en el Timer");

        }
        else
        {
            canzoomed = false;

            playerMovement.CantMove = false;

            playerCamera.CantMoveCamera = false;

            playerCamera.SyncRotation();

            ResetCameraSequence();
        }

        CameraZoom();

        if (Input.GetKey(KeyCode.Z))
        {
            PlaySequence(0);
        }
        if (Input.GetKey(KeyCode.F))
        {
            PlaySequence(1);
        }
    }

    private void CameraZoom()
    {
        if (canzoomed) targetFov = zoomFov;
        else targetFov = nomalFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * SpeedZoom);
    }
    private void FixedCamera()
    {
        if (sequences == null || sequences.Length == 0 || currentSequenceIndex >= sequences.Length)
            return;

        ObjectWithFocus step = sequences[currentSequenceIndex].objectives[currentTargetIndex];
        if (step == null || step.TargetsMrBeast == null)
            return;

        Transform target = step.TargetsMrBeast;
        float transitionTime = step.TransitionDuration;
        float speedCamera = step.SpeedCamera;

        Vector3 directionPlayer = target.position - Player.position;
        directionPlayer.y = 0;

        Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer);
        Player.transform.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, speedCamera * Time.deltaTime);

        Vector3 directionCam = target.position - cam.transform.position;

        Quaternion CamRotation = Quaternion.LookRotation(directionCam);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, speedCamera * Time.deltaTime);



        targetTimer += Time.deltaTime;

        AngelMetodo();

        if (targetTimer >= transitionTime)
        {
            targetTimer = 0f;

            if (currentTargetIndex < sequences.Length - 1)
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

    private void ResetCameraSequence()
    {
        currentTargetIndex = 0;
        targetTimer = 0f;
        timer = 0f;
    }

    private void DurationTotalScene()
    {
        DurationTotal = 0f;
        if (sequences == null || sequences.Length == 0)
            return;

            foreach (ObjectWithFocus obj in sequences[currentSequenceIndex].objectives)
            {
                DurationTotal += obj.TransitionDuration;
            }
    }

    // Activa la secuencia ya armada en pools (sin pasar foco ni tiempos desde afuera).
    public void PlayFocusSequence()
    {
        enabled = true;
        ResetCameraSequence();
        DurationTotalScene();
        isPlayingSequence = true;
    }

    // Duración total de la secuencia (para coroutines que esperan al foco).
    public float GetTotalSequenceDuration()
    {
        DurationTotalScene();
        return DurationTotal;
    }

    public void PlaySequence(int sequenceIndex)
    {
        currentSequenceIndex = sequenceIndex;

        currentTargetIndex = 0;
        targetTimer = 0f;
        timer = 0f;

        DurationTotalScene();

        isPlayingSequence = true;
    }

}
