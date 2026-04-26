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
    [SerializeField] private CameraFallEffect cameraFallEffect;
    // QUIZAS y solo quizas, meto algo asi.

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

    private IEnumerator DemoEndRoutine()
    {
        // Debug.Log("DemoEndController: cierre de demo iniciado.");

        if (cameraFallEffect != null)
        {
            cameraFallEffect.Play();
        }

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
