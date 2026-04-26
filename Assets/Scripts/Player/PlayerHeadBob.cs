using UnityEngine;

/* Efecto visual simple para que la camara se sienta como si el jugador caminara.
*  No configura velocidades de movimiento: esas salen de PlayerMovement.
*  Este script solo regula como se ve el movimiento de la camara.
*/
public class PlayerHeadBob : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform playerCamera;
    // Camara del jugador.

    [SerializeField] private PlayerMovement playerMovement;
    // Referencia al movimiento para leer velocidad y estados sin duplicar configuracion.

    [Header("Efecto visual")]
    [SerializeField] private float bobFrequencyMultiplier = 2f;
    [SerializeField] private float bobAmountMultiplier = 0.012f;
    [SerializeField] private float crouchBobMultiplier = 0.4f;
    [SerializeField] private float returnSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugHeadBob = true;
    [SerializeField] private float debugInterval = 0.5f;

    private Vector3 originalLocalPosition;
    private float bobTimer;
    private float debugTimer;

    private void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerCamera != null)
        {
            originalLocalPosition = playerCamera.localPosition;
        }

        if (debugHeadBob)
        {
            // Debug.Log("[HeadBob] Start. PlayerMovement: " + playerMovement + " | Camera: " + playerCamera);

            if (playerMovement == null)
                Debug.LogWarning("[HeadBob] No tiene PlayerMovement asignado/encontrado.");

            if (playerCamera == null)
                Debug.LogWarning("[HeadBob] No tiene PlayerCamera Transform asignado.");
        }
    }

    private void Update()
    {
        HandleHeadBob();
    }

    private void HandleHeadBob()
    {
        if (playerMovement == null || playerCamera == null)
        {
            if (debugHeadBob)

            return;
        }

        debugTimer += Time.deltaTime;

        if (debugHeadBob && debugTimer >= debugInterval)
        {
            debugTimer = 0f;
        }

        if (!playerMovement.IsMoving)
        {
            bobTimer = 0f;

            playerCamera.localPosition = Vector3.Lerp(
                playerCamera.localPosition,
                originalLocalPosition,
                Time.deltaTime * returnSpeed
            );

            return;
        }

        float speed = playerMovement.CurrentSpeed;

        float bobFrequency = speed * bobFrequencyMultiplier;
        float bobAmount = speed * bobAmountMultiplier;

        if (playerMovement.IsCrouching)
        {
            bobAmount *= crouchBobMultiplier;
            bobFrequency *= crouchBobMultiplier;
        }

        bobTimer += Time.deltaTime * bobFrequency;

        float yOffset = Mathf.Sin(bobTimer) * bobAmount;

        Vector3 targetPosition = originalLocalPosition + new Vector3(0f, yOffset, 0f);

        playerCamera.localPosition = Vector3.Lerp(
            playerCamera.localPosition,
            targetPosition,
            Time.deltaTime * returnSpeed
        );
    }
}