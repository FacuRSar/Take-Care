using UnityEngine;

// Debug que tuve que crear porque el personaje giraba sin sentido a veces
public class PlayerViewDebug : MonoBehaviour
{
    [SerializeField] private Transform cam;

    private Vector3 lastPlayerRot;
    private Vector3 lastCamRot;

    private void Start()
    {
        lastPlayerRot = transform.eulerAngles;
        lastCamRot = cam.localEulerAngles;
    }

    private void Update()
    {
        if (transform.eulerAngles != lastPlayerRot)
        {
            Debug.Log("Player rot actual: " + transform.eulerAngles);
            lastPlayerRot = transform.eulerAngles;
        }

        if (cam.localEulerAngles != lastCamRot)
        {
            Debug.Log("Cam rot actual: " + cam.localEulerAngles);
            lastCamRot = cam.localEulerAngles;
        }
    }
}