using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoEndController : MonoBehaviour
{
    [Header("Visuales")]
    [SerializeField] private Image fadeImage;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("Escena")]
    [SerializeField] private string endSceneName;

    [Header("Cierre por captura")]
    // El mensaje se muestra con el manager de dialogos/subtitulos por id (DialogueController).
    [SerializeField] private string captureMessageDialogueId = "captureMessege";
    // risa del perseguidor al capturar
    [SerializeField] private string captureLaughSfxId = "PursuerLaugh";
    // frase/pista de audio que suena al ser atrapado. Vacio = sin sonido.
    [SerializeField] private string endPhraseSfxId = "endPhrase";
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

        // frase/pista de audio al ser atrapado
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(endPhraseSfxId))
        {
            SFXManager.Instance.Play2D(endPhraseSfxId);
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

        // si hay escena, cambia; si no, queda en negro.
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
