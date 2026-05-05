using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [TextArea]
    public string text;
    // delay antes de mostrar esta linea, para separar frases sin armar mil coroutines a mano
    public float delayBefore = 0f;
    public float duration = 2.5f;
}

[Serializable]
public class DialoguePool
{
    public string id;
    public DialogueLine[] lines;
}

public class DialogueSequencePlayer : MonoBehaviour
{
    public static DialogueSequencePlayer Instance;

    [Header("Referencias")]
    [SerializeField] private SubtitleUI subtitleUI;

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
    }

    public void PlayDialogue(string id)
    {
        DialogueLine[] lines = GetLines(id);

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("dialoguesequenceplayer: no se encontro dialogo o esta vacio: " + id);
            return;
        }

        PlayLines(lines);
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

    private void PlayLines(DialogueLine[] lines)
    {
        StopCurrentRoutine();
        // corto la secuencia anterior antes de arrancar otra. si no, los subtitulos se pisan tipo charla familiar

        currentRoutine = StartCoroutine(PlayRoutine(lines));
    }

    private void StopCurrentRoutine()
    {
        if (currentRoutine == null)
        {
            return;
        }

        StopCoroutine(currentRoutine);
        currentRoutine = null;
    }

    private DialogueLine[] GetLines(string id)
    {
        if (string.IsNullOrEmpty(id) || pools == null)
        {
            return null;
        }

        foreach (DialoguePool pool in pools)
        {
            if (pool != null && pool.id == id)
            {
                return pool.lines;
            }
        }

        return null;
    }

    private IEnumerator PlayRoutine(DialogueLine[] lines)
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

            SubtitleUI targetSubtitle = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;

            if (targetSubtitle != null)
            {
                targetSubtitle.ShowSubtitle(line.text, line.duration, SubtitlePriority.Dialogue);
            }
            else
            {
                Debug.LogWarning("DialogueSequencePlayer: no hay SubtitleUI asignado o disponible.");
            }

            if (line.duration > 0f)
            {
                yield return new WaitForSeconds(line.duration);
            }
        }

        currentRoutine = null;
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
            totalDuration += Mathf.Max(0f, line.duration);
        }

        return totalDuration;
    }
}
