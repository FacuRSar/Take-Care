using UnityEngine;

public class MirrorMessageController : MonoBehaviour
{
    [Header("Visuales")]
    [SerializeField] private GameObject[] messageObjects;
    [SerializeField] private bool hideOnStart = true;

    private bool visible;

    private void Start()
    {
        if (hideOnStart)
        {
            HideMessage();
        }
    }

    public void ShowMessage()
    {
        SetVisible(true);
        // Debug.Log("MirrorMessageController: mensaje del espejo mostrado.");
    }

    public void HideMessage()
    {
        SetVisible(false);
    }

    private void SetVisible(bool value)
    {
        visible = value;

        // activo/desactivo todos los visuales asignados. sirve si el escapa esta armado con varias partes,
        // tipo texto, mancha, sangre falsa y cosas de pelicula
        if (messageObjects == null)
        {
            return;
        }

        foreach (GameObject messageObject in messageObjects)
        {
            if (messageObject != null)
            {
                messageObject.SetActive(visible);
            }
        }
    }
}
