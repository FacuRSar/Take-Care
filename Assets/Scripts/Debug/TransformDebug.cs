using UnityEngine;

// Debug porque tuve bardos agarrando objetos asi que puse este
public class TransformDebug : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.lossyScale;
    }

    private void Update()
    {
        if (transform.position != lastPosition)
        {
            Debug.Log(gameObject.name + " cambio posicion: " + transform.position);
            lastPosition = transform.position;
        }

        if (transform.rotation != lastRotation)
        {
            Debug.Log(gameObject.name + " cambio rotacion: " + transform.rotation.eulerAngles);
            lastRotation = transform.rotation;
        }

        if (transform.lossyScale != lastScale)
        {
            Debug.Log(gameObject.name + " cambio escala real: " + transform.lossyScale);
            lastScale = transform.lossyScale;
        }
    }
}