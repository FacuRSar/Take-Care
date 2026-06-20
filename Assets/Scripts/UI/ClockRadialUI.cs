using UnityEngine;
using UnityEngine.UI;

public class ClockRadialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform handPivot;

    [Header("Settings")]
    // true = el reloj arranca lleno y se vacia. Pasale tiempo transcurrido en UpdateClock.
    [SerializeField] private bool emptyOverTime = true;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private float startAngle = 0f;

    public void UpdateClock(float elapsedTime, float maxTime)
    {
        if (maxTime <= 0f)
        {
            SetProgress(0f);
            return;
        }

        float progress = elapsedTime / maxTime;
        SetProgress(progress);
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        float visualProgress = emptyOverTime ? 1f - progress : progress;

        if (fillImage != null)
            fillImage.fillAmount = visualProgress;

        if (handPivot != null)
        {
            float direction = clockwise ? -1f : 1f;
            float angle = startAngle + visualProgress * 360f * direction;

            handPivot.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    public void SetFull()
    {
        SetProgress(emptyOverTime ? 0f : 1f);
    }

    public void SetEmpty()
    {
        SetProgress(emptyOverTime ? 1f : 0f);
    }
}
