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
}

[Serializable]
public class DialoguePool
{
    public string id;
    [Tooltip("Color del fondo del panel de subtítulos mientras corre este grupo (ej. muñeca vs protagonista).")]
    public Color subtitleBackgroundColor = new Color(0f, 0f, 0f, 0.72f);
    public DialogueLine[] lines;
}

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance;

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
        DialoguePool pool = GetPool(id);

        if (pool == null || pool.lines == null || pool.lines.Length == 0)
        {
            Debug.LogWarning("DialogueController: no se encontro dialogo o esta vacio: " + id);
            return;
        }

        PlayLines(pool.lines, pool.subtitleBackgroundColor);
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

    private void PlayLines(DialogueLine[] lines, Color subtitleBackdrop)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(PlayRoutine(lines, subtitleBackdrop));
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

    private IEnumerator PlayRoutine(DialogueLine[] lines, Color subtitleBackdrop)
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
                targetSubtitle.ShowSubtitle(line.text, line.duration, SubtitlePriority.Dialogue, subtitleBackdrop);
            }
            else
            {
                Debug.LogWarning("DialogueController: no hay SubtitleUI asignado o disponible.");
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
