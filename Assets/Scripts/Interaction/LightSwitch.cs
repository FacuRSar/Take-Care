using UnityEngine;

public class LightSwitch : Interactable
{
    [SerializeField] private Light[] luces;

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
