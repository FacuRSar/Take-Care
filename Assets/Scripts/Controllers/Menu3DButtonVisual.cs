using UnityEngine;

/* Visual simple para boton 3D de menu.
 * Recibe hover desde el controller principal.
 */
public class Menu3DButtonVisual : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Movimiento opcional")]
    [SerializeField] private bool moveOnHover = true;
    [SerializeField] private Vector3 hoverLocalOffset = new Vector3(0f, 0.02f, 0f);

    private Material runtimeMaterial;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            normalColor = runtimeMaterial.color;
        }

        originalLocalPosition = transform.localPosition;
    }

    public void SetHover(bool value)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = value ? hoverColor : normalColor;
        }

        if (moveOnHover)
        {
            transform.localPosition = value ? originalLocalPosition + hoverLocalOffset : originalLocalPosition;
        }
    }
}