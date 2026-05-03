using System.Collections;
using UnityEngine;

// componente legacy. los dialogos iniciales ahora los dispara unicamente introsequencecontroller
public class IntroSubtitleTrigger : MonoBehaviour
{
    [Header("Subtitulo de intro")]
    [SerializeField] private string introMessage = "dios, no esperaba que se largara esa tormenta";
    // mensaje que aparecia al inicio

    [SerializeField] private float delay = 2f;
    // tiempo que esperaba antes de mostrar el subtitulo

    [SerializeField] private float duration = 2.5f;
    // tiempo que permanecia visible

    [SerializeField] private bool playLegacyIntroOnStart;
    // queda apagado por defecto para que no pise al introsequencecontroller

    private void Start()
    {
        if (playLegacyIntroOnStart)
        {
            StartCoroutine(ShowIntroSubtitle());
        }
    }

    private IEnumerator ShowIntroSubtitle()
    {
        yield return new WaitForSeconds(delay);

        if (SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(introMessage, duration);
        }
    }
}