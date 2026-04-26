using System.Collections;
using UnityEngine;

public class HintDialogueController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DialogueSequencePlayer dialoguePlayer;
    [SerializeField] private SubtitleUI subtitleUI;

    [Header("Ayuda")]
    [SerializeField] private string hintText = "Deberia buscar si hay alguna manera de encender la luz.";
    [SerializeField] private float firstHintDelay = 15f;
    [SerializeField] private float repeatInterval = 18f;
    [SerializeField] private float hintDuration = 3f;
    [SerializeField] private bool activeOnStart;

    private Coroutine hintRoutine;

    private void Awake()
    {
        if (subtitleUI == null)
        {
            subtitleUI = SubtitleUI.Instance;
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
        Debug.Log("Pistas de dialogo iniciadas.");
    }

    public void StopHints()
    {
        StopHintRoutine();

        if (dialoguePlayer != null)
        {
            dialoguePlayer.StopSequence();
        }
    }

    private void StopHintRoutine()
    {
        // solo freno el timer de pistas. no corto el subtitulo actual salvo que stophints lo pida,
        // porque si no la intro se autoboicotea, no me saltaba un carajo los subtitulos de intro por esto
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
            ShowHint();
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    private void ShowHint()
    {
        if (dialoguePlayer != null)
        {
            DialogueLine[] lines =
            {
                new DialogueLine
                {
                    text = hintText,
                    duration = hintDuration
                }
            };

            dialoguePlayer.PlaySequence(lines);
            return;
        }

        SubtitleUI targetSubtitle = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;

        if (targetSubtitle != null)
        {
            targetSubtitle.ShowSubtitle(hintText, hintDuration);
        }
        else
        {
            Debug.LogWarning("HintDialogueController: no hay DialogueSequencePlayer ni SubtitleUI disponible.");
        }
    }
}
