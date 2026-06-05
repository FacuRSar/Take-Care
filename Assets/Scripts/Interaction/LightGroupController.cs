using UnityEngine;

public class LightGroupController : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private bool collectChildLightsOnAwake = true;

    [Header("Energia")]
    [SerializeField] private bool requirePowerFlag = true;
    [SerializeField] private string powerFlagName = "power_on";

    [Header("Estado inicial")]
    [SerializeField] private bool switchStartsOn;

    private bool switchIsOn;

    private void Awake()
    {
        if (collectChildLightsOnAwake && (lights == null || lights.Length == 0))
        {
            lights = GetComponentsInChildren<Light>(true);
        }

        switchIsOn = switchStartsOn;
    }

    private void OnEnable()
    {
        GameStateController.OnFlagChanged += HandleFlagChanged;
    }

    private void Start()
    {
        ApplyLightState();
    }

    private void OnDisable()
    {
        GameStateController.OnFlagChanged -= HandleFlagChanged;
    }

    public void ToggleLights()
    {
        switchIsOn = !switchIsOn;
        ApplyLightState();
    }

    public void SetLights(bool enabled)
    {
        switchIsOn = enabled;
        ApplyLightState();
    }

    public bool CanReceivePower()
    {
        if (!requirePowerFlag)
        {
            return true;
        }

        GameStateController targetState = GameStateController.Instance;
        return targetState != null && targetState.GetFlag(powerFlagName);
    }

    private void ApplyLightState()
    {
        bool shouldBeOn = switchIsOn && CanReceivePower();

        if (lights == null)
        {
            return;
        }

        foreach (Light lightSource in lights)
        {
            if (lightSource == null)
            {
                continue;
            }

            lightSource.enabled = shouldBeOn;
        }
    }

    private void HandleFlagChanged(string flagName, bool value)
    {
        if (flagName == powerFlagName)
        {
            ApplyLightState();
        }
    }

    public bool HasAnyLightOn()
    {
        if (lights == null)
        {
            return false;
        }

        foreach (Light lightSource in lights)
        {
            if (lightSource != null && lightSource.enabled)
            {
                return true;
            }
        }

        return false;
    }
}
