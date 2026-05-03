using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HintDialogueEntry
{
    [TextArea]
    public string text = "deberia buscar si hay alguna manera de encender la luz";
    public float duration = 3f;
    public string[] requiredFlags;
    public string[] blockedFlags;
}

public class HintDialogueController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DialogueSequencePlayer dialoguePlayer;
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private GameStateController gameStateController;

    [Header("Ayuda")]
    [SerializeField] private float firstHintDelay = 15f;
    [SerializeField] private float repeatInterval = 18f;
    [SerializeField] private HintDialogueEntry[] hintPool;

    [Header("Legacy")]
    [SerializeField] private string fallbackHintText = "deberia buscar si hay alguna manera de encender la luz";
    [SerializeField] private float fallbackHintDuration = 3f;
    [SerializeField] private bool activeOnStart;

    private Coroutine hintRoutine;

    private void Awake()
    {
        if (subtitleUI == null)
        {
            subtitleUI = SubtitleUI.Instance;
        }

        if (gameStateController == null)
        {
            gameStateController = GameStateController.Instance;
        }
    }

    private void Start()
    {
        if (activeOnStart)
        {
            StartHints();
        }
    }

    public void StartHints()
    {
        StopHintRoutine();
        hintRoutine = StartCoroutine(HintRoutine());
        Debug.Log("pistas de dialogo iniciadas");
    }

    public void StopHints()
    {
        StopHintRoutine();

        if (dialoguePlayer != null)
        {
            dialoguePlayer.StopSequence();
        }
    }

    public void ClearFlag(string flagName)
    {
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;

        if (targetState != null)
        {
            targetState.ClearFlag(flagName);
        }
    }

    public void RemoveFlag(string flagName)
    {
        ClearFlag(flagName);
    }

    private void StopHintRoutine()
    {
        // solo freno el timer de pistas. no corto el subtitulo actual salvo que stophints lo pida
        if (hintRoutine == null)
        {
            return;
        }

        StopCoroutine(hintRoutine);
        hintRoutine = null;
    }

    private IEnumerator HintRoutine()
    {
        yield return new WaitForSeconds(firstHintDelay);

        while (true)
        {
            ShowRandomHint();
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    private void ShowRandomHint()
    {
        HintDialogueEntry hint = GetRandomAvailableHint();

        if (hint != null)
        {
            ShowHint(hint.text, hint.duration);
            return;
        }

        ShowHint(fallbackHintText, fallbackHintDuration);
    }

    private HintDialogueEntry GetRandomAvailableHint()
    {
        if (hintPool == null || hintPool.Length == 0)
        {
            return null;
        }

        List<HintDialogueEntry> availableHints = new List<HintDialogueEntry>();

        foreach (HintDialogueEntry hint in hintPool)
        {
            if (hint == null || string.IsNullOrEmpty(hint.text))
            {
                continue;
            }

            if (CanUseHint(hint))
            {
                availableHints.Add(hint);
            }
        }

        if (availableHints.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableHints.Count);
        return availableHints[randomIndex];
    }

    private bool CanUseHint(HintDialogueEntry hint)
    {
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;

        if (targetState == null)
        {
            return true;
        }

        if (hint.requiredFlags != null)
        {
            foreach (string flag in hint.requiredFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !targetState.GetFlag(flag))
                {
                    return false;
                }
            }
        }

        if (hint.blockedFlags != null)
        {
            foreach (string flag in hint.blockedFlags)
            {
                if (!string.IsNullOrEmpty(flag) && targetState.GetFlag(flag))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ShowHint(string text, float duration)
    {
        if (dialoguePlayer != null)
        {
            DialogueLine[] lines =
            {
                new DialogueLine
                {
                    text = text,
                    duration = duration
                }
            };

            dialoguePlayer.PlaySequence(lines);
            return;
        }

        SubtitleUI targetSubtitle = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;

        if (targetSubtitle != null)
        {
            targetSubtitle.ShowSubtitle(text, duration);
        }
        else
        {
            Debug.LogWarning("hintdialoguecontroller: no hay dialoguesequenceplayer ni subtitleui disponible");
        }
    }
}
