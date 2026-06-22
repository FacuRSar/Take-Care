using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* Manager de musica.
 * Patron similar al SFXManager pero:
 *   - Siempre loop.
 *   - Una pista principal + capas opcionales mezcladas (AddMusic).
 *   - Todas las operaciones (Play/Stop/Pause/Resume/AddMusic/RemoveMusic)
 *     aceptan un fade en segundos. Si no se pasa o es 0, es instantaneo.
 *
 * Uso tipico:
 *   MusicManager.Instance.Play("tension", 2f);
 *   MusicManager.Instance.AddMusic("strings", 1.5f, waitForLoopStart: true);
 *   MusicManager.Instance.Stop(1f);
 */
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [System.Serializable]
    public class MusicTrack
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)] public float defaultVolume = 1f;
    }

    [Header("Pistas configuradas")]
    [SerializeField] private MusicTrack[] tracks;

    [Header("Audio")]
    [SerializeField] private AudioSource mainSource;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup mixerGroup;
    [SerializeField, Range(0f, 1f)] private float globalVolume = 1f;

    public enum PauseBehavior
    {
        Pause,
        LowerVolume
    }

    [Header("Comportamiento en pausa del juego")]
    // Pause: detiene la musica mientras el juego este en pausa.
    // LowerVolume: la musica sigue sonando pero con el volumen reducido temporalmente.
    [SerializeField] private PauseBehavior pauseBehavior = PauseBehavior.Pause;

    // Multiplicador de volumen cuando pauseBehavior = LowerVolume. 0.5 = mitad.
    [SerializeField, Range(0f, 1f)] private float pauseVolumeMultiplier = 0.5f;

    // Duracion del fade al entrar/salir de pausa, en segundos.
    [SerializeField] private float pauseFade = 0.3f;

    [Header("Debug (testeo)")]
    // Si esta activo, las teclas del numpad 7/8/9 reproducen los ids de abajo con Play().
    [SerializeField] private bool enableDebugKeys = false;
    [SerializeField] private string debugTrackId7 = "";
    [SerializeField] private string debugTrackId8 = "";
    [SerializeField] private string debugTrackId9 = "";
    // Fade que usa el debug al cambiar de pista.
    [SerializeField] private float debugFade = 1f;

    private readonly Dictionary<string, AudioSource> layeredSources = new Dictionary<string, AudioSource>();
    private readonly Dictionary<AudioSource, Coroutine> activeFades = new Dictionary<AudioSource, Coroutine>();

    private string currentTrackId;

    private float currentDuckMultiplier = 1f;
    private bool gamePaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (mainSource == null)
        {
            mainSource = GetComponent<AudioSource>();
            if (mainSource == null)
            {
                mainSource = gameObject.AddComponent<AudioSource>();
            }
        }

        ConfigureSource(mainSource);
    }

    private void Update()
    {
        HandleDebugKeys();
    }

    // Debug de testeo: numpad 7/8/9 reproducen los ids configurados en el inspector.
    private void HandleDebugKeys()
    {
        if (!enableDebugKeys || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.numpad7Key.wasPressedThisFrame)
        {
            DebugPlay(debugTrackId7);
        }

        if (Keyboard.current.numpad8Key.wasPressedThisFrame)
        {
            DebugPlay(debugTrackId8);
        }

        if (Keyboard.current.numpad9Key.wasPressedThisFrame)
        {
            DebugPlay(debugTrackId9);
        }
    }

    private void DebugPlay(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            //Debug.LogWarning("[MusicManager] Debug: no hay id configurado para esa tecla.");
            return;
        }

        //Debug.Log("[MusicManager] Debug Play: " + id);
        Play(id, debugFade);
    }

    // ----------------------- API publica -----------------------

    // Cambia (o arranca) la musica principal. Si ya habia una, se hace fade out + fade in.
    public void Play(string id, float fade = 0f)
    {
        MusicTrack track = GetTrack(id);
        if (track == null || track.clip == null)
        {
            //Debug.LogWarning("[MusicManager] No se encontro la pista de musica: " + id);
            return;
        }

        if (currentTrackId == id && mainSource.isPlaying)
        {
            return;
        }

        if (mainSource.isPlaying && fade > 0f)
        {
            StartFade(mainSource, mainSource.volume, 0f, fade, () =>
            {
                SwapTrack(track);
                StartFade(mainSource, 0f, TargetVolume(track), fade);
            });
        }
        else
        {
            SwapTrack(track);

            if (fade > 0f)
            {
                StartFade(mainSource, 0f, TargetVolume(track), fade);
            }
            else
            {
                mainSource.volume = TargetVolume(track);
            }
        }
    }

    public void Stop(float fade = 0f)
    {
        if (!mainSource.isPlaying && layeredSources.Count == 0)
        {
            return;
        }

        if (fade <= 0f)
        {
            mainSource.Stop();
            StopAllLayers(0f, destroy: true);
            currentTrackId = null;
            return;
        }

        StartFade(mainSource, mainSource.volume, 0f, fade, () =>
        {
            mainSource.Stop();
            currentTrackId = null;
        });

        StopAllLayers(fade, destroy: true);
    }

    public void Pause(float fade = 0f)
    {
        if (!mainSource.isPlaying)
        {
            return;
        }

        if (fade <= 0f)
        {
            mainSource.Pause();
            foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
            {
                if (kv.Value != null)
                {
                    kv.Value.Pause();
                }
            }
            return;
        }

        StartFade(mainSource, mainSource.volume, 0f, fade, () => mainSource.Pause());

        foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
        {
            AudioSource src = kv.Value;
            if (src == null)
            {
                continue;
            }

            StartFade(src, src.volume, 0f, fade, () => src.Pause());
        }
    }

    public void Resume(float fade = 0f)
    {
        if (mainSource.clip == null)
        {
            return;
        }

        if (mainSource.isPlaying)
        {
            return;
        }

        MusicTrack track = GetTrack(currentTrackId);
        float targetVolume = track != null ? TargetVolume(track) : globalVolume;

        if (fade <= 0f)
        {
            mainSource.UnPause();
            mainSource.volume = targetVolume;

            foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
            {
                AudioSource src = kv.Value;
                if (src == null || src.clip == null)
                {
                    continue;
                }

                src.UnPause();
                src.volume = LayerTargetVolume(kv.Key);
            }
            return;
        }

        mainSource.UnPause();
        StartFade(mainSource, 0f, targetVolume, fade);

        foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
        {
            AudioSource src = kv.Value;
            if (src == null || src.clip == null)
            {
                continue;
            }

            src.UnPause();
            StartFade(src, 0f, LayerTargetVolume(kv.Key), fade);
        }
    }

    // Suma una pista en paralelo a la principal.
    // waitForLoopStart=true: arranca cuando la principal vuelva a empezar el loop
    //                       (asi quedan alineadas en el inicio).
    // waitForLoopStart=false: arranca ya, sincronizada al "time" actual de la principal
    //                        (utiles si los clips tienen misma duracion).
    public void AddMusic(string id, float fade = 0f, bool waitForLoopStart = false)
    {
        MusicTrack track = GetTrack(id);
        if (track == null || track.clip == null)
        {
            //Debug.LogWarning("[MusicManager] No se encontro la pista para AddMusic: " + id);
            return;
        }

        if (layeredSources.ContainsKey(id) && layeredSources[id] != null)
        {
            return;
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(newSource);
        newSource.clip = track.clip;
        newSource.volume = 0f;

        layeredSources[id] = newSource;

        float targetVolume = LayerTargetVolume(id);

        if (waitForLoopStart && mainSource.clip != null && mainSource.isPlaying)
        {
            double dspNow = AudioSettings.dspTime;
            double remaining = mainSource.clip.length - mainSource.time;

            if (remaining < 0d)
            {
                remaining = 0d;
            }

            newSource.PlayScheduled(dspNow + remaining);

            if (fade > 0f)
            {
                StartCoroutine(DelayedFade(newSource, 0f, targetVolume, fade, (float)remaining));
            }
            else
            {
                StartCoroutine(DelayedSet(newSource, targetVolume, (float)remaining));
            }
        }
        else
        {
            if (mainSource.isPlaying && mainSource.clip != null)
            {
                newSource.time = mainSource.time;
            }

            newSource.Play();

            if (fade > 0f)
            {
                StartFade(newSource, 0f, targetVolume, fade);
            }
            else
            {
                newSource.volume = targetVolume;
            }
        }
    }

    public void RemoveMusic(string id, float fade = 0f)
    {
        if (!layeredSources.TryGetValue(id, out AudioSource src) || src == null)
        {
            return;
        }

        if (fade <= 0f)
        {
            src.Stop();
            Destroy(src);
            layeredSources.Remove(id);
            return;
        }

        StartFade(src, src.volume, 0f, fade, () =>
        {
            src.Stop();
            Destroy(src);
            layeredSources.Remove(id);
        });
    }

    public void SetVolume(float value)
    {
        globalVolume = Mathf.Clamp01(value);

        if (mainSource != null)
        {
            MusicTrack track = GetTrack(currentTrackId);
            mainSource.volume = track != null ? TargetVolume(track) : globalVolume;
        }

        foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
        {
            if (kv.Value != null)
            {
                kv.Value.volume = LayerTargetVolume(kv.Key);
            }
        }
    }

    public bool IsPlaying => mainSource != null && mainSource.isPlaying;
    public string CurrentTrackId => currentTrackId;

    // Reproduce una pista una sola vez (sin loop) y avisa cuando termina.
    public void PlayOnce(string id, float fadeIn = 0f, System.Action onComplete = null)
    {
        MusicTrack track = GetTrack(id);
        if (track == null || track.clip == null)
        {
            //Debug.LogWarning("[MusicManager] No se encontro la pista para PlayOnce: " + id);
            onComplete?.Invoke();
            return;
        }

        StopAllLayers(0f, destroy: true);

        if (mainSource.isPlaying && fadeIn > 0f)
        {
            StartFade(mainSource, mainSource.volume, 0f, fadeIn, () =>
            {
                StartOneShotTrack(track, fadeIn, onComplete);
            });
            return;
        }

        StartOneShotTrack(track, fadeIn, onComplete);
    }

    private void StartOneShotTrack(MusicTrack track, float fadeIn, System.Action onComplete)
    {
        mainSource.loop = false;
        mainSource.clip = track.clip;
        mainSource.volume = fadeIn > 0f ? 0f : TargetVolume(track);
        mainSource.Play();
        currentTrackId = track.id;

        if (fadeIn > 0f)
        {
            StartFade(mainSource, 0f, TargetVolume(track), fadeIn);
        }

        StartCoroutine(WaitForOneShotEnd(mainSource, onComplete));
    }

    private IEnumerator WaitForOneShotEnd(AudioSource source, System.Action onComplete)
    {
        if (source == null || source.clip == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        while (source.isPlaying)
        {
            yield return null;
        }

        currentTrackId = null;
        onComplete?.Invoke();
    }

    // Lo llama el PauseMenuController cuando entra a pausa.
    public void OnGamePauseStart()
    {
        if (gamePaused)
        {
            return;
        }

        gamePaused = true;

        if (pauseBehavior == PauseBehavior.Pause)
        {
            Pause(pauseFade);
        }
        else
        {
            ApplyDuckMultiplier(pauseVolumeMultiplier);
        }
    }

    // Lo llama el PauseMenuController cuando sale de pausa.
    public void OnGamePauseEnd()
    {
        if (!gamePaused)
        {
            return;
        }

        gamePaused = false;

        if (pauseBehavior == PauseBehavior.Pause)
        {
            Resume(pauseFade);
        }
        else
        {
            ApplyDuckMultiplier(1f);
        }
    }

    private void ApplyDuckMultiplier(float multiplier)
    {
        currentDuckMultiplier = Mathf.Clamp01(multiplier);

        if (mainSource != null && mainSource.isPlaying)
        {
            MusicTrack track = GetTrack(currentTrackId);
            float target = track != null ? TargetVolume(track) : globalVolume * currentDuckMultiplier;
            StartFade(mainSource, mainSource.volume, target, pauseFade);
        }

        foreach (KeyValuePair<string, AudioSource> kv in layeredSources)
        {
            AudioSource src = kv.Value;
            if (src == null || !src.isPlaying)
            {
                continue;
            }

            StartFade(src, src.volume, LayerTargetVolume(kv.Key), pauseFade);
        }
    }

    // ----------------------- Internos -----------------------

    private void SwapTrack(MusicTrack track)
    {
        StopAllLayers(0f, destroy: true);

        mainSource.clip = track.clip;
        mainSource.loop = true;
        mainSource.volume = 0f;
        mainSource.Play();
        currentTrackId = track.id;
    }

    private void StopAllLayers(float fade, bool destroy)
    {
        if (layeredSources.Count == 0)
        {
            return;
        }

        List<string> ids = new List<string>(layeredSources.Keys);
        foreach (string id in ids)
        {
            AudioSource src = layeredSources[id];
            if (src == null)
            {
                layeredSources.Remove(id);
                continue;
            }

            if (fade <= 0f)
            {
                src.Stop();
                if (destroy)
                {
                    Destroy(src);
                }
                layeredSources.Remove(id);
            }
            else
            {
                string capturedId = id;
                AudioSource capturedSrc = src;
                StartFade(capturedSrc, capturedSrc.volume, 0f, fade, () =>
                {
                    capturedSrc.Stop();
                    if (destroy)
                    {
                        Destroy(capturedSrc);
                    }
                    layeredSources.Remove(capturedId);
                });
            }
        }
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;

        if (mixerGroup != null)
        {
            source.outputAudioMixerGroup = mixerGroup;
        }
    }

    private MusicTrack GetTrack(string id)
    {
        if (string.IsNullOrEmpty(id) || tracks == null)
        {
            return null;
        }

        foreach (MusicTrack track in tracks)
        {
            if (track != null && track.id == id)
            {
                return track;
            }
        }

        return null;
    }

    private float TargetVolume(MusicTrack track)
    {
        return Mathf.Clamp01(track.defaultVolume * globalVolume * currentDuckMultiplier);
    }

    private float LayerTargetVolume(string id)
    {
        MusicTrack track = GetTrack(id);
        return track != null ? TargetVolume(track) : globalVolume;
    }

    private void StartFade(AudioSource source, float from, float to, float duration, System.Action onComplete = null)
    {
        if (source == null)
        {
            return;
        }

        if (activeFades.TryGetValue(source, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        activeFades[source] = StartCoroutine(FadeRoutine(source, from, to, duration, onComplete));
    }

    private IEnumerator FadeRoutine(AudioSource source, float from, float to, float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        source.volume = from;

        while (elapsed < duration)
        {
            if (source == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (source != null)
        {
            source.volume = to;
        }

        activeFades.Remove(source);
        onComplete?.Invoke();
    }

    private IEnumerator DelayedFade(AudioSource source, float from, float to, float duration, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (source != null)
        {
            StartFade(source, from, to, duration);
        }
    }

    private IEnumerator DelayedSet(AudioSource source, float value, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (source != null)
        {
            source.volume = value;
        }
    }
    public void targetChangeVolume(string id, float newVolume, float fade)
    {
        if (id == currentTrackId)
        {
            MusicTrack track = GetTrack(currentTrackId);
            float target = track != null ? TargetVolume(track) : globalVolume * currentDuckMultiplier;
            StartFade(mainSource, mainSource.volume, target * newVolume, fade);
        }
        else if (layeredSources.TryGetValue(id, out AudioSource src) && src != null)
        {
            StartFade(src, src.volume, LayerTargetVolume(id) * newVolume, fade);
        }
    }
}
