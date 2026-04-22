using System.Collections;
using TMPro;
using UnityEngine;

// controlador simple de subtítulos para mostrar frases del protagonista o mensajes narrativos en pantalla
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance;
    // singleton simple para acceder mas tranqui desde otros scripts

    [Header("Subtitulos")]
    [SerializeField] private GameObject subtitleRoot;
    // contenedor principal del subtitulo (panel, fondo, etc.)

    [SerializeField] private TextMeshProUGUI subtitleText;
    // Texto donde aparece el mensaje

    private Coroutine currentRoutine;
    // guardo la coroutine actual para poder detenerla si entra un subtítulo nuevo antes de que termine el anterior

    private void Awake()
    {
        Instance = this;

        if (subtitleRoot != null)
        {
            subtitleRoot.SetActive(false);
        }
    }

    public void ShowSubtitle(string message, float duration = 2.5f)
    {
        // si ya había una rutina, la freno para reemplazar el subtítulo.
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        subtitleText.text = message;
        subtitleRoot.SetActive(true);

        yield return new WaitForSeconds(duration);

        subtitleRoot.SetActive(false);
        currentRoutine = null;
    }
}