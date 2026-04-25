using UnityEngine;

/* Manager simple para sonidos especificos
*  esta para reproducir sonidos 2D o 3D desde una pool de clips
*  la idea es que otros scripts pidan un sonido por ID, sin tener que conocer los AudioClips directamente
*/
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;
    // Lo instancio para llamarlo facil. Solo se pone SFXManager.Instance.Play2D("IdSound") o SFXManager.Instance.Play3D("IdSound", transform.position);
    // siendo IdSound el id del sonido que se quiere llamar

    [System.Serializable]
    public class SFXPool
    {
        public string id;
        // Nombre con el que vamos a pedir este grupo de sonidos. Ej: "WoodCreak", "PhoneRing", "Thunder"

        public AudioClip[] clips;
        // Variantes posibles del sonido.
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource oneShotSource;
    // AudioSource principal para sonidos 2D que se ecucha al unisono

    [Header("Pools")]
    [SerializeField] private SFXPool[] pools;
    // Lista de sonidos disponibles

    [Header("Configuraciones 3D")]
    [SerializeField] private float default3DVolume = 1f;
    // Volumen usado por defecto para sonidos 3D

    private void Awake()
    {
        // Solo por si acaso pongo un validador de instancia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (oneShotSource == null)
        {
            oneShotSource = GetComponent<AudioSource>();
        }
    }

    public void Play2D(string id)
    {
        AudioClip clip = GetRandomClip(id);

        if (clip == null || oneShotSource == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip);
    }

    public void Play3D(string id, Vector3 position)
    {
        AudioClip clip = GetRandomClip(id);

        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, default3DVolume);
    }

    private AudioClip GetRandomClip(string id)
    {
        SFXPool pool = GetPool(id);

        if (pool == null || pool.clips == null || pool.clips.Length == 0)
        {
            Debug.LogWarning("No se encontro pool de SFX o esta vacia: " + id);
            return null;
        }

        int randomIndex = Random.Range(0, pool.clips.Length);
        return pool.clips[randomIndex];
    }

    private SFXPool GetPool(string id)
    {
        foreach (SFXPool pool in pools)
        {
            if (pool.id == id)
            {
                return pool;
            }
        }

        return null;
    }
}