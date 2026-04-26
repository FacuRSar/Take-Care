using System.Collections;
using UnityEngine;

public class CameraFallEffect : MonoBehaviour
{

    // Lo saque porque se veia medio mal. Despues lo mejoro.
    [Header("Objetivo")]
    [SerializeField] private Transform cameraTransform;

    [Header("Caida")]
    [SerializeField] private Vector3 fallLocalEuler = new Vector3(20f, 0f, 8f);
    [SerializeField] private float duration = 0.6f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void Play()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("CameraFallEffect: no hay cameraTransform asignada.");
            return;
        }

        // si ya estaba cayendo, reinicio el efecto. una sola caida prolija, sin marear
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        Quaternion startRotation = cameraTransform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(fallLocalEuler);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            cameraTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        currentRoutine = null;
    }
}
