using TMPro;
using UnityEngine;

/* Visual simple para boton 3D de menu.
 * Recibe hover desde el controller principal.
 * Puede colorear un Renderer (mesh comun) o un TMP_Text (TextMeshPro).
 * El TMP_Text puede vivir en cualquier parte de la jerarquia, no hace falta que sea hijo del boton.
 */
public class Menu3DButtonVisual : MonoBehaviour
{
    [Header("Visual - opcion A: Mesh comun")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Visual - opcion B: TextMeshPro")]
    [SerializeField] private TMP_Text targetText;

    [Header("Colores")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("Movimiento opcional")]
    [SerializeField] private bool moveOnHover = true;
    [SerializeField] private Vector3 hoverLocalOffset = new Vector3(0f, 0.02f, 0f);

    private Material runtimeMaterial;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        // Si no se asigno nada, intentamos auto-detectar en este GameObject.
        if (targetText == null && targetRenderer == null)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }
        }

        if (targetText != null)
        {
            normalColor = targetText.color;
        }
        else if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            normalColor = runtimeMaterial.color;
        }

        originalLocalPosition = transform.localPosition;
    }

    public void SetHover(bool value)
    {
        Color colorToApply = value ? hoverColor : normalColor;

        if (targetText != null)
        {
            targetText.color = colorToApply;
        }
        else if (runtimeMaterial != null)
        {
            runtimeMaterial.color = colorToApply;
        }

        if (moveOnHover)
        {
            transform.localPosition = value ? originalLocalPosition + hoverLocalOffset : originalLocalPosition;
        }
    }
}
