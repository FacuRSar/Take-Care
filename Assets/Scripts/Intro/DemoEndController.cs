using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoEndController : MonoBehaviour
{
    [Header("Visuales")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI finalMessageText;
    [TextArea]
    [SerializeField] private string finalMessage = "Fin de la demo";

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float messageDelay = 0.5f;
    [SerializeField] private float sceneLoadDelay = 2f;

    [Header("Audio")]
    [SerializeField] private string finalSfxId = "FinalSting";

    [Header("Escena")]
    [SerializeField] private string endSceneName;

    [Header("Efectos opcionales")]
    // [SerializeField] private CameraFallEffect cameraFallEffect;
    // QUIZAS y solo quizas, meto algo asi.

    [Header("Cierre por captura")]
    // El mensaje se muestra con el manager de dialogos/subtitulos por id (DialogueController).
    [SerializeField] private string captureMessageDialogueId = "captureMessege";
    // risa del perseguidor al capturar
    [SerializeField] private string captureLaughSfxId = "PursuerLaugh";
    // sonido que suena junto con el subtitulo. Vacio = sin sonido.
    [SerializeField] private string captureMessageSfxId = "";
    // efecto opcional de manos cerrandose sobre los ojos (id en ScreenEffectController). Vacio = sin efecto.
    [SerializeField] private string handsEffectId = "hands";
    // vignette opcional que "cierra" la pantalla en negro (id en ScreenEffectController). Vacio = sin vignette.
    // reutilizamos el mismo efecto de fatiga.
    [SerializeField] private string captureVignetteEffectId = "fatigue";
    // margen para que las manos/vignette se vean antes de que arranque el fade a negro
    [SerializeField] private float handsLeadTime = 0.6f;
    // cuanto se mantiene el negro antes de cambiar de escena / mostrar mensaje final
    [SerializeField] private float blackHoldDuration = 2f;

    private bool endingStarted;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(true);
        }

        if (finalMessageText != null)
        {
            finalMessageText.gameObject.SetActive(false);
        }
    }

    public void StartDemoEnd()
    {
        if (endingStarted)
        {
            return;
        }

        endingStarted = true;
        // arranca una sola vez
        StartCoroutine(DemoEndRoutine());
    }

    // Cierre cuando el perseguidor atrapa al jugador (igual de frente o de espalda).
    public void StartCaptureEnd()
    {
        if (endingStarted)
        {
            Debug.Log("DemoEndController: StartCaptureEnd ignorado (el cierre ya habia arrancado).");
            return;
        }

        endingStarted = true;
        StartCoroutine(CaptureEndRoutine());
    }

    private IEnumerator CaptureEndRoutine()
    {
        Debug.Log("DemoEndController: cierre por captura iniciado.");

        // manos cerrandose sobre los ojos (opcional)
        if (ScreenEffectController.Instance != null && !string.IsNullOrEmpty(handsEffectId))
        {
            ScreenEffectController.Instance.PlayEffect(handsEffectId);
        }
        else if (!string.IsNullOrEmpty(handsEffectId))
        {
            Debug.LogWarning("DemoEndController: no hay ScreenEffectController.Instance para las manos.");
        }

        // vignette que cierra la pantalla (opcional)
        if (ScreenEffectController.Instance != null && !string.IsNullOrEmpty(captureVignetteEffectId))
        {
            ScreenEffectController.Instance.PlayEffect(captureVignetteEffectId);
        }

        // risa del perseguidor
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(captureLaughSfxId))
        {
            SFXManager.Instance.Play2D(captureLaughSfxId);
        }

        // subtitulo dramatico a traves del manager de dialogos/subtitulos
        if (DialogueController.Instance != null && !string.IsNullOrEmpty(captureMessageDialogueId))
        {
            DialogueController.Instance.PlayDialogue(captureMessageDialogueId);
        }

        // sonido que acompaña al subtitulo
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(captureMessageSfxId))
        {
            SFXManager.Instance.Play2D(captureMessageSfxId);
        }

        // damos un margen para que las manos/vignette se vean antes de tapar con negro
        if (handsLeadTime > 0f)
        {
            yield return new WaitForSeconds(handsLeadTime);
        }

        yield return FadeToBlack();

        // el negro se mantiene un rato
        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        // si hay escena, cambia; si no, queda el fadeImage con el mensaje final
        if (!string.IsNullOrWhiteSpace(endSceneName))
        {
            SceneManager.LoadScene(endSceneName);
        }
        else
        {
            if (finalMessageText != null)
            {
                finalMessageText.text = finalMessage;
                finalMessageText.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator DemoEndRoutine()
    {
        // Debug.Log("DemoEndController: cierre de demo iniciado.");

        //if (cameraFallEffect != null)
        //{
        //    cameraFallEffect.Play();
       // }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.Play2D(finalSfxId);
        }

        yield return FadeToBlack();

        if (messageDelay > 0f)
        {
            yield return new WaitForSeconds(messageDelay);
        }

        if (finalMessageText != null)
        {
            finalMessageText.text = finalMessage;
            finalMessageText.gameObject.SetActive(true);
        }

        if (sceneLoadDelay > 0f)
        {
            yield return new WaitForSeconds(sceneLoadDelay);
        }

        if (!string.IsNullOrWhiteSpace(endSceneName))
        {
            SceneManager.LoadScene(endSceneName);
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            yield break;
        }

        // fade manual simple. No habia presupuesto para mas
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }
    }
}
