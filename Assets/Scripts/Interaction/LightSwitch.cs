using UnityEngine;

public class LightSwitch : Interactable
{
    [SerializeField] private Light[] luces;
    [SerializeField] private IntroSequenceController introSequenceController;

    private bool energyNotified;

    public override void Interact(PlayerInteraction player)
    {
        if (luces == null || luces.Length == 0) return;

        foreach (Light Luz in luces)
        {
            Luz.enabled = !Luz.enabled;
            // Debug.Log("Interruptor activado");
        }

        if (!energyNotified && introSequenceController != null)
        {
            // aviso una sola vez a la intro. el switch puede seguir prendiendo/apagando,
            // pero sino se traba todo
            energyNotified = true;
            introSequenceController.OnEnergyRestored();
        }
    }
}
