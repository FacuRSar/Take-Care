using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* hace aparecer al Pursuer en modo patrulla cada cierto tiempo.
*  elige un punto lejano al jugador, lo teletransporta ahi con la risa, lo deja patrullar
*  el tiempo que diga ese punto y despues titila (sin captura) hasta la proxima aparicion.
*
*  Pensado para una partida de 10 minutos: apariciones espaciadas y cortas, para meter
*  presion sin frenar las misiones.
*/
public class PursuerPatrolSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject pursuerObject;
    [SerializeField] private PursuerPatrolController patrolController;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Puntos de aparicion (pools separadas)")]
    // puntos dentro de habitaciones (aparicion mas rara, segun la cuota de abajo)
    [SerializeField] private PursuerPatrolPoint[] roomSpawnPoints;
    // puntos en pasillos (la mayoria de las apariciones caen aca)
    [SerializeField] private PursuerPatrolPoint[] hallwaySpawnPoints;

    [Header("Cuota de habitaciones")]
    // como minimo/maximo cuantas apariciones caen en habitaciones; el resto va a pasillos.
    [SerializeField] private int minRoomAppearances = 2;
    [SerializeField] private int maxRoomAppearances = 3;
    // dentro de cuantas apariciones iniciales se reparten las de habitacion.
    [SerializeField] private int roomScheduleWindow = 8;

    [Header("Tiempos entre apariciones")]
    // espera entre apariciones (aleatorio entre min y max). espaciado para no agobiar.
    [SerializeField] private Vector2 timeBetweenAppearances = new Vector2(45f, 75f);
    // espera inicial antes de la primera aparicion
    [SerializeField] private float firstAppearanceDelay = 30f;

    [Header("Eleccion de punto")]
    // distancia minima al jugador para que un punto sea valido (que no aparezca encima)
    [SerializeField] private float minDistanceToPlayer = 12f;

    [Header("Sonido de aparicion")]
    [SerializeField] private string spawnSfxId = "DollLaugh";
    [SerializeField] private bool spawnSoundAs3D = false;

    [Header("Musica de aparicion")]
    // capa de musica de tension que entra al aparecer y sale con fade al guardarse. Vacio = sin musica.
    [SerializeField] private string spawnMusicId = "";
    [SerializeField] private float musicFadeIn = 1.5f;
    [SerializeField] private float musicFadeOut = 2f;

    [Header("Desaparicion (efecto fantasma)")]
    // al terminar la aparicion, titila el visual para asustar (ya no puede capturar).
    [SerializeField] private bool flickerOnVanish = true;
    // cuanto dura el titileo en total
    [SerializeField] private float flickerDuration = 0.6f;
    // cada cuanto prende/apaga el visual durante el titileo
    [SerializeField] private float flickerInterval = 0.08f;

    [Header("Control")]
    // arranca el ciclo apenas se activa el spawner
    [SerializeField] private bool autoStart = false;
    // o arranca cuando se prende esta flag (vacio = no usa flag). Asi el flujo queda fijo.
    [SerializeField] private string startFlagName = "doll_quests_start";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("Debug (testeo)")]
    // si esta activo, el numpad 6 fuerza una aparicion en modo patrulla.
    [SerializeField] private bool enableDebugKey = true;
#endif

    private Coroutine loopRoutine;
    private bool appearing;

    // cuenta de apariciones desde que arranco el ciclo y en cuales toca habitacion.
    private int appearanceIndex;
    private HashSet<int> roomAppearanceIndices;

    private void OnEnable()
    {
        ResolvePlayer();

        GameStateController.OnFlagChanged += HandleFlagChanged;

        if (autoStart)
        {
            StartLoop();
        }
        else if (!string.IsNullOrEmpty(startFlagName) && IsFlagOn(startFlagName))
        {
            // por si la flag ya estaba puesta antes de activarse el spawner
            StartLoop();
        }
    }

    private void OnDisable()
    {
        GameStateController.OnFlagChanged -= HandleFlagChanged;
        StopLoop();
    }

    private void HandleFlagChanged(string flagName, bool value)
    {
        if (value && !string.IsNullOrEmpty(startFlagName) && flagName == startFlagName)
        {
            StartLoop();
        }
    }

    private static bool IsFlagOn(string flagName)
    {
        return GameStateController.Instance != null
            && !string.IsNullOrEmpty(flagName)
            && GameStateController.Instance.GetFlag(flagName);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!enableDebugKey || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            //Debug.Log("PursuerPatrolSpawner: debug numpad6 -> aparicion forzada en patrulla.");
            ForceAppearOnce();
        }
    }
#endif

    // Fuerza una aparicion ya, sin esperar el ciclo. La usa el debug del numpad 6.
    public void ForceAppearOnce()
    {
        if (appearing)
        {
            return;
        }

        ResolvePlayer();

        if (roomAppearanceIndices == null)
        {
            BuildRoomSchedule();
        }

        StartCoroutine(AppearOnce());
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

    // Arranca el ciclo de apariciones. Lo puede llamar el flujo del InGame cuando empieza la presion.
    public void StartLoop()
    {
        if (loopRoutine != null)
        {
            return;
        }

        appearanceIndex = 0;
        BuildRoomSchedule();
        loopRoutine = StartCoroutine(AppearanceLoop());
    }

    // Decide en que apariciones (indices) toca habitacion: entre min y max, repartidas dentro
    // de la ventana inicial. El resto de las apariciones caen siempre en pasillos.
    private void BuildRoomSchedule()
    {
        roomAppearanceIndices = new HashSet<int>();

        if (!HasValidPoints(roomSpawnPoints))
        {
            return;
        }

        int min = Mathf.Max(0, minRoomAppearances);
        int max = Mathf.Max(min, maxRoomAppearances);
        int target = Random.Range(min, max + 1);

        if (target <= 0)
        {
            return;
        }

        int window = Mathf.Max(target, roomScheduleWindow);

        List<int> candidates = new List<int>(window);
        for (int i = 0; i < window; i++)
        {
            candidates.Add(i);
        }

        // shuffle simple (Fisher-Yates) para repartir las habitaciones al azar dentro de la ventana
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        for (int i = 0; i < target && i < candidates.Count; i++)
        {
            roomAppearanceIndices.Add(candidates[i]);
        }
    }

    public void StopLoop()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        // si se corto a mitad de una aparicion, saco la capa de musica igual
        if (appearing)
        {
            PursuerSpawnUtils.StopSpawnMusic(spawnMusicId, musicFadeOut);
            appearing = false;
        }
    }

    private IEnumerator AppearanceLoop()
    {
        yield return new WaitForSeconds(firstAppearanceDelay);

        while (true)
        {
            if (!appearing)
            {
                yield return StartCoroutine(AppearOnce());
            }

            float wait = Random.Range(timeBetweenAppearances.x, timeBetweenAppearances.y);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator AppearOnce()
    {
        if (pursuerObject == null || patrolController == null)
        {
            //Debug.LogWarning("PursuerPatrolSpawner: falta pursuerObject o patrolController.");
            yield break;
        }

        bool useRoom = roomAppearanceIndices != null
            && roomAppearanceIndices.Contains(appearanceIndex)
            && HasValidPoints(roomSpawnPoints);

        // elige de la pool que toca; si esa quedo sin punto valido, cae a la otra
        PursuerPatrolPoint[] primary = useRoom ? roomSpawnPoints : hallwaySpawnPoints;
        PursuerPatrolPoint[] fallback = useRoom ? hallwaySpawnPoints : roomSpawnPoints;

        PursuerPatrolPoint point = PickFarthestPoint(primary);

        if (point == null)
        {
            point = PickFarthestPoint(fallback);
        }

        if (point == null)
        {
            //Debug.LogWarning("PursuerPatrolSpawner: no hay punto de spawn valido (ni habitaciones ni pasillos).");
            yield break;
        }

        appearanceIndex++;
        appearing = true;

        PursuerSpawnUtils.PlaceOnNavMesh(pursuerObject, point.transform.position, point.transform.rotation);

        pursuerObject.SetActive(true);
        patrolController.BeginPatrol(point);

        if (SFXManager.Instance != null && !string.IsNullOrEmpty(spawnSfxId))
        {
            if (spawnSoundAs3D)
            {
                SFXManager.Instance.Play3D(spawnSfxId, point.transform.position);
            }
            else
            {
                SFXManager.Instance.Play2D(spawnSfxId);
            }
        }

        // capa de musica de tension que entra con la aparicion
        PursuerSpawnUtils.PlaySpawnMusic(spawnMusicId, musicFadeIn);

        // se queda el tiempo que diga el punto y despues entra la fase fantasma (titileo, sin captura)
        yield return new WaitForSeconds(point.StayDuration);

        // sale la capa de musica con fade out al terminar la aparicion activa
        PursuerSpawnUtils.StopSpawnMusic(spawnMusicId, musicFadeOut);

        yield return StartCoroutine(VanishRoutine());

        appearing = false;
    }

    // Titila el visual para asustar y despues desactiva al Pursuer hasta la proxima aparicion.
    private IEnumerator VanishRoutine()
    {
        if (patrolController != null)
        {
            patrolController.BeginVanishPhase();
        }

        Renderer[] renderers = pursuerObject.GetComponentsInChildren<Renderer>(true);

        if (flickerOnVanish && renderers.Length > 0 && flickerInterval > 0f)
        {
            float elapsed = 0f;
            bool visible = true;

            while (elapsed < flickerDuration)
            {
                visible = !visible;
                SetRenderersEnabled(renderers, visible);

                yield return new WaitForSeconds(flickerInterval);
                elapsed += flickerInterval;
            }
        }

        if (renderers.Length > 0)
        {
            SetRenderersEnabled(renderers, true);
        }

        if (pursuerObject != null)
        {
            pursuerObject.SetActive(false);
        }
    }

    private static void SetRenderersEnabled(Renderer[] renderers, bool value)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = value;
            }
        }
    }

    // elige el punto valido mas lejano al jugador dentro de la pool dada, asi nunca aparece encima
    private PursuerPatrolPoint PickFarthestPoint(PursuerPatrolPoint[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        PursuerPatrolPoint best = null;
        float bestDistance = -1f;

        foreach (PursuerPatrolPoint point in pool)
        {
            if (point == null)
            {
                continue;
            }

            float distance = player != null
                ? Vector3.Distance(point.transform.position, player.position)
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

    private static bool HasValidPoints(PursuerPatrolPoint[] pool)
    {
        if (pool == null)
        {
            return false;
        }

        foreach (PursuerPatrolPoint point in pool)
        {
            if (point != null)
            {
                return true;
            }
        }

        return false;
    }
}
