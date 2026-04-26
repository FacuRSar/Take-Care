using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip tensionMusic;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.75f;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }
    }

    public void PlayTensionMusic()
    {
        if (musicSource == null || tensionMusic == null)
        {
            Debug.LogWarning("MusicManager: falta musicSource o tensionMusic.");
            return;
        }

        // la musica vive aca y no en sfxmanager para no mezclar golpes puntuales con ambiente musical
        // Hace 2 horas estoy limpiando debug.logs lpm
        musicSource.clip = tensionMusic;
        musicSource.loop = true;
        musicSource.volume = defaultVolume;
        musicSource.Play();
        // Debug.Log("MusicManager: musica de tension iniciada.");
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void SetVolume(float value)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(value);
        }
    }
}
