using UnityEngine;
using UnityEngine.Rendering;

public class ProximityTension : MonoBehaviour
{
    private string musicID = "PersecucionLayer";
    private float tensionLevel = 0f;
    private float DistanseToPlayer;
    private GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        DistanseToPlayer = Vector3.Distance(transform.position, player.transform.position);
        tensionLevel = 1 - (DistanseToPlayer / 5);
        if (tensionLevel < 0)
        {
            tensionLevel = 0;
        }
        MusicManager.Instance.Play(musicID, tensionLevel);
    }
    private void OnDisable()
    {
        MusicManager.Instance.Stop();
    }
    // Update is called once per frame
    void Update()
    {
        DistanseToPlayer = Vector3.Distance(transform.position, player.transform.position);
        tensionLevel = 1 - (DistanseToPlayer / 5);
        if (tensionLevel < 0)
        {
            tensionLevel = 0;
        }
        MusicManager.Instance.targetChangeVolume(musicID, tensionLevel, 0.1f);
    }
}
