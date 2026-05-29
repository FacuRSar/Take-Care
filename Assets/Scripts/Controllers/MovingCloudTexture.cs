using UnityEngine;

/* Mueve la textura del material para simular nubes desplazandose.
*  Es barato porque no mueve objetos ni usa particulas, solo cambia el offset del material.
*/
public class MovingCloudTexture : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Renderer targetRenderer;
    // Renderer del plano donde estan las nubes.

    [Header("Movimiento")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.01f, 0f);
    // Velocidad del movimiento de la textura.
    // X mueve horizontal, Y mueve vertical.

    private Material runtimeMaterial;
    // Material instanciado para no modificar el asset original.

    private Vector2 currentOffset;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
        }
    }

    private void Update()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        currentOffset += scrollSpeed * Time.deltaTime;
        runtimeMaterial.mainTextureOffset = currentOffset;
    }
}