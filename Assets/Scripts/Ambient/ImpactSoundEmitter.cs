using UnityEngine;

/* reproduce un sonido cuando el objeto choca lo bastante fuerte.
*  pensado para cosas que caen o se tiran (cajas, objetos, etc.) asi el golpe
*  suena justo en el impacto y no antes. usa el SFXManager por id.
*/
[RequireComponent(typeof(Rigidbody))]
public class ImpactSoundEmitter : MonoBehaviour
{
    [Header("Sonido")]
    [SerializeField] private string sfxId;

    [Header("Configuracion")]
    // velocidad minima del choque para que suene. golpes muy suaves no disparan nada
    [SerializeField] private float minImpactVelocity = 1.5f;

    // tiempo minimo entre sonidos para que no spamee cuando rebota varias veces
    [SerializeField] private float cooldown = 0.15f;

    private float lastPlayTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        if (string.IsNullOrEmpty(sfxId) || SFXManager.Instance == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactVelocity)
        {
            return;
        }

        if (Time.time - lastPlayTime < cooldown)
        {
            return;
        }

        lastPlayTime = Time.time;

        // suena en el punto del choque si lo tenemos, si no en el centro del objeto
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        SFXManager.Instance.Play3D(sfxId, point);
    }
}
