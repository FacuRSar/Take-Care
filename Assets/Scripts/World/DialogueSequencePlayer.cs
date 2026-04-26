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

public class DialogueSequencePlayer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SubtitleUI subtitleUI;

    [Header("Secuencia configurada")]
    [SerializeField] private DialogueLine[] configuredLines;

    private Coroutine currentRoutine;

    public bool IsPlaying => currentRoutine != null;
    public bool HasConfiguredLines => configuredLines != null && configuredLines.Length > 0;

    private void Awake()
    {
        if (subtitleUI == null)
        {
            subtitleUI = SubtitleUI.Instance;
        }
    }

    public void PlayConfiguredSequence()
    {
        PlaySequence(configuredLines);
    }

    public void PlaySequence(DialogueLine[] lines)
    {
        StopSequence();
        // corto la secuencia anterior antes de arrancar otra. si no, los subtitulos se pisan tipo charla familiar

        if (lines == null || lines.Length == 0)
        {
            return;
        }

        currentRoutine = StartCoroutine(PlayRoutine(lines));
    }

    public void StopSequence()
    {
        if (currentRoutine == null)
        {
            return;
        }

        StopCoroutine(currentRoutine);
        currentRoutine = null;
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
                targetSubtitle.ShowSubtitle(line.text, line.duration);
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
}
