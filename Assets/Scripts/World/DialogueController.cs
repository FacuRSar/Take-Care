using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [TextArea]
    public string text;
    public float delayBefore = 0f;
    public float duration = 2.5f;

    // Audio opcional de la linea. El subtitulo manda: si el clip dura distinto a "duration",
    // se ajusta el pitch para que el audio entre justo en ese tiempo (se acelera o frena).
    // Si "duration" es 0 y hay clip, se usa el largo del clip tal cual.
    public AudioClip audioClip;
}

[Serializable]
public class DialoguePool
{
    public string id;
    [Tooltip("Si esta activo, el texto de este grupo usa el color de abajo. Si no, usa el color por defecto del subtitulo.")]
    public bool useCustomTextColor = false;
    [Tooltip("Color del TEXTO del subtítulo mientras corre este grupo (ej. muñeca vs protagonista). Solo se usa si 'Use Custom Text Color' esta activo.")]
    public Color subtitleTextColor = Color.white;
    public DialogueLine[] lines;
}

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance;

    [Header("Referencias")]
    [SerializeField] private SubtitleUI subtitleUI;
    // AudioSource para la voz de los dialogos. Si queda vacio se crea uno solo.
    [SerializeField] private AudioSource voiceSource;

    [Header("Audio")]
    // limites de pitch para que al sincronizar no quede irreconocible (chipmunk o gravedad extrema)
    [SerializeField] private float minVoicePitch = 0.5f;
    [SerializeField] private float maxVoicePitch = 2.5f;

    [Header("Dialogos")]
    [SerializeField] private DialoguePool[] pools;

    private Coroutine currentRoutine;

    public bool IsPlaying => currentRoutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (subtitleUI == null)
        {
            subtitleUI = SubtitleUI.Instance;
        }

        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
            }
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
    }

    public void PlayDialogue(string id)
    {
        DialoguePool pool = GetPool(id);

        if (pool == null || pool.lines == null || pool.lines.Length == 0)
        {
            //Debug.LogWarning("DialogueController: no se encontro dialogo o esta vacio: " + id);
            return;
        }

        // solo pasamos color custom si el pool lo pide; si no, el subtitulo usa su color por defecto
        Color? textColor = pool.useCustomTextColor ? pool.subtitleTextColor : (Color?)null;
        PlayLines(pool.lines, textColor);
    }

    public float GetDialogueDuration(string id)
    {
        DialogueLine[] lines = GetLines(id);
        return GetLinesDuration(lines);
    }

    public void StopDialogue()
    {
        StopCurrentRoutine();
    }

    private void PlayLines(DialogueLine[] lines, Color? textColor)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(PlayRoutine(lines, textColor));
    }

    private void StopCurrentRoutine()
    {
        if (currentRoutine == null)
        {
            return;
        }

        StopCoroutine(currentRoutine);
        currentRoutine = null;
        StopVoice();
    }

    private DialoguePool GetPool(string id)
    {
        if (string.IsNullOrEmpty(id) || pools == null)
        {
            return null;
        }

        foreach (DialoguePool pool in pools)
        {
            if (pool != null && pool.id == id)
            {
                return pool;
            }
        }

        return null;
    }

    private DialogueLine[] GetLines(string id)
    {
        DialoguePool pool = GetPool(id);
        return pool != null ? pool.lines : null;
    }

    private IEnumerator PlayRoutine(DialogueLine[] lines, Color? textColor)
    {
        foreach (DialogueLine line in lines)
        {
            if (line == null || string.IsNullOrEmpty(line.text))
            {
                continue;
            }

            if (line.delayBefore > 0f)
            {
                yield return new WaitForSeconds(line.delayBefore);
            }

            // la duracion del subtitulo manda; el audio (si hay) se ajusta a ella
            float lineDuration = GetLineDuration(line);
            PlayLineAudio(line, lineDuration);

            SubtitleUI targetSubtitle = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;

            if (targetSubtitle != null)
            {
                // el fondo lo dejamos por defecto (transparente segun config del panel) y solo pasamos el color del texto
                targetSubtitle.ShowSubtitle(line.text, lineDuration, SubtitlePriority.Dialogue, null, textColor);
            }
            else
            {
                //Debug.LogWarning("DialogueController: no hay SubtitleUI asignado o disponible.");
            }

            if (lineDuration > 0f)
            {
                yield return new WaitForSeconds(lineDuration);
            }
        }

        StopVoice();
        currentRoutine = null;
    }

    private void PlayLineAudio(DialogueLine line, float lineDuration)
    {
        if (voiceSource == null)
        {
            return;
        }

        // si la linea no trae audio, corto cualquier voz previa y no reproduzco nada
        if (line.audioClip == null)
        {
            StopVoice();
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = line.audioClip;

        // pitch para que el clip entre justo en lineDuration (acelera si el clip es mas largo)
        float pitch = 1f;
        if (lineDuration > 0.01f)
        {
            pitch = Mathf.Clamp(line.audioClip.length / lineDuration, minVoicePitch, maxVoicePitch);
        }

        voiceSource.pitch = pitch;
        voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
    }

    private float GetLinesDuration(DialogueLine[] lines)
    {
        if (lines == null)
        {
            return 0f;
        }

        float totalDuration = 0f;

        foreach (DialogueLine line in lines)
        {
            if (line == null || string.IsNullOrEmpty(line.text))
            {
                continue;
            }

            totalDuration += Mathf.Max(0f, line.delayBefore);
            totalDuration += GetLineDuration(line);
        }

        return totalDuration;
    }

    // Duracion real del subtitulo: la que se configura, o el largo del clip si no se puso duracion.
    private float GetLineDuration(DialogueLine line)
    {
        if (line == null)
        {
            return 0f;
        }

        if (line.duration > 0f)
        {
            return line.duration;
        }

        if (line.audioClip != null)
        {
            return line.audioClip.length;
        }

        return 0f;
    }
}
