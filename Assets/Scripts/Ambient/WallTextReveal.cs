using System.Collections;
using TMPro;
using UnityEngine;

/* Revela texto TMP con un barrido horizontal (efecto tiza / rayón en pared).
 * Lo usa AmbientEvent.SetText; se agrega solo al primer disparo si no está.
 */
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class WallTextReveal : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float defaultDuration = 4f;
    [SerializeField] private string defaultScratchSfxId = "WallScratch";
    [SerializeField] private float edgeNoise = 0.04f;
    [SerializeField] private float edgeFeather = 0.08f;

    private Coroutine revealRoutine;
    private AudioSource scratchSource;
    private float revealProgress;
    private bool isRevealing;
    private float boundsMinX;
    private float boundsWidth;
    private float maxCutoffReached;

    public float DefaultDuration => defaultDuration;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }
    }

    public void Write(string content, float duration, string scratchSfxId, bool scratch3D)
    {
        if (text == null)
        {
            return;
        }

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        isRevealing = false;
        StopScratch();

        if (string.IsNullOrEmpty(content))
        {
            text.text = string.Empty;
            text.ForceMeshUpdate();
            return;
        }

        float writeDuration = duration > 0f ? duration : defaultDuration;
        string sfxId = string.IsNullOrEmpty(scratchSfxId) ? defaultScratchSfxId : scratchSfxId;

        if (writeDuration <= 0f)
        {
            text.text = content;
            text.ForceMeshUpdate();
            SetAllVerticesVisible();
            return;
        }

        revealRoutine = StartCoroutine(RevealRoutine(content, writeDuration, sfxId, scratch3D));
    }

    private IEnumerator RevealRoutine(string content, float duration, string sfxId, bool scratch3D)
    {
        text.text = content;
        CacheTextBounds();
        maxCutoffReached = boundsMinX;
        revealProgress = 0f;
        isRevealing = true;

        StartScratch(sfxId, scratch3D);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            revealProgress = t * t * (3f - 2f * t);
            yield return null;
        }

        revealProgress = 1f;
        ApplyReveal(1f);
        isRevealing = false;
        StopScratch();
        revealRoutine = null;
    }

    private void LateUpdate()
    {
        if (!isRevealing)
        {
            return;
        }

        ApplyReveal(revealProgress);
    }

    private void CacheTextBounds()
    {
        text.ForceMeshUpdate();

        TMP_TextInfo info = text.textInfo;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        if (info != null)
        {
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = info.characterInfo[i];
                if (!charInfo.isVisible)
                {
                    continue;
                }

                minX = Mathf.Min(minX, charInfo.bottomLeft.x);
                maxX = Mathf.Max(maxX, charInfo.topRight.x);
            }
        }

        if (minX == float.MaxValue)
        {
            boundsMinX = 0f;
            boundsWidth = 1f;
            return;
        }

        boundsMinX = minX;
        boundsWidth = Mathf.Max(maxX - minX, 0.001f);
    }

    private void ApplyReveal(float progress)
    {
        TMP_TextInfo info = text.textInfo;
        if (info == null || info.characterCount == 0)
        {
            return;
        }

        if (progress >= 1f)
        {
            SetAllVerticesVisible();
            return;
        }

        float cutoff = boundsMinX + boundsWidth * progress;
        maxCutoffReached = Mathf.Max(maxCutoffReached, cutoff);
        float feather = Mathf.Max(boundsWidth * edgeFeather, 0.002f);
        Color32 baseColor = text.color;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = info.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            float charLeft = charInfo.bottomLeft.x;
            float charRight = charInfo.topRight.x;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = info.meshInfo[meshIndex].colors32;
            Vector3[] vertices = info.meshInfo[meshIndex].vertices;

            // Ya pasó el barrido: queda visible para siempre.
            if (charRight <= maxCutoffReached - feather * 0.25f)
            {
                for (int v = 0; v < 4; v++)
                {
                    colors[vertexIndex + v] = baseColor;
                }

                continue;
            }

            // Todavía no llegó el barrido.
            if (charLeft >= maxCutoffReached + feather)
            {
                for (int v = 0; v < 4; v++)
                {
                    Color32 hidden = baseColor;
                    hidden.a = 0;
                    colors[vertexIndex + v] = hidden;
                }

                continue;
            }

            // Borde activo: suavizado por vértice. Lo que queda a la izquierda del corte se mantiene.
            for (int v = 0; v < 4; v++)
            {
                float vx = vertices[vertexIndex + v].x;
                float noise = (Mathf.PerlinNoise(charLeft * 0.15f, charInfo.baseLine * 0.1f) - 0.5f) * boundsWidth * edgeNoise;
                float edge = maxCutoffReached + noise;

                Color32 c = baseColor;

                if (vx <= edge - feather)
                {
                    c.a = baseColor.a;
                }
                else if (vx >= edge)
                {
                    c.a = 0;
                }
                else
                {
                    float visibility = 1f - ((vx - (edge - feather)) / feather);
                    c.a = (byte)(baseColor.a * visibility * 255f);
                }

                colors[vertexIndex + v] = c;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetAllVerticesVisible()
    {
        TMP_TextInfo info = text.textInfo;
        if (info == null || info.characterCount == 0)
        {
            return;
        }

        Color32 baseColor = text.color;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = info.characterInfo[i];
            if (!charInfo.isVisible)
            {
                continue;
            }

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = info.meshInfo[meshIndex].colors32;

            for (int v = 0; v < 4; v++)
            {
                colors[vertexIndex + v] = baseColor;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void StartScratch(string sfxId, bool scratch3D)
    {
        if (string.IsNullOrEmpty(sfxId) || SFXManager.Instance == null)
        {
            return;
        }

        if (scratchSource == null)
        {
            scratchSource = gameObject.AddComponent<AudioSource>();
            scratchSource.playOnAwake = false;
        }

        SFXManager.Instance.ConfigureLoopingSource(scratchSource, sfxId, scratch3D);
    }

    private void StopScratch()
    {
        if (scratchSource != null && scratchSource.isPlaying)
        {
            scratchSource.Stop();
        }
    }

    private void OnDisable()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        isRevealing = false;
        StopScratch();
    }
}
