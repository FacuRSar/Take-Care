using UnityEngine;

public class LightSwitch : Interactable
{
    [SerializeField] private LightGroupController lightGroup;
    [SerializeField] private Light[] luces;

    [Header("Estado del switch")]
    [SerializeField] private bool switchStartsOn;
    // define si este interruptor arranca como prendido o apagado

    [SerializeField] private bool applyInitialStateOnStart = true;
    // si esta activo, este switch aplica su estado apenas empieza la escena
    // ojo si hay dos switches apuntando al mismo grupo porque uno puede pisar al otro

    [Header("Energia")]
    [SerializeField] private bool requirePowerFlag = true;
    [SerializeField] private string powerFlagName = "power_on";
    [SerializeField] private string noPowerMessage = "no hay energia";
    [SerializeField] private float noPowerMessageDuration = 2f;

    private bool switchIsOn;

    private void Start()
    {
        switchIsOn = switchStartsOn;
        // estado interno del switch, separado de si hay energia o no

        if (applyInitialStateOnStart)
        {
            ApplySwitchState();
        }
    }

    public override void Interact(PlayerInteraction player)
    {
        if (!CanUseSwitch())
        {
            if (SubtitleUI.Instance != null)
            {
                SubtitleUI.Instance.ShowSubtitle(noPowerMessage, noPowerMessageDuration, SubtitlePriority.Environment);
            }

            return;
        }

        switchIsOn = !switchIsOn;
        // pulso el switch interno y despues aplico el estado al grupo de luces
        ApplySwitchState();
    }

    private void ApplySwitchState()
    {
        if (lightGroup != null)
        {
            lightGroup.SetLights(switchIsOn);
            // si hay grupo, mando el estado ahi y evito repetir arrays de luces en cada switch
            return;
        }
        
        if (luces == null || luces.Length == 0)
        {
            return;
        }

        foreach (Light Luz in luces)
        {
            if (Luz == null)
            {
                continue;
            }

            Luz.enabled = switchIsOn;
            // Debug.Log("Interruptor activado");
        }
    }

    private bool CanUseSwitch()
    {
        if (!requirePowerFlag)
        {
            return true;
        }

        GameStateController targetState = GameStateController.Instance;
        return targetState != null && targetState.GetFlag(powerFlagName);
    }
}
