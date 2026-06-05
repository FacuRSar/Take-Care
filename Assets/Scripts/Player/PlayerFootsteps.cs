using UnityEngine;

/* Reproduce sonidos de pasos del jugador basados en la velocidad real.
 *
 * No depende del PlayerHeadBob directamente: ambos usan PlayerMovement.CurrentSpeed,
 * asi que quedan visualmente sincronizados sin acoplar scripts.
 *
 * Como funciona:
 *  - Lleva un "stepPhase" que avanza cada frame proporcional a la velocidad.
 *  - Cada vez que stepPhase cruza PI (medio ciclo) dispara un paso.
 *  - Mas velocidad = mas pasos por segundo automaticamente.
 *  - Si el clip ya contiene mas de un paso (ej. dos pasos por archivo), se puede
 *    bajar stepFrequencyMultiplier para compensar.
 *
 * Cambia el pool de sonidos segun el estado (camina, corre, agachado).
 */
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AudioSource source;

    [Header("Pools de sonidos")]
    // Si dejas vacios sprintClips o crouchClips se usa walkClips para esos estados.
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] sprintClips;
    [SerializeField] private AudioClip[] crouchClips;

    [Header("Velocidad de pasos")]
    // Multiplicador sobre la velocidad del jugador para definir la frecuencia.
    // 2 = aprox 1 paso por unidad de speed. Subi este valor si los pasos suenan muy lentos.
    [SerializeField] private float stepFrequencyMultiplier = 2f;

    // Velocidad minima para considerar que el jugador esta caminando.
    // Evita pasos cuando se mueve apenas (ej. empuja una pared).
    [SerializeField] private float minSpeedToStep = 0.5f;

    [Header("Variacion")]
    [SerializeField, Range(0f, 1f)] private float volumeMin = 0.85f;
    [SerializeField, Range(0f, 1f)] private float volumeMax = 1f;
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.04f;

    private float stepPhase;
    private AudioClip lastClipPlayed;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        if (source != null)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        if (playerMovement == null || source == null)
        {
            return;
        }

        bool moving = playerMovement.IsMoving && playerMovement.CurrentSpeed >= minSpeedToStep;

        if (!moving)
        {
            // reseteamos la fase asi el proximo paso suena al toque y no a la mitad
            stepPhase = 0f;
            return;
        }

        stepPhase += Time.deltaTime * playerMovement.CurrentSpeed * stepFrequencyMultiplier;

        if (stepPhase >= Mathf.PI)
        {
            stepPhase -= Mathf.PI;
            PlayStep();
        }
    }

    private void PlayStep()
    {
        AudioClip[] pool = SelectPool();

        if (pool == null || pool.Length == 0)
        {
            return;
        }

        AudioClip clip = GetRandomClipAvoidingRepeat(pool);

        if (clip == null)
        {
            return;
        }

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        float volume = Random.Range(volumeMin, volumeMax);
        source.PlayOneShot(clip, volume);

        lastClipPlayed = clip;
    }

    private AudioClip[] SelectPool()
    {
        if (playerMovement.IsCrouching && crouchClips != null && crouchClips.Length > 0)
        {
            return crouchClips;
        }

        if (playerMovement.IsSprinting && sprintClips != null && sprintClips.Length > 0)
        {
            return sprintClips;
        }

        return walkClips;
    }

    private AudioClip GetRandomClipAvoidingRepeat(AudioClip[] pool)
    {
        if (pool.Length == 1)
        {
            return pool[0];
        }

        AudioClip clip = pool[Random.Range(0, pool.Length)];

        // Si salio el mismo que el anterior, lo intentamos una vez mas para variar.
        if (clip == lastClipPlayed)
        {
            clip = pool[Random.Range(0, pool.Length)];
        }

        return clip;
    }
}
