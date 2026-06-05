using UnityEngine;

public class LightSwitch : Interactable
{
    [SerializeField] new Light light;

    public void LightOnOff()
    {
            light.enabled = !light.enabled;
    }
}
