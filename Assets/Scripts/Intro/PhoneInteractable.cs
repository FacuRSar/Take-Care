using UnityEngine;

public class PhoneInteractable : Interactable
{
    [Header("Controlador de intro")]
    [SerializeField] private IntroSequenceController introSequenceController;

    [Header("Estado")]
    [SerializeField] private bool canAnswer;

    [Header("Antes de que suene")]
    [SerializeField] private string notWorkingMessage = "No parece funcionar.";
    [SerializeField] private float notWorkingMessageDuration = 2.5f;

    private bool answered;

    public void SetCanAnswer(bool value)
    {
        canAnswer = value;
        // Debug.Log("Telefono: se puede atender = " + canAnswer);
    }

    public override void Interact(PlayerInteraction player)
    {
        // Debug.Log("Telefono: recibio interaccion");

        if (answered)
        {
            // Debug.Log("Telefono: ya fue atendido wacheeeeeeen");
            return;
        }

        if (!canAnswer)
        {
            if (SubtitleUI.Instance != null)
            {
                SubtitleUI.Instance.ShowSubtitle(notWorkingMessage, notWorkingMessageDuration);
            }

            return;
        }

        answered = true;
        // Debug.Log("Telefono: atendiendo llamada.");

        // corto el ring apenas se atiende. si no queda sonando mientras habla la estatica,
        // y ahi el telefono parece poseido por dos fantasmas a la vez
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopLoop("PhoneRing");
        }

        if (introSequenceController != null)
        {
            introSequenceController.OnPhoneAnswered();
        }
        else
        {
            //Debug.LogWarning("Telefono: no tiene IntroSequenceController asignado.");
        }
    }
}
