using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* Secuencia de transicion al apretar "Jugar" en el menu.
 *
 * Flujo:
 *  1. Las luces titilan varias veces (fuerte: se apagan 100% y se suma el overlay oscuro).
 *  2. Ultimo apagado: suena la risa con la pantalla oscura. Cuando termina la risa,
 *     simultaneamente vuelve la luz y se hace el swap de texturas a la version creepy.
 *  3. Despues de unos segundos viendo la habitacion cambiada, fade a negro total.
 *     Pantalla negra en silencio, despues suena el portazo y al terminar carga la Intro.
 *
 * El Menu3DButtonsController llama a Run() en lugar de cargar la escena directamente.
 */
public class MenuPlaySequence : MonoBehaviour
{
    [System.Serializable]
    public class TextureSwap
    {
        public Renderer renderer;
        // Indice del slot de material en el renderer. 0 si tiene un solo material.
        public int materialIndex = 0;
        public Texture creepyTexture;
    }

    [System.Serializable]
    public class ColorSwap
    {
        public Renderer renderer;
        // Indice del slot de material en el renderer. 0 si tiene un solo material.
        public int materialIndex = 0;
        public Color creepyColor = Color.white;
    }

    [Header("Luces a controlar")]
    [SerializeField] private Light[] lights;

    [Header("Swap de texturas")]
    [SerializeField] private TextureSwap[] textureSwaps;

    [Header("Swap de colores")]
    [SerializeField] private ColorSwap[] colorSwaps;

    [Header("Fade / Oscurecimiento")]
    [SerializeField] private Image fadeImage;
    // Alpha (0-1) que se aplica cuando la luz esta "apagada" durante la secuencia.
    // 240/255 = 0.941.
    [SerializeField, Range(0f, 1f)] private float darkOverlayAlpha = 240f / 255f;

    [Header("Sonidos (SFXManager ids)")]
    [SerializeField] private string laughSfxId = "menu_laugh";
    [SerializeField] private string doorSlamSfxId = "menu_door_slam";
    // Duracion a usar si el SFX todavia no esta registrado en el SFXManager.
    [SerializeField] private float fallbackLaughDuration = 1.5f;
    [SerializeField] private float fallbackDoorSlamDuration = 1f;

    [Header("Escena destino")]
    [SerializeField] private string introSceneName = "Intro";

    [Header("Etapa 1: titileo fuerte")]
    [SerializeField, Min(1)] private int flickerCycles = 4;
    [SerializeField] private float flickerOffMin = 0.04f;
    [SerializeField] private float flickerOffMax = 0.14f;
    [SerializeField] private float flickerOnMin = 0.06f;
    [SerializeField] private float flickerOnMax = 0.12f;

    [Header("Etapa 2: luz on tras risa y swap")]
    // Tiempo que se ve la habitacion con las texturas creepy antes de empezar el fade.
    [SerializeField] private float waitWithCreepyView = 1.5f;

    [Header("Etapa 3: blackout y portazo")]
    [SerializeField] private float finalFadeDuration = 0.25f;
    // Cuanto se queda la pantalla negra en silencio antes de disparar el portazo.
    [SerializeField] private float blackHoldBeforeDoor = 1f;

    private float[] originalIntensities;
    private bool running;

    public bool IsRunning => running;

    private void Awake()
    {
        CacheOriginalIntensities();

        SetOverlayAlpha(0f);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void Run()
    {
        if (running)
        {
            return;
        }

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        running = true;

        if (AmbientManager.Instance != null)
        {
            // Cortamos los rayos para que no interfieran con el titileo.
            AmbientManager.Instance.StopThunderLoop();
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
        }

        // Etapa 1: titileo. Cada off prende el overlay oscuro para que se note el apagon.
        for (int i = 0; i < flickerCycles; i++)
        {
            SetDark(true);
            yield return new WaitForSecondsRealtime(Random.Range(flickerOffMin, flickerOffMax));

            SetDark(false);
            yield return new WaitForSecondsRealtime(Random.Range(flickerOnMin, flickerOnMax));
        }

        // Etapa 2: ultima vez que se apaga la luz. Apenas se apaga suena la risa.
        SetDark(true);

        AudioClip laughClip = PlaySfx(laughSfxId);
        float laughDuration = laughClip != null ? laughClip.length : fallbackLaughDuration;
        yield return new WaitForSecondsRealtime(laughDuration);

        // La risa termino: luz ON y swap de texturas/colores simultaneo.
        ApplyTextureSwaps();
        ApplyColorSwaps();
        SetDark(false);

        yield return new WaitForSecondsRealtime(waitWithCreepyView);

        // Etapa 3: fade a negro completo.
        yield return StartCoroutine(FadeOverlayAlpha(0f, 1f, finalFadeDuration));

        // Pantalla negra en silencio antes del portazo.
        yield return new WaitForSecondsRealtime(blackHoldBeforeDoor);

        // Portazo: esperamos a que termine antes de cargar.
        AudioClip doorClip = PlaySfx(doorSlamSfxId);
        float doorDuration = doorClip != null ? doorClip.length : fallbackDoorSlamDuration;
        yield return new WaitForSecondsRealtime(doorDuration);

        LoadIntroScene();
    }

    // Apaga/prende las luces y ajusta el overlay al alpha correspondiente.
    private void SetDark(bool dark)
    {
        SetLightsOn(!dark);
        SetOverlayAlpha(dark ? darkOverlayAlpha : 0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(alpha);

        // Si vamos a mostrar overlay y por algun motivo el GO esta apagado, lo reactivamos.
        if (clamped > 0f && !fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }

        Color c = fadeImage.color;
        c.a = clamped;
        fadeImage.color = c;
    }

    private IEnumerator FadeOverlayAlpha(float from, float to, float duration)
    {
        if (fadeImage == null || duration <= 0f)
        {
            SetOverlayAlpha(to);
            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetOverlayAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetOverlayAlpha(to);
    }

    private void CacheOriginalIntensities()
    {
        if (lights == null)
        {
            return;
        }

        originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                originalIntensities[i] = lights[i].intensity;
            }
        }
    }

    private void SetLightsOn(bool on)
    {
        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            light.enabled = on;
            light.intensity = on ? originalIntensities[i] : 0f;
        }
    }

    private void ApplyTextureSwaps()
    {
        if (textureSwaps == null)
        {
            return;
        }

        foreach (TextureSwap swap in textureSwaps)
        {
            if (swap == null || swap.renderer == null || swap.creepyTexture == null)
            {
                continue;
            }

            Material[] mats = swap.renderer.materials;
            int index = Mathf.Clamp(swap.materialIndex, 0, mats.Length - 1);

            mats[index].SetTexture("_BaseMap", swap.creepyTexture);
            swap.renderer.materials = mats;
        }
    }

    private void ApplyColorSwaps()
    {
        if (colorSwaps == null)
        {
            return;
        }

        foreach (ColorSwap swap in colorSwaps)
        {
            if (swap == null || swap.renderer == null)
            {
                continue;
            }

            Material[] mats = swap.renderer.materials;
            int index = Mathf.Clamp(swap.materialIndex, 0, mats.Length - 1);

            mats[index].SetColor("_BaseColor", swap.creepyColor);
            swap.renderer.materials = mats;
        }
    }

    private AudioClip PlaySfx(string id)
    {
        if (string.IsNullOrEmpty(id) || SFXManager.Instance == null)
        {
            return null;
        }

        return SFXManager.Instance.Play2D(id);
    }

    private void LoadIntroScene()
    {
        running = false;

        if (string.IsNullOrEmpty(introSceneName))
        {
            Debug.LogWarning("[MenuPlaySequence] introSceneName vacio.");
            return;
        }

        if (GameController.Instance != null)
        {
            GameController.Instance.GoToScene(introSceneName);
        }
        else
        {
            SceneManager.LoadScene(introSceneName);
        }
    }
}
