using UnityEngine;

/* Movimiento sutil de camara para escenas estaticas (menu, cinematicas cortas, etc).
 * Suma un offset de posicion y rotacion basado en ruido Perlin para que se sienta
 * "viva" sin moverla bruscamente.
 *
 * Pone esto en el GameObject de la camara del menu y ajusta amplitudes/frecuencias
 * desde el Inspector. Todo es opcional: si una amplitud es 0, ese eje no se mueve.
 *
 * Uso recomendado para menu: amplitudes muy chicas (rotacion 0.3-0.8 grados,
 * posicion 0.01-0.03). Frecuencias bajas (0.1-0.4) para que sea lento.
 */
public class CameraIdleSway : MonoBehaviour
{
    [Header("Posicion")]
    // Amplitud maxima de desplazamiento en cada eje, en unidades del mundo.
    [SerializeField] private Vector3 positionAmplitude = new Vector3(0.02f, 0.015f, 0f);
    // Velocidad del ruido por eje. Mas alto = se mueve mas rapido.
    [SerializeField] private Vector3 positionFrequency = new Vector3(0.2f, 0.25f, 0.2f);

    [Header("Rotacion (grados)")]
    [SerializeField] private Vector3 rotationAmplitude = new Vector3(0.4f, 0.6f, 0.2f);
    [SerializeField] private Vector3 rotationFrequency = new Vector3(0.15f, 0.2f, 0.1f);

    [Header("Comportamiento")]
    // Tarda este tiempo en alcanzar la amplitud configurada al activarse.
    // Sirve para no romper transiciones de camara con un salto rancio.
    [SerializeField] private float warmUpDuration = 1f;
    // Si esta en true ignora Time.timeScale. Para la pausa.
    [SerializeField] private bool useUnscaledTime = true;
    // Distintos seeds aleatorios por eje asi los 6 canales no se sincronizan.
    [SerializeField] private bool randomizeSeeds = true;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float elapsed;
    private float warmUp;

    private Vector3 posSeedX, posSeedY, posSeedZ;
    private Vector3 rotSeedX, rotSeedY, rotSeedZ;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        if (randomizeSeeds)
        {
            posSeedX = new Vector3(Random.value * 1000f, 0f, 0f);
            posSeedY = new Vector3(0f, Random.value * 1000f, 0f);
            posSeedZ = new Vector3(0f, 0f, Random.value * 1000f);
            rotSeedX = new Vector3(Random.value * 1000f, 0f, 0f);
            rotSeedY = new Vector3(0f, Random.value * 1000f, 0f);
            rotSeedZ = new Vector3(0f, 0f, Random.value * 1000f);
        }
    }

    private void OnEnable()
    {
        warmUp = 0f;
    }

    private void LateUpdate()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsed += dt;

        if (warmUpDuration > 0f)
        {
            warmUp = Mathf.Min(1f, warmUp + dt / warmUpDuration);
        }
        else
        {
            warmUp = 1f;
        }

        Vector3 posOffset = new Vector3(
            SampleNoise(elapsed, positionFrequency.x, posSeedX.x),
            SampleNoise(elapsed, positionFrequency.y, posSeedY.y),
            SampleNoise(elapsed, positionFrequency.z, posSeedZ.z)
        );

        posOffset.x *= positionAmplitude.x;
        posOffset.y *= positionAmplitude.y;
        posOffset.z *= positionAmplitude.z;

        Vector3 rotOffset = new Vector3(
            SampleNoise(elapsed, rotationFrequency.x, rotSeedX.x) * rotationAmplitude.x,
            SampleNoise(elapsed, rotationFrequency.y, rotSeedY.y) * rotationAmplitude.y,
            SampleNoise(elapsed, rotationFrequency.z, rotSeedZ.z) * rotationAmplitude.z
        );

        transform.localPosition = basePosition + posOffset * warmUp;
        transform.localRotation = baseRotation * Quaternion.Euler(rotOffset * warmUp);
    }

    // Devuelve un valor entre -1 y 1 usando Perlin desplazado.
    private static float SampleNoise(float time, float frequency, float seed)
    {
        return (Mathf.PerlinNoise(time * frequency + seed, seed * 0.37f) - 0.5f) * 2f;
    }

    // API publica por si despues queres "calmar" la camara desde otro script (ej. cinematica).
    public void ResetToBase()
    {
        transform.localPosition = basePosition;
        transform.localRotation = baseRotation;
        elapsed = 0f;
        warmUp = 0f;
    }
}
