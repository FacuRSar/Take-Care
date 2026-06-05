using UnityEngine;

public class FaucetInteractable : Interactable
{
    [Header("Controlador de intro")]
    [SerializeField] private IntroSequenceController introSequenceController;

    private bool closed;

    public override void Interact(PlayerInteraction player)
    {
        if (closed)
        {
            return;
        }

        closed = true;
        // la canilla se cierra una sola vez. si no para que no te quedes boludeando con la canillita
        // Deja de distraerte con comentarios de mierda

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopLoop("FaucetLoop");
        }

        if (introSequenceController != null)
        {
            introSequenceController.OnFaucetClosed();
        }
        else
        {
            Debug.LogWarning("Canilla: no tiene IntroSequenceController asignado.");
        }
    }
}
