using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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

    [Header("Cinemachine (opcional)")]
    // Si se asigna, el foco usa esta camara virtual en vez de rotar la camara a mano.
    [SerializeField] private CinemachineCamera focusVirtualCamera;
    // Opcional: se congela durante el foco para que no pelee con la posicion de la camara.
    [SerializeField] private PlayerHeadBob headBob;
    // Posicion world capturada al iniciar el foco: la camara virtual queda clavada aca.
    private Vector3 focusAnchorPosition;


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
    private float currentZoomFov;
    //Angel: Agrego esta variable para que pueda hacer el llamado con un zoom personalizado.


    [Header("Timer")]

    [SerializeField] private float DurationTotal;
    private float timer = 0f;


    private int currentSequenceIndex;


    private void Start()
    {
        currentZoomFov = zoomFov;
        DurationTotalScene();
    }
    private void Update()
    {
        if (isPlayingSequence)
        {
            if (focusVirtualCamera != null && !focusVirtualCamera.enabled)
            {
                // Clavamos el foco EXACTAMENTE en la posicion/rotacion que tiene la camara real ahora.
                focusAnchorPosition = cam.transform.position;
                focusVirtualCamera.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
                focusVirtualCamera.enabled = true;

                if (headBob != null)
                    headBob.SetFocusFreeze(true);
            }

            timer += Time.deltaTime;

            if (timer < DurationTotal)
            {
                FixedCamera();
            }
            else if (timer >= DurationTotal)
            {
                isPlayingSequence = false;
            }
            else return;//Debug.LogWarning("Error en el Timer");

        }
        else
        {
            if (focusVirtualCamera != null && focusVirtualCamera.enabled)
            {
                focusVirtualCamera.enabled = false;

                if (headBob != null)
                    headBob.SetFocusFreeze(false);
            }

            canzoomed = false;

            playerMovement.CantMove(false);
            playerCamera._MoveCamera(false);

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
        if (canzoomed) targetFov = currentZoomFov;
        else targetFov = nomalFov;
        //Angel: Cambio para usar currentZoomFov

        if (focusVirtualCamera != null && focusVirtualCamera.enabled)
        {
            LensSettings lens = focusVirtualCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, Time.deltaTime * SpeedZoom);
            focusVirtualCamera.Lens = lens;
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * SpeedZoom);
        }
    }
    private void FixedCamera()
    {
        if (sequences == null || sequences.Length == 0 || currentSequenceIndex < 0 || currentSequenceIndex >= sequences.Length)
            return;

        CameraSequence currentSequence = sequences[currentSequenceIndex];
        if (currentSequence == null || currentSequence.objectives == null || currentSequence.objectives.Count == 0)
            return;

        if (currentTargetIndex < 0 || currentTargetIndex >= currentSequence.objectives.Count)
        {
            isPlayingSequence = false;
            return;
        }

        ObjectWithFocus step = currentSequence.objectives[currentTargetIndex];
        if (step == null || step.TargetsMrBeast == null)
            return;

        Transform target = step.TargetsMrBeast;
        float transitionTime = step.TransitionDuration;
        float speedCamera = step.SpeedCamera;

        Vector3 directionPlayer = target.position - Player.position;
        directionPlayer.y = 0;

        Quaternion PlayerRotation = Quaternion.LookRotation(directionPlayer);
        Player.transform.rotation = Quaternion.Lerp(Player.rotation, PlayerRotation, speedCamera * Time.deltaTime);

        if (focusVirtualCamera != null)
        {
            // Mantenemos la camara virtual clavada en la posicion capturada (no se mueve, no orbita).
            focusVirtualCamera.transform.position = focusAnchorPosition;

            // Solo rotamos suave hacia el objetivo desde esa posicion fija.
            Vector3 directionCam = target.position - focusAnchorPosition;
            if (directionCam.sqrMagnitude > 0.0001f)
            {
                Quaternion CamRotation = Quaternion.LookRotation(directionCam);
                focusVirtualCamera.transform.rotation = Quaternion.Lerp(focusVirtualCamera.transform.rotation, CamRotation, speedCamera * Time.deltaTime);
            }
        }
        else
        {
            Vector3 directionCam = target.position - cam.transform.position;

            Quaternion CamRotation = Quaternion.LookRotation(directionCam);
            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, CamRotation, speedCamera * Time.deltaTime);
        }



        targetTimer += Time.deltaTime;

        AngelMetodo();

        if (targetTimer >= transitionTime)
        {
            targetTimer = 0f;

            if (currentTargetIndex < currentSequence.objectives.Count - 1)
            {
                currentTargetIndex++;
                //Debug.Log("Cambiando al Target: " + currentTargetIndex);
            }
        }
    }




    private void AngelMetodo()
    {
        canzoomed = true;

        playerMovement.CantMove(true);
        playerCamera._MoveCamera(true);
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
        if (sequences == null || sequences.Length == 0 || currentSequenceIndex < 0 || currentSequenceIndex >= sequences.Length)
            return;

        CameraSequence currentSequence = sequences[currentSequenceIndex];
        if (currentSequence == null || currentSequence.objectives == null)
            return;

        foreach (ObjectWithFocus obj in currentSequence.objectives)
        {
            if (obj != null)
            {
                DurationTotal += obj.TransitionDuration;
            }
        }
    }

    // Activa la secuencia ya armada en pools (sin pasar foco ni tiempos desde afuera).
    private void PlayFocusSequence()
    {
        enabled = true;
        ResetCameraSequence();
        DurationTotalScene();
        isPlayingSequence = true;
    }

    // Duración total de la secuencia (para coroutines que esperan al foco).
    private float _GetTotalSequenceDuration()
    {
        DurationTotalScene();
        return DurationTotal;
    }

    public float GetTotalSequenceDuration()
    {
        return _GetTotalSequenceDuration();
    }

    public bool IsPlayingSequence()
    {
        return isPlayingSequence;
    }

    private void _PlaySequence(int sequenceIndex)
    {
        currentZoomFov = zoomFov;

        currentSequenceIndex = sequenceIndex;

        currentTargetIndex = 0;
        targetTimer = 0f;
        timer = 0f;

        DurationTotalScene();

        isPlayingSequence = true;
    }
    public void PlaySequence(int sequenceIndex)
    {
        _PlaySequence(sequenceIndex);
    }

    private void _PlaySequence(int sequenceIndex, float customZoomFov)
    {
        currentZoomFov = customZoomFov > 0f ? customZoomFov : nomalFov;

        currentSequenceIndex = sequenceIndex;

        currentTargetIndex = 0;
        targetTimer = 0f;
        timer = 0f;

        DurationTotalScene();

        isPlayingSequence = true;
    }

    public void PlaySequence(int sequenceIndex, float customZoomFov)
    {
        _PlaySequence(sequenceIndex, customZoomFov);
    }


}
