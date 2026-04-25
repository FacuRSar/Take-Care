using UnityEngine;

public class LightSwitch : Interactable
{
    private Light[] luces;

    private void Awake()
    {
        luces = GetComponentsInChildren<Light>();
    }
    public override void Interact(PlayerInteraction player)
    {
        if (luces == null || luces.Length == 0) return;

        foreach (Light Luz in luces)
        {
            Luz.enabled = !Luz.enabled;
            Debug.Log("ANgel puto");
        }
    }
}
