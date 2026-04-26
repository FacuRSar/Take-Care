using System.Collections;
using TMPro;
using UnityEngine;

// controlador simple de subtitulos para mostrar frases del protagonista o mensajes narrativos en pantalla
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance;
    // singleton simple para acceder mas tranqui desde otros scripts

    [Header("Subtitulos")]
    [SerializeField] private GameObject subtitleRoot;
    // contenedor principal del subtitulo (panel, fondo, etc.)

    [SerializeField] private TextMeshProUGUI subtitleText;
    // texto donde aparece el mensaje

    private CanvasGroup subtitleCanvasGroup;
    private Coroutine currentRoutine;
    // guardo la coroutine actual para poder detenerla si entra un subtitulo nuevo antes de que termine el anterior

    private void Awake()
    {
        Instance = this;

        if (subtitleRoot != null)
        {
            subtitleCanvasGroup = subtitleRoot.GetComponent<CanvasGroup>();

            if (subtitleRoot == gameObject && subtitleCanvasGroup == null)
            {
                subtitleCanvasGroup = subtitleRoot.AddComponent<CanvasGroup>();
            }

            SetSubtitleVisible(false);
        }
    }

    public void ShowSubtitle(string message, float duration = 2.5f)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("SubtitleUI: no puede mostrar subtitulo porque el componente esta deshabilitado.");
            return;
        }

        // si ya habia una rutina, la freno para reemplazar el subtitulo
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        if (subtitleText != null)
        {
            subtitleText.text = message;
        }

        SetSubtitleVisible(true);

        yield return new WaitForSeconds(duration);

        SetSubtitleVisible(false);
        currentRoutine = null;
    }

    private void SetSubtitleVisible(bool visible)
    {
        if (subtitleRoot == null)
        {
            return;
        }

        if (subtitleRoot != gameObject)
        {
            // si el root es un hijo, lo prendo/apago normal
            // esto evita apagar el monobehaviour que tiene que arrancar la coroutine, sino, tremendo tiro en el pie
            subtitleRoot.SetActive(visible);
            return;
        }
    }

}
