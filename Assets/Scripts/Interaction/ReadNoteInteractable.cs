using System.Collections;
using UnityEngine;

// interactable específico para una nota, lo mando separado por ser el que inicia todo y porque puede que despues se cambie asi no rompe nada
public class ReadNoteInteractable : Interactable
{
    [Header("Configuracion de nota")]
    [TextArea]
    [SerializeField] private string[] noteLines;
    // las lineas para llamar a los subtitulos

    [SerializeField] private string alreadyReadMessage = "Ya leí esta nota...";


    private bool alreadyRead;
    // evita que la nota dispare la secuencia completa infinitas veces y te sirve para otras cosas tambien

    [Header("Audio")]
    [SerializeField] private AudioSource noteAudioSource;
    // AudioSource asociado a la nota para confirmar que funca

    [SerializeField] private AudioClip readNoteClip;


    private FixedCameraWithZoom cameraScript;

    private void Awake()
    {
        cameraScript = GetComponent<FixedCameraWithZoom>();
    }

    public override void Interact(PlayerInteraction player)
    {
        cameraScript.active = true;
        if (alreadyRead)
        {
            // si ya fue leída, mostramos una respuesta más simple
            SubtitleUI.Instance.ShowSubtitle(alreadyReadMessage, 2f);
            return;
        }

        alreadyRead = true;

        // reproducimos sonido de lectura solo la primera vez
        PlayReadSound();

        // damos por activado el game, el jugador ya se la mando
        GameStateController.Instance.ActivateIntro();

        // disparamos secuencia de lectura
        StartCoroutine(ReadSequence());
    }

    private IEnumerator ReadSequence()
    {
        if (noteLines == null || noteLines.Length == 0)
        {
            yield break;
        }

        foreach (string line in noteLines)
        {
            SubtitleUI.Instance.ShowSubtitle(line, 3f);
            yield return new WaitForSeconds(3.1f);
        }
    }

    private void PlayReadSound()
    {
        // Si no hay audio configurado, no rompe nada
        if (noteAudioSource == null || readNoteClip == null)
        {
            return;
        }

        noteAudioSource.PlayOneShot(readNoteClip);
    }
}