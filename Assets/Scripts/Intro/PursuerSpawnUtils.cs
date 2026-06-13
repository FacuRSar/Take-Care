using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* Utilidades compartidas por los spawners del Pursuer (final y patrulla).
*  Centraliza elegir un punto lejano al jugador, reubicar respetando el NavMesh
*  y la musica de tension que entra/sale con cada aparicion.
*/
public static class PursuerSpawnUtils
{
    // Elige el punto valido mas lejano al jugador para que nunca aparezca encima.
    public static Transform PickFarthestPoint(IReadOnlyList<Transform> points, Transform player, float minDistanceToPlayer)
    {
        if (points == null || points.Count == 0)
        {
            return null;
        }

        Transform best = null;
        float bestDistance = -1f;

        for (int i = 0; i < points.Count; i++)
        {
            Transform point = points[i];

            if (point == null)
            {
                continue;
            }

            float distance = player != null
                ? Vector3.Distance(point.position, player.position)
                : Mathf.Infinity;

            if (distance < minDistanceToPlayer)
            {
                continue;
            }

            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = point;
            }
        }

        return best;
    }

    // Reubica el objeto en el punto respetando el NavMesh: apaga el agente, lo mueve y lo reubica con Warp.
    public static void PlaceOnNavMesh(GameObject pursuer, Vector3 position, Quaternion rotation)
    {
        if (pursuer == null)
        {
            return;
        }

        NavMeshAgent agent = pursuer.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = false;
        }

        pursuer.transform.position = position;
        pursuer.transform.rotation = rotation;

        if (agent != null)
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
            else
            {
                //Debug.LogWarning("PursuerSpawnUtils: el punto de spawn no quedo sobre el NavMesh.");
            }
        }
    }

    // Suma la capa de musica de tension con fade in. Vacio = no hace nada.
    public static void PlaySpawnMusic(string musicId, float fadeIn)
    {
        if (MusicManager.Instance != null && !string.IsNullOrEmpty(musicId))
        {
            MusicManager.Instance.AddMusic(musicId, fadeIn);
        }
    }

    // Saca la capa de musica de tension con fade out. Vacio = no hace nada.
    public static void StopSpawnMusic(string musicId, float fadeOut)
    {
        if (MusicManager.Instance != null && !string.IsNullOrEmpty(musicId))
        {
            MusicManager.Instance.RemoveMusic(musicId, fadeOut);
        }
    }
}
