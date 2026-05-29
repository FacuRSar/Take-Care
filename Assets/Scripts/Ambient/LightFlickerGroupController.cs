using System.Collections;
using UnityEngine;

public class LightFlickerGroupController : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private bool collectChildLightsOnAwake = true;
    [SerializeField] private bool flickerOnStart = true;

    [Header("Pulso inicial")]
    [SerializeField] private bool useStartupPulse = false;
    [SerializeField] private float startupPulseIntensity = 2f;
    [SerializeField] private float startupPulseDuration = 0.15f;

    [Header("Variacion organica")]
    [SerializeField] private bool useOrganicVariation = false;
    [SerializeField] private Vector2 organicIntensityRange = new Vector2(0.8f, 1.5f);
    [SerializeField] private Vector2 organicChangeTimeRange = new Vector2(0.05f, 0.18f);
    [SerializeField] private float organicSmoothSpeed = 12f;

    [Header("Pulsos random")]
    [SerializeField] private bool useRandomPulses = true;
    [SerializeField] private Vector2 timeBetweenPulses = new Vector2(1f, 3f);
    [SerializeField] private Vector2 pulseIntensityRange = new Vector2(0.6f, 1.4f);
    [SerializeField] private Vector2 pulseDurationRange = new Vector2(0.05f, 0.15f);

    [Header("Configuraciones adicionales")]
    [SerializeField] private bool smoothReturnToBase = true;
    [SerializeField] private float returnSpeed = 8f;

    private float[] baseIntensities;
    private float organicTargetMultiplier = 1f;
    private Coroutine startupRoutine;
    private Coroutine pulseRoutine;
    private Coroutine organicRoutine;
    private bool isPulsing;

    private void Awake()
    {
        if (collectChildLightsOnAwake && (lights == null || lights.Length == 0))
        {
            lights = GetComponentsInChildren<Light>(true);
        }

        CacheBaseIntensities();
    }

    private void OnEnable()
    {
        if (flickerOnStart)
        {
            StartFlicker();
        }
    }

    private void OnDisable()
    {
        StopFlicker();
    }

    public void StartFlicker()
    {
        StopFlicker();
        CacheBaseIntensities();
        organicTargetMultiplier = 1f;
        isPulsing = false;

        if (useOrganicVariation)
        {
            organicRoutine = StartCoroutine(OrganicVariationLoop());
        }

        if (useStartupPulse)
        {
            startupRoutine = StartCoroutine(DoPulse(startupPulseIntensity, startupPulseDuration));
        }

        if (useRandomPulses)
        {
            pulseRoutine = StartCoroutine(RandomPulseLoop());
        }
    }

    public void StopFlicker()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (startupRoutine != null)
        {
            StopCoroutine(startupRoutine);
            startupRoutine = null;
        }

        if (organicRoutine != null)
        {
            StopCoroutine(organicRoutine);
            organicRoutine = null;
        }

        isPulsing = false;
        RestoreBaseIntensities();
    }

    private IEnumerator OrganicVariationLoop()
    {
        while (true)
        {
            organicTargetMultiplier = Random.Range(organicIntensityRange.x, organicIntensityRange.y);

            float wait = Random.Range(organicChangeTimeRange.x, organicChangeTimeRange.y);
            float timer = 0f;

            while (timer < wait)
            {
                if (!isPulsing)
                {
                    LerpToMultiplier(organicTargetMultiplier, organicSmoothSpeed);
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator RandomPulseLoop()
    {
        while (true)
        {
            float wait = Random.Range(timeBetweenPulses.x, timeBetweenPulses.y);
            yield return new WaitForSeconds(wait);

            float pulseMultiplier = Random.Range(pulseIntensityRange.x, pulseIntensityRange.y);
            float pulseDuration = Random.Range(pulseDurationRange.x, pulseDurationRange.y);

            yield return StartCoroutine(DoPulse(pulseMultiplier, pulseDuration));
        }
    }

    private IEnumerator DoPulse(float intensityMultiplier, float duration)
    {
        isPulsing = true;
        SetIntensityMultiplier(intensityMultiplier);

        yield return new WaitForSeconds(duration);

        float returnMultiplier = useOrganicVariation ? organicTargetMultiplier : 1f;

        if (!smoothReturnToBase)
        {
            SetIntensityMultiplier(returnMultiplier);
            isPulsing = false;
            yield break;
        }

        while (!IsNearMultiplier(returnMultiplier))
        {
            LerpToMultiplier(returnMultiplier, returnSpeed);
            yield return null;
        }

        SetIntensityMultiplier(returnMultiplier);
        isPulsing = false;
    }

    private void CacheBaseIntensities()
    {
        if (lights == null)
        {
            baseIntensities = null;
            return;
        }

        baseIntensities = new float[lights.Length];

        for (int i = 0; i < lights.Length; i++)
        {
            baseIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
        }
    }

    private void RestoreBaseIntensities()
    {
        SetIntensityMultiplier(1f);
    }

    private void SetIntensityMultiplier(float multiplier)
    {
        if (lights == null || baseIntensities == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
            {
                continue;
            }

            lights[i].intensity = baseIntensities[i] * multiplier;
        }
    }

    private void LerpToMultiplier(float multiplier, float speed)
    {
        if (lights == null || baseIntensities == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
            {
                continue;
            }

            float targetIntensity = baseIntensities[i] * multiplier;
            lights[i].intensity = Mathf.Lerp(lights[i].intensity, targetIntensity, speed * Time.deltaTime);
        }
    }

    private bool IsNearMultiplier(float multiplier)
    {
        if (lights == null || baseIntensities == null)
        {
            return true;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
            {
                continue;
            }

            float targetIntensity = baseIntensities[i] * multiplier;

            if (Mathf.Abs(lights[i].intensity - targetIntensity) > 0.01f)
            {
                return false;
            }
        }

        return true;
    }
}
