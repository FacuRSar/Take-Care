using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/* Controla la aparicion del stalker.
 * Se llama desde la intro cuando empieza la fase de persecucion.
 */
public class PursuerSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject pursuerObject;
    [SerializeField] private Transform spawnPoint;

    [Header("Configuracion")]
    [SerializeField] private float spawnDelay = 3f;
    [SerializeField] private bool spawnOnlyOnce = true;

    [Header("Sonido")]
    [SerializeField] private string spawnSfxId = "StalkerSpawn";
    [SerializeField] private bool playSpawnSoundAs3D = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool alreadySpawned;

    public void StartSpawnSequence()
    {
        if (showDebugLogs)
        {
            Debug.Log("PursuerSpawnController: StartSpawnSequence llamado.");
        }

        if (spawnOnlyOnce && alreadySpawned)
        {
            if (showDebugLogs)
            {
                Debug.Log("PursuerSpawnController: ya habia spawneado, salgo.");
            }

            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        alreadySpawned = true;

        if (showDebugLogs)
        {
            Debug.Log("PursuerSpawnController: empieza rutina. Delay: " + spawnDelay);
        }

        yield return new WaitForSeconds(spawnDelay);

        if (pursuerObject == null)
        {
            Debug.LogWarning("PursuerSpawnController: no hay pursuerObject asignado.");
            yield break;
        }

        if (showDebugLogs)
        {
            Debug.Log("PursuerSpawnController: stalker antes de mover | active: " + pursuerObject.activeSelf + " | pos: " + pursuerObject.transform.position + " | rot: " + pursuerObject.transform.rotation.eulerAngles);
        }

        if (spawnPoint != null)
        {
            if (showDebugLogs)
            {
                Debug.Log("PursuerSpawnController: spawnPoint: " + spawnPoint.name + " | pos: " + spawnPoint.position + " | rot: " + spawnPoint.rotation.eulerAngles);
            }

            NavMeshAgent agent = pursuerObject.GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log("PursuerSpawnController: NavMeshAgent detectado. enabled antes: " + agent.enabled + " | isOnNavMesh antes: " + agent.isOnNavMesh);
                }

                agent.enabled = false;
            }

            pursuerObject.transform.position = spawnPoint.position;
            pursuerObject.transform.rotation = spawnPoint.rotation;

            if (showDebugLogs)
            {
                Debug.Log("PursuerSpawnController: stalker despues de mover con agent apagado | pos: " + pursuerObject.transform.position + " | rot: " + pursuerObject.transform.rotation.eulerAngles);
            }

            if (agent != null)
            {
                agent.enabled = true;

                if (showDebugLogs)
                {
                    Debug.Log("PursuerSpawnController: NavMeshAgent reactivado. enabled ahora: " + agent.enabled + " | isOnNavMesh ahora: " + agent.isOnNavMesh);
                }

                if (agent.isOnNavMesh)
                {
                    agent.Warp(spawnPoint.position);

                    if (showDebugLogs)
                    {
                        Debug.Log("PursuerSpawnController: agent.Warp aplicado | pos final: " + pursuerObject.transform.position);
                    }
                }
                else
                {
                    Debug.LogWarning("PursuerSpawnController: el stalker NO quedo sobre el NavMesh. Revisar spawnPoint o bake.");
                }
            }
        }
        else
        {
            Debug.LogWarning("PursuerSpawnController: no hay spawnPoint asignado.");
        }

        pursuerObject.SetActive(true);

        if (showDebugLogs)
        {
            Debug.Log("PursuerSpawnController: stalker activado | active: " + pursuerObject.activeSelf + " | pos final: " + pursuerObject.transform.position);
        }

        if (SFXManager.Instance != null && !string.IsNullOrEmpty(spawnSfxId))
        {
            if (playSpawnSoundAs3D)
            {
                SFXManager.Instance.Play3D(spawnSfxId, pursuerObject.transform.position);
            }
            else
            {
                SFXManager.Instance.Play2D(spawnSfxId);
            }

            if (showDebugLogs)
            {
                Debug.Log("PursuerSpawnController: sonido de spawn reproducido: " + spawnSfxId);
            }
        }
        else if (showDebugLogs)
        {
            Debug.Log("PursuerSpawnController: no se reprodujo sonido. Falta SFXManager o spawnSfxId.");
        }
    }
}