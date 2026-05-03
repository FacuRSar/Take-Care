using System.Collections;
using UnityEngine;

/* Controlador reutilizable para luces que pulsan/titilan.
*  Sirve para fogatas, lamparas viejas o luces defectuosas.
*  Todo se maneja como pulsos configurables.
*/
[RequireComponent(typeof(Light))]
public class LightPulseController : MonoBehaviour
{
    [Header("Pulso inicial")]
    [SerializeField] private bool useStartupPulse = false;
    [SerializeField] private float startupPulseIntensity = 2f;
    [SerializeField] private float startupPulseDuration = 0.15f;
    /* Controlamos si tira un pulso inicial (como encendiendo)
     * La intensidad de ese pulso inicial
     * La duracion de ese pulso inicial
     */

    [Header("Variacion organica")]
    [SerializeField] private bool useOrganicVariation = false;
    [SerializeField] private Vector2 organicIntensityRange = new Vector2(0.8f, 1.5f);
    [SerializeField] private Vector2 organicChangeTimeRange = new Vector2(0.05f, 0.18f);
    [SerializeField] private float organicSmoothSpeed = 12f;
    /* Si esta activo, la luz va cambiando entre intensidades random de forma constante
     * Esto sirve mucho para fogatas porque evita que la luz vuelva siempre a una base fija
     * Controlas el rango de intensidad, cada cuanto cambia y que tan suave se mueve
     */

    [Header("Pulsos random")]
    [SerializeField] private bool useRandomPulses = true;
    [SerializeField] private Vector2 timeBetweenPulses = new Vector2(1f, 3f);
    [SerializeField] private Vector2 pulseIntensityRange = new Vector2(0.6f, 1.4f);
    [SerializeField] private Vector2 pulseDurationRange = new Vector2(0.05f, 0.15f);
    /* Si esta activo, hace pulsos random cada cierto tiempo
     * Controlas el tiempo minimo y maximo entre pulsos
     * El rango de intensidad para cada pulso
     * y el rango de duracion random para cada pulso
     */

    [Header("Configuraciones adicionales")]
    [SerializeField] private bool smoothReturnToBase = true;
    [SerializeField] private float returnSpeed = 8f;
    //[SerializeField] private bool showDebugLogs = false;
    /* Si esta activo, mete smooth a la vuelta de intensidad
     * y le controlas tambien la velocidad con la que vuelve
     */

    private float baseIntensity;
    private float organicTargetIntensity;
    private Light targetLight;
    private Coroutine pulseRoutine;
    private Coroutine organicRoutine;
    private bool lastLightEnabledState;
    private bool isPulsing;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        lastLightEnabledState = targetLight.enabled;
        baseIntensity = targetLight.intensity;
        organicTargetIntensity = baseIntensity;
    }

    private void OnEnable()
    {
        if (targetLight == null) return;

        lastLightEnabledState = targetLight.enabled;

        if (targetLight.enabled) StartPulse();
    }

    private void Update()
    {
        if (targetLight.enabled == lastLightEnabledState) return;

        lastLightEnabledState = targetLight.enabled;

        if (targetLight.enabled)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private IEnumerator OrganicVariationLoop()
    {
        while (true)
        {
            // Le agrego variacion respecto a la intensidad base
            float organicMultiplier = Random.Range(organicIntensityRange.x, organicIntensityRange.y);
            organicTargetIntensity = baseIntensity * organicMultiplier;

            float wait = Random.Range(organicChangeTimeRange.x, organicChangeTimeRange.y);
            float timer = 0f;

            while (timer < wait)
            {
                if (!isPulsing)
                {
                    targetLight.intensity = Mathf.Lerp(
                        targetLight.intensity,
                        organicTargetIntensity,
                        organicSmoothSpeed * Time.deltaTime
                    );
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

            // Variacion atada a la intensidad base
            float pulseMultiplier = Random.Range(pulseIntensityRange.x, pulseIntensityRange.y);
            float pulseIntensity = baseIntensity * pulseMultiplier;
            float pulseDuration = Random.Range(pulseDurationRange.x, pulseDurationRange.y);

            //if (showDebugLogs) Debug.Log(gameObject.name + " pulso random | intensidad: " + pulseIntensity + " duracion: " + pulseDuration);

            yield return StartCoroutine(DoPulse(pulseIntensity, pulseDuration));
        }
    }

    private IEnumerator DoPulse(float intensity, float duration)
    {
        isPulsing = true;

        //if (showDebugLogs) Debug.Log(gameObject.name + " inicia pulso | intensidad: " + intensity);

        targetLight.intensity = intensity;

        yield return new WaitForSeconds(duration);

        float returnIntensity = useOrganicVariation ? organicTargetIntensity : baseIntensity;

        if (!smoothReturnToBase)
        {
            targetLight.intensity = returnIntensity;
            isPulsing = false;

            //if (showDebugLogs) Debug.Log(gameObject.name + " termina pulso sin smooth | vuelve a: " + returnIntensity);
            yield break;
        }

        while (Mathf.Abs(targetLight.intensity - returnIntensity) > 0.01f)
        {
            targetLight.intensity = Mathf.Lerp(
                targetLight.intensity,
                returnIntensity,
                returnSpeed * Time.deltaTime
            );

            yield return null;
        }

        targetLight.intensity = returnIntensity;
        isPulsing = false;

        //if (showDebugLogs) Debug.Log(gameObject.name + " termina pulso | vuelve a: " + returnIntensity);
    }

    public void StartPulse()
    {
        StopPulse();

        baseIntensity = targetLight.intensity;
        organicTargetIntensity = baseIntensity;
        isPulsing = false;

        if (useOrganicVariation) organicRoutine = StartCoroutine(OrganicVariationLoop());
        if (useStartupPulse) StartCoroutine(DoPulse(baseIntensity * startupPulseIntensity, startupPulseDuration));
        if (useRandomPulses) pulseRoutine = StartCoroutine(RandomPulseLoop());
    }

    public void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (organicRoutine != null)
        {
            StopCoroutine(organicRoutine);
            organicRoutine = null;
        }

        isPulsing = false;
    }
}