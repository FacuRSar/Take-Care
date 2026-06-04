using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/* Controller generico de efectos de pantalla.
 * Sirve para overlays UI, Vignette de URP y animaciones simples.
 *
 * Uso:
 *   ScreenEffectController.Instance.PlayEffect("fatigue");
 *   ScreenEffectController.Instance.StopEffect("fatigue");
 *   ScreenEffectController.Instance.PlayEffect("hands", 0.5f);
 */
public class ScreenEffectController : MonoBehaviour
{
    public static ScreenEffectController Instance;

    public enum ScreenEffectType
    {
        ImageAlpha,
        Vignette,
        Animator
    }

    [System.Serializable]
    public class ScreenEffect
    {
        public string id;
        public ScreenEffectType type;

        [Header("Comun")]
        public bool startHidden = true;
        public bool useUnscaledTime = false;
        public float fadeInDuration = 1f;
        public float fadeOutDuration = 1f;

        [Header("Image Alpha")]
        public Image image;
        [Range(0f, 1f)] public float targetAlpha = 1f;

        [Header("URP Vignette")]
        public Volume volume;
        [Range(0f, 1f)] public float targetVignetteIntensity = 0.55f;
        [Range(0f, 1f)] public float targetVignetteSmoothness = 0.5f;

        [Header("Animator")]
        public Animator animator;
        public string playTrigger = "Play";
        public string stopTrigger = "Stop";
        public GameObject animatorRoot;
    }

    [SerializeField] private ScreenEffect[] effects;

    private readonly Dictionary<string, Coroutine> activeRoutines = new Dictionary<string, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        InitializeEffects();
    }

    public void PlayEffect(string id)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect != null)
        {
            PlayEffect(effect, effect.fadeInDuration);
        }
    }

    public void PlayEffect(string id, float customFadeDuration)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect != null)
        {
            PlayEffect(effect, customFadeDuration);
        }
    }

    public void StopEffect(string id)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect != null)
        {
            StopEffect(effect, effect.fadeOutDuration);
        }
    }

    public void StopEffect(string id, float customFadeDuration)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect != null)
        {
            StopEffect(effect, customFadeDuration);
        }
    }

    // Setea la intensidad de la vignette de un efecto por id y la aplica al instante.
    public void SetVignetteIntensity(string id, float intensity)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect == null || effect.type != ScreenEffectType.Vignette)
        {
            return;
        }

        effect.targetVignetteIntensity = Mathf.Clamp01(intensity);
        SetVignette(effect, effect.targetVignetteIntensity, effect.targetVignetteSmoothness);
    }

    // Setea el smoothness de la vignette de un efecto por id y lo aplica al instante.
    public void SetVignetteSmoothness(string id, float smoothness)
    {
        ScreenEffect effect = GetEffect(id);
        if (effect == null || effect.type != ScreenEffectType.Vignette)
        {
            return;
        }

        effect.targetVignetteSmoothness = Mathf.Clamp01(smoothness);
        SetVignette(effect, GetVignetteIntensity(effect), effect.targetVignetteSmoothness);
    }

    public void StopAll()
    {
        if (effects == null)
        {
            return;
        }

        foreach (ScreenEffect effect in effects)
        {
            if (effect != null)
            {
                StopEffect(effect, effect.fadeOutDuration);
            }
        }
    }

    private void InitializeEffects()
    {
        if (effects == null)
        {
            return;
        }

        foreach (ScreenEffect effect in effects)
        {
            if (effect == null || !effect.startHidden)
            {
                continue;
            }

            switch (effect.type)
            {
                case ScreenEffectType.ImageAlpha:
                    SetImageAlpha(effect, 0f);
                    if (effect.image != null)
                    {
                        effect.image.gameObject.SetActive(false);
                    }
                    break;

                case ScreenEffectType.Vignette:
                    SetVignette(effect, 0f, effect.targetVignetteSmoothness);
                    break;

                case ScreenEffectType.Animator:
                    GameObject root = GetAnimatorRoot(effect);
                    if (root != null)
                    {
                        root.SetActive(false);
                    }
                    break;
            }
        }
    }

    private void PlayEffect(ScreenEffect effect, float duration)
    {
        StopActiveRoutine(effect.id);

        switch (effect.type)
        {
            case ScreenEffectType.ImageAlpha:
                if (effect.image != null)
                {
                    effect.image.gameObject.SetActive(true);

                    // si el padre (panel/canvas) esta desactivado, la imagen no se ve aunque la prendamos
                    if (!effect.image.isActiveAndEnabled)
                    {
                        Debug.LogWarning("[ScreenEffectController] El efecto '" + effect.id +
                            "' tiene la Image asignada pero no queda activa en jerarquia. Revisa que su panel/Canvas padre este activo.");
                    }

                    activeRoutines[effect.id] = StartCoroutine(ImageFadeRoutine(effect, effect.image.color.a, effect.targetAlpha, duration));
                }
                else
                {
                    Debug.LogWarning("[ScreenEffectController] El efecto '" + effect.id +
                        "' es de tipo ImageAlpha pero no tiene ninguna Image asignada.");
                }
                break;

            case ScreenEffectType.Vignette:
                activeRoutines[effect.id] = StartCoroutine(VignetteFadeRoutine(effect, GetVignetteIntensity(effect), effect.targetVignetteIntensity, duration));
                break;

            case ScreenEffectType.Animator:
                PlayAnimator(effect);
                break;
        }
    }

    private void StopEffect(ScreenEffect effect, float duration)
    {
        StopActiveRoutine(effect.id);

        switch (effect.type)
        {
            case ScreenEffectType.ImageAlpha:
                if (effect.image != null)
                {
                    activeRoutines[effect.id] = StartCoroutine(ImageFadeRoutine(effect, effect.image.color.a, 0f, duration, () =>
                    {
                        effect.image.gameObject.SetActive(false);
                    }));
                }
                break;

            case ScreenEffectType.Vignette:
                activeRoutines[effect.id] = StartCoroutine(VignetteFadeRoutine(effect, GetVignetteIntensity(effect), 0f, duration));
                break;

            case ScreenEffectType.Animator:
                StopAnimator(effect);
                break;
        }
    }

    private IEnumerator ImageFadeRoutine(ScreenEffect effect, float from, float to, float duration, System.Action onComplete = null)
    {
        if (duration <= 0f)
        {
            SetImageAlpha(effect, to);
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime(effect);
            SetImageAlpha(effect, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetImageAlpha(effect, to);
        activeRoutines.Remove(effect.id);
        onComplete?.Invoke();
    }

    private IEnumerator VignetteFadeRoutine(ScreenEffect effect, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetVignette(effect, to, effect.targetVignetteSmoothness);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime(effect);
            SetVignette(effect, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)), effect.targetVignetteSmoothness);
            yield return null;
        }

        SetVignette(effect, to, effect.targetVignetteSmoothness);
        activeRoutines.Remove(effect.id);
    }

    private void SetImageAlpha(ScreenEffect effect, float alpha)
    {
        if (effect.image == null)
        {
            return;
        }

        Color color = effect.image.color;
        color.a = Mathf.Clamp01(alpha);
        effect.image.color = color;
    }

    private void SetVignette(ScreenEffect effect, float intensity, float smoothness)
    {
        if (!TryGetVignette(effect, out Vignette vignette))
        {
            return;
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = Mathf.Clamp01(intensity);
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = Mathf.Clamp01(smoothness);
    }

    private float GetVignetteIntensity(ScreenEffect effect)
    {
        return TryGetVignette(effect, out Vignette vignette) ? vignette.intensity.value : 0f;
    }

    private bool TryGetVignette(ScreenEffect effect, out Vignette vignette)
    {
        vignette = null;

        if (effect.volume == null)
        {
            return false;
        }

        VolumeProfile profile = effect.volume.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            effect.volume.profile = profile;
        }

        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }

        return vignette != null;
    }

    private void PlayAnimator(ScreenEffect effect)
    {
        GameObject root = GetAnimatorRoot(effect);
        if (root != null)
        {
            root.SetActive(true);
        }

        if (effect.animator != null && !string.IsNullOrEmpty(effect.playTrigger))
        {
            effect.animator.ResetTrigger(effect.stopTrigger);
            effect.animator.SetTrigger(effect.playTrigger);
        }
    }

    private void StopAnimator(ScreenEffect effect)
    {
        if (effect.animator != null && !string.IsNullOrEmpty(effect.stopTrigger))
        {
            effect.animator.ResetTrigger(effect.playTrigger);
            effect.animator.SetTrigger(effect.stopTrigger);
        }
        else
        {
            GameObject root = GetAnimatorRoot(effect);
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    private GameObject GetAnimatorRoot(ScreenEffect effect)
    {
        if (effect.animatorRoot != null)
        {
            return effect.animatorRoot;
        }

        return effect.animator != null ? effect.animator.gameObject : null;
    }

    private ScreenEffect GetEffect(string id)
    {
        if (string.IsNullOrEmpty(id) || effects == null)
        {
            return null;
        }

        foreach (ScreenEffect effect in effects)
        {
            if (effect != null && effect.id == id)
            {
                return effect;
            }
        }

        Debug.LogWarning("[ScreenEffectController] No existe efecto con id: " + id);
        return null;
    }

    private void StopActiveRoutine(string id)
    {
        if (activeRoutines.TryGetValue(id, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
        }

        activeRoutines.Remove(id);
    }

    private static float GetDeltaTime(ScreenEffect effect)
    {
        return effect.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
