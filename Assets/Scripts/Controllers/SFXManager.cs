using System.Collections.Generic;
using UnityEngine;

/* manager simple para sonidos especificos
*  esta para reproducir sonidos 2d o 3d desde una pool de clips
*  la idea es que otros scripts pidan un sonido por id, sin tener que conocer los audioclips directamente
*/
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;
    // lo instancio para llamarlo facil. solo se pone sfxmanager.instance.play2d("idsound") o sfxmanager.instance.play3d("idsound", transform.position);
    // siendo idsound el id del sonido que se quiere llamar

    [System.Serializable]
    public class SFXPool
    {
        public string id;
        // nombre con el que vamos a pedir este grupo de sonidos. ej: "woodcreak", "phonering", "thunder"

        public AudioClip[] clips;
        // variantes posibles del sonido

        [Range(0f, 1f)] public float volume = 1f;
        // volumen
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource oneShotSource;
    // audiosource principal para sonidos 2d que se ecucha al unisono

    [Header("Pools")]
    [SerializeField] private SFXPool[] pools;
    // lista de sonidos disponibles

    [Header("Configuraciones 3D")]
    [SerializeField] private float default3DVolume = 1f;
    // volumen usado por defecto para sonidos 3d

    [SerializeField] private float min3DDistance = 3f;
    // hasta esta distancia el sonido 3d suena a volumen full. mas alto = se escucha fuerte aunque estes algo lejos
    [SerializeField] private float max3DDistance = 25f;
    // distancia donde el sonido 3d ya no se escucha

    private Dictionary<string, AudioSource> activeLoops = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        // solo por si acaso pongo un validador de instancia
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

    public AudioClip Play2D(string id)
    {
        AudioClip clip = GetRandomClip(id);
        SFXPool pool = GetPool(id);

        if (clip == null || oneShotSource == null)
        {
            return null;
        }
        oneShotSource.PlayOneShot(clip, pool.volume);
        //oneShotSource.PlayOneShot(clip);
        return clip;
    }

    public AudioClip Play3D(string id, Vector3 position)
    {
        AudioClip clip = GetRandomClip(id);

        if (clip == null)
        {
            return null;
        }
        SFXPool pool = GetPool(id);

        if (pool == null)
        {
            return null;
        }

        // antes usaba PlayClipAtPoint, pero ese arranca con minDistance 1 y rolloff logaritmico,
        // asi que a pocos metros casi no se escuchaba. armo un AudioSource propio con rolloff lineal
        // y distancias configurables para que el golpe se oiga bien.
        GameObject soundObject = new GameObject("OneShot3D_" + id);
        soundObject.transform.position = position;

        // resguardo: si las distancias quedaron en 0 (campo nuevo sobre un componente ya serializado)
        // uso valores sanos, si no el sonido sale mudo por maxDistance 0
        float minDist = min3DDistance > 0f ? min3DDistance : 3f;
        float maxDist = max3DDistance > minDist ? max3DDistance : minDist + 22f;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = default3DVolume * pool.volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDist;
        source.maxDistance = maxDist;
        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
        return clip;
    }

    public void ResetVolume2D(string id  ,float NewVolume)
    {
        if (!activeLoops.ContainsKey(id) || activeLoops[id] == null) return;

        activeLoops[id].volume = NewVolume;

    }
    public AudioSource PlayLoop2D(string id, float volume = -1)
    {
        
        if (activeLoops.ContainsKey(id) && activeLoops[id] != null)
        {
            return activeLoops[id];
        }

        // loop 2d para sonidos globales, tipo telefono/estatica. queda registrado por id para frenarlo despues
        // sin esto despues hay que perseguir audiosources
        AudioClip clip = GetRandomClip(id);

        if (clip == null)
        {
            return null;
        }

        AudioSource loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = clip;
        loopSource.loop = true;
        if(volume != -1) loopSource.volume = volume;
        loopSource.spatialBlend = 0f;
        loopSource.Play();
        activeLoops[id] = loopSource;

        return loopSource;
    }

    public AudioSource PlayLoop3D(string id, Vector3 position)
    {
        if (activeLoops.ContainsKey(id) && activeLoops[id] != null)
        {
            return activeLoops[id];
        }

        // loop 3d para cosas que viven en el mundo, como la canilla
        // lo creo en un gameobject aparte asi queda en el punto correcto
        AudioClip clip = GetRandomClip(id);

        if (clip == null)
        {
            return null;
        }

        GameObject loopObject = new GameObject("LoopingSFX_" + id);
        loopObject.transform.position = position;

        AudioSource loopSource = loopObject.AddComponent<AudioSource>();
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
        loopSource.volume = default3DVolume;
        loopSource.Play();
        activeLoops[id] = loopSource;

        return loopSource;
    }

    public void StopLoop(string id)
    {
        if (!activeLoops.ContainsKey(id))
        {
            return;
        }

        AudioSource loopSource = activeLoops[id];

        if (loopSource != null)
        {
            GameObject loopObject = loopSource.gameObject;
            loopSource.Stop();

            if (loopObject == gameObject)
            {
                Destroy(loopSource);
            }
            else
            {
                Destroy(loopObject);
            }
        }

        activeLoops.Remove(id);
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
        if (pools == null)
        {
            return null;
        }

        foreach (SFXPool pool in pools)
        {
            if (pool != null && pool.id == id)
            {
                return pool;
            }
        }

        return null;
    }
}
