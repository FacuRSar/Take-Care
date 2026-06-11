using System.Collections;
using UnityEngine;




/* Manager para ambiente.
*  Controla sonidos constantes como lluvia/viento y eventos aleatorios de ambiente como rayos
*  Ademas tambien controla los flashes de los rayos.
*/
public class AmbientManager : MonoBehaviour
{
    public static AmbientManager Instance;
    // Se instancia

    [Header("Loop de ambiente")]
    [SerializeField] private AudioSource[] ambientSources;
    // AudioSources que reproducen ambiente constante
    // (lluvia afuera, viento, interior de casa, etc)

    //[SerializeField] private float ambientVolume = 1f;
    // Volumen normal del ambiente

    [SerializeField] private bool playOnStart = true;
    // Si esta activo, arranca el ambiente apenas empieza la escena

    [Header("Audios de trueno")]
    [SerializeField] private AudioSource[] thunderSources;
    // AudioSource que reproduce los rayos que le manda al oneshot

    [SerializeField] private AudioClip[] thunderClips;
    // Variantes de rayos para que no suene siempre igual.

    [SerializeField] private Vector2 thunderDelayRange = new Vector2(6f, 14f);
    // tiempo min. y max. entre chequeos de rayo

    [SerializeField] private float thunderChance = 0.7f;
    // Probabilidad de que suene un rayo cuando llega el chequeo
    // 1 = siempre, 0 = nunca

    [Header("Sonidos de ambiente al azar")]
    // ids de pools del SFXManager que suenan cada tanto (crujidos, golpes lejanos, susurros, etc.)
    [SerializeField] private string[] randomAmbientSfxIds;
    // tiempo min. y max. entre intentos de sonido. amplio para no agobiar en una partida corta
    [SerializeField] private Vector2 randomAmbientDelayRange = new Vector2(20f, 45f);
    // probabilidad de que suene cuando llega el chequeo (1 = siempre)
    [SerializeField, Range(0f, 1f)] private float randomAmbientChance = 0.7f;
    // si suena en 3D necesita puntos en el mundo; si no, suena 2D/global
    [SerializeField] private bool randomAmbientAs3D = false;
    // puntos opcionales desde donde puede salir el sonido 3D (elige uno al azar)
    [SerializeField] private Transform[] randomAmbientPoints;
    [SerializeField] private bool playRandomAmbientOnStart = false;

    [Header("Config. Luces de rayos")]
    [SerializeField] private Light[] lightningLights;
    // Luces que van a flashear cuando cae un rayo
    // Se pueden agregar varias

    [SerializeField] private float lightningIntensity = 4f;
    // Intensidad que tendran las luces durante el flash

    [SerializeField] private float flashDuration = 0.08f;
    // Duracion de cada flash.

    [SerializeField] private int flashCount = 2;
    // Cantidad de flashes por rayo.

    [SerializeField] private Vector2 timeBetweenFlashes = new Vector2(0.05f, 0.18f);
    // Tiempo aleatorio entre flashes para que no sea tan mecanico.

    private Coroutine thunderRoutine;
    private Coroutine randomAmbientRoutine;
    private float[] originalLightIntensities;
    // Guarda la intensidad original de cada luz para restaurarla despues.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CacheOriginalLightIntensities();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartAmbient();
            StartThunderLoop();
        }

        if (playRandomAmbientOnStart)
        {
            StartRandomAmbientLoop();
        }
    }

    public void StartAmbient()
    {
        // Arranca todos los AudioSources de ambiente configurados.
        foreach (AudioSource source in ambientSources)
        {
            if (source == null)
            {
                continue;
            }

            source.loop = true;
           // source.volume = ambientVolume;

            if (!source.isPlaying)
            {
                source.Play();
            }
        }
    }

    public void StopAmbient()
    {
        // Detiene los sonidos de ambiente constantes.
        foreach (AudioSource source in ambientSources)
        {
            if (source == null)
            {
                continue;
            }

            source.Stop();
        }
    }
    /*
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = volume;

        foreach (AudioSource source in ambientSources)
        {
            if (source == null)
            {
                continue;
            }

            source.volume = ambientVolume;
        }
    }
    */
    public void StartThunderLoop()
    {
        if (thunderRoutine != null)
        {
            StopCoroutine(thunderRoutine);
        }

        thunderRoutine = StartCoroutine(ThunderLoop());
    }

    public void StopThunderLoop()
    {
        if (thunderRoutine != null)
        {
            StopCoroutine(thunderRoutine);
            thunderRoutine = null;
        }
    }

    public void TriggerThunderNow()
    {
        // Permite disparar un rayo manualmente desde otro evento si hace falta.
        PlayRandomThunder();
        StartCoroutine(FlashLightning());
    }

    public void StartRandomAmbientLoop()
    {
        if (randomAmbientRoutine != null)
        {
            StopCoroutine(randomAmbientRoutine);
        }

        randomAmbientRoutine = StartCoroutine(RandomAmbientLoop());
    }

    public void StopRandomAmbientLoop()
    {
        if (randomAmbientRoutine != null)
        {
            StopCoroutine(randomAmbientRoutine);
            randomAmbientRoutine = null;
        }
    }

    private IEnumerator RandomAmbientLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(randomAmbientDelayRange.x, randomAmbientDelayRange.y);
            yield return new WaitForSeconds(waitTime);

            if (Random.value <= randomAmbientChance)
            {
                PlayRandomAmbientSound();
            }
        }
    }

    private void PlayRandomAmbientSound()
    {
        if (randomAmbientSfxIds == null || randomAmbientSfxIds.Length == 0 || SFXManager.Instance == null)
        {
            return;
        }

        string id = randomAmbientSfxIds[Random.Range(0, randomAmbientSfxIds.Length)];

        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (randomAmbientAs3D && randomAmbientPoints != null && randomAmbientPoints.Length > 0)
        {
            Transform point = randomAmbientPoints[Random.Range(0, randomAmbientPoints.Length)];
            Vector3 position = point != null ? point.position : transform.position;
            SFXManager.Instance.Play3D(id, position);
        }
        else
        {
            SFXManager.Instance.Play2D(id);
        }
    }

    private IEnumerator ThunderLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(thunderDelayRange.x, thunderDelayRange.y);
            yield return new WaitForSeconds(waitTime);

            float roll = Random.value;

            if (roll <= thunderChance)
            {
                TriggerThunderNow();
            }
        }
    }

    private void PlayRandomThunder()
    {
        if (thunderSources == null)
        {
            Debug.LogWarning("AmbientManager no tiene thunderSources asignado.");
            return;
        }

        if (thunderClips == null || thunderClips.Length == 0)
        {
            Debug.LogWarning("AmbientManager no tiene thunderClips cargados.");
            return;
        }

        int index = Random.Range(0, thunderClips.Length);

        foreach (AudioSource thunder in thunderSources)
        {
            thunder.PlayOneShot(thunderClips[index]);
        }
    }

    private IEnumerator FlashLightning()
    {
        if (lightningLights == null || lightningLights.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < flashCount; i++)
        {
            SetLightningIntensity(lightningIntensity);
            yield return new WaitForSeconds(flashDuration);

            RestoreLightIntensities();

            float wait = Random.Range(timeBetweenFlashes.x, timeBetweenFlashes.y);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SetLightningIntensity(float intensity)
    {
        foreach (Light lightSource in lightningLights)
        {
            if (lightSource == null)
            {
                continue;
            }

            lightSource.intensity = intensity;
            lightSource.enabled = true;
        }
    }

    private void RestoreLightIntensities()
    {
        if (lightningLights == null || originalLightIntensities == null)
        {
            return;
        }

        for (int i = 0; i < lightningLights.Length; i++)
        {
            if (lightningLights[i] == null)
            {
                continue;
            }

            lightningLights[i].intensity = originalLightIntensities[i];
        }
    }

    private void CacheOriginalLightIntensities()
    {
        if (lightningLights == null)
        {
            return;
        }

        originalLightIntensities = new float[lightningLights.Length];

        for (int i = 0; i < lightningLights.Length; i++)
        {
            if (lightningLights[i] == null)
            {
                continue;
            }

            originalLightIntensities[i] = lightningLights[i].intensity;
        }
    }
}