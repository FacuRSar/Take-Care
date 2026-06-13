using System.Collections;
using UnityEngine;

/* Controla la aparicion del stalker.
 * Se llama desde la intro cuando empieza la fase de persecucion.
 */
public class PursuerSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject pursuerObject;
    // punto unico de respaldo: se usa si no hay puntos en spawnPoints.
    [SerializeField] private Transform spawnPoint;
    // IA del final. Si queda vacia, se busca en el pursuerObject.
    [SerializeField] private PursuerNavMeshController chaseController;
    // IA de patrulla. El spawn del final la apaga siempre.
    [SerializeField] private PursuerPatrolController patrolController;

    [Header("Jugador (para elegir punto lejano)")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Puntos de aparicion (solo pasillos)")]
    // la persecucion solo aparece en el punto de pasillo mas lejano al jugador.
    // si esta vacio, usa spawnPoint.
    [SerializeField] private Transform[] hallwaySpawnPoints;
    // distancia minima al jugador para que un punto sea valido (que no aparezca encima).
    [SerializeField] private float minDistanceToPlayer = 12f;

    [Header("Configuracion")]
    [SerializeField] private float spawnDelay = 3f;
    [SerializeField] private bool spawnOnlyOnce = true;

    [Header("Sonido")]
    [SerializeField] private string spawnSfxId = "StalkerSpawn";
    [SerializeField] private bool playSpawnSoundAs3D = true;

    [Header("Musica de aparicion")]
    // capa de musica de tension que entra al spawnear y sale al desaparecer. Vacio = sin musica.
    [SerializeField] private string spawnMusicId = "";
    [SerializeField] private float musicFadeIn = 1.5f;
    [SerializeField] private float musicFadeOut = 1.5f;

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
            Debug.Log("PursuerSpawnController: stalker antes de mover | active: " + pursuerObject.activeSelf + " | pos: " + pursuerObject.transform.position);
        }

        Transform chosen = ResolveSpawnTransform();

        if (chosen != null)
        {
            if (showDebugLogs)
            {
                Debug.Log("PursuerSpawnController: punto elegido: " + chosen.name + " | pos: " + chosen.position);
            }

            PursuerSpawnUtils.PlaceOnNavMesh(pursuerObject, chosen.position, chosen.rotation);
        }
        else
        {
            Debug.LogWarning("PursuerSpawnController: no hay punto de spawn valido (ni spawnPoints ni spawnPoint).");
        }

        pursuerObject.SetActive(true);
        ActivateChaseMode();

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

        // capa de musica de tension que entra con la aparicion
        PursuerSpawnUtils.PlaySpawnMusic(spawnMusicId, musicFadeIn);
    }

    // El spawn del final siempre fuerza la IA de persecucion directa.
    private void ActivateChaseMode()
    {
        ResolveControllers();

        if (patrolController != null)
        {
            patrolController.enabled = false;
        }

        if (chaseController != null)
        {
            if (!chaseController.enabled)
            {
                chaseController.enabled = true;
            }
            else
            {
                chaseController.ResetSpeedRamp();
            }
        }
        else
        {
            Debug.LogWarning("PursuerSpawnController: no hay PursuerNavMeshController para activar la persecucion.");
        }
    }

    private void ResolveControllers()
    {
        if (pursuerObject == null)
        {
            return;
        }

        if (chaseController == null)
        {
            chaseController = pursuerObject.GetComponent<PursuerNavMeshController>();
        }

        if (patrolController == null)
        {
            patrolController = pursuerObject.GetComponent<PursuerPatrolController>();
        }
    }

    // Elige el punto de pasillo mas lejano; si no hay lista usa el spawnPoint unico.
    private Transform ResolveSpawnTransform()
    {
        ResolvePlayer();

        Transform farthest = PursuerSpawnUtils.PickFarthestPoint(hallwaySpawnPoints, player, minDistanceToPlayer);

        if (farthest != null)
        {
            return farthest;
        }

        return spawnPoint;
    }

    private void ResolvePlayer()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);

            if (found != null)
            {
                player = found.transform;
            }
        }
    }

    // corta la capa de musica cuando el stalker se va (lo llama el flujo del juego o al desactivar).
    public void StopSpawnMusic()
    {
        PursuerSpawnUtils.StopSpawnMusic(spawnMusicId, musicFadeOut);
    }

    private void OnDisable()
    {
        StopSpawnMusic();
    }
}