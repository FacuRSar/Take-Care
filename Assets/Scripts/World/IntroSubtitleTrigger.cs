using System.Collections;
using UnityEngine;

//muestra un subtítulo automático al poco tiempo de arrancar la partida
public class IntroSubtitleTrigger : MonoBehaviour
{
    [Header("Subtitulo de intro")]
    [SerializeField] private string introMessage = "Dios, no esperaba que se largara esa tormenta";
    //mensaje que aparece al inicio

    [SerializeField] private float delay = 2f;
    //tiempo que espera antes de mostrar el subtítulo

    [SerializeField] private float duration = 2.5f;
    //tiempo que permanece visible

    private void Start()
    {
        StartCoroutine(ShowIntroSubtitle());
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