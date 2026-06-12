using UnityEngine;

/* Feedback continuo segun la distancia entre un punto (source) y el jugador (target).
*  Sirve para dos cosas (se pueden combinar):
*
*  1) Vignette por proximidad: cuanto mas cerca, mas intensa. Arranca sutil.
*     Ej: ponerlo en el Pursuer para que la pantalla se cierre cuando se acerca.
*
*  2) Pistas por distancia ("caliente / tibio / frio"): cada cierto delay tira un
*     subtitulo segun la banda de distancia en la que este el jugador.
*     Ej: ponerlo en el objeto escondido de una quest para guiar al jugador.
*
*  Se prende solo (AlwaysWhenEnabled) o mientras una flag este activa (WhileFlagOn),
*  asi un AmbientEvent que setea esa flag lo enciende/apaga.
*/
public class ProximityFeedback : MonoBehaviour
{
    public enum ActivationMode { AlwaysWhenEnabled, WhileFlagOn }

    [System.Serializable]
    public class DistanceHint
    {
        // se elige esta pista si la distancia es <= a este valor (gana la banda mas chica que cumpla)
        public float withinDistance = 3f;
        [TextArea]
        public string message = "Caliente";
    }

    [Header("Activacion")]
    [SerializeField] private ActivationMode activation = ActivationMode.AlwaysWhenEnabled;
    // si activation = WhileFlagOn, solo corre mientras esta flag este prendida
    [SerializeField] private string activeFlagName = "";

    [Header("Medicion")]
    // desde donde se mide. Si queda vacio, usa este mismo transform.
    [SerializeField] private Transform source;
    // hacia quien se mide. Si queda vacio, lo busca por tag.
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Rango")]
    // mas lejos que esto: feedback en 0 / sin pista
    [SerializeField] private float startDistance = 12f;
    // a esta distancia o menos: maximo ("caliente")
    [SerializeField] private float fullDistance = 1.5f;

    [Header("Vignette (opcional)")]
    [SerializeField] private bool useVignette = false;
    // id del efecto Vignette en el ScreenEffectController (conviene uno dedicado)
    [SerializeField] private string vignetteEffectId = "pursuerVignette";
    [SerializeField, Range(0f, 1f)] private float minIntensity = 0.08f;
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.55f;
    // velocidad de cambio de la intensidad (unidades/seg) para que no salte
    [SerializeField] private float changeSpeed = 1.5f;

    [Header("Pistas por distancia (opcional)")]
    [SerializeField] private bool useDistanceHints = false;
    // cada cuanto repite la pista
    [SerializeField] private float hintInterval = 3f;
    [SerializeField] private float hintDuration = 2f;
    [SerializeField] private SubtitlePriority hintPriority = SubtitlePriority.Hint;
    // solo da pistas si el jugador esta mas cerca que esto. 0 = sin limite (siempre da pista).
    [SerializeField] private float hintMaxDistance = 0f;
    // bandas de distancia: ej. caliente(2), tibio(5), frio(12)
    [SerializeField] private DistanceHint[] hints;

    private float currentIntensity;
    private bool vignetteApplied;
    private float nextHintTime;

    private void OnEnable()
    {
        ResolveTarget();

        if (source == null)
        {
            source = transform;
        }

        currentIntensity = 0f;
        vignetteApplied = false;
        nextHintTime = 0f;
        ApplyVignette(0f);
    }

    private void OnDisable()
    {
        ApplyVignette(0f);
    }

    private void ResolveTarget()
    {
        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(targetTag);

            if (found != null)
            {
                target = found.transform;
            }
        }
    }

    private bool IsActive()
    {
        if (activation == ActivationMode.AlwaysWhenEnabled)
        {
            return true;
        }

        return GameStateController.Instance != null
            && !string.IsNullOrEmpty(activeFlagName)
            && GameStateController.Instance.GetFlag(activeFlagName);
    }

    private void Update()
    {
        bool active = IsActive();

        float distance = float.PositiveInfinity;

        if (active)
        {
            if (target == null)
            {
                ResolveTarget();
            }

            if (target != null)
            {
                Vector3 from = source != null ? source.position : transform.position;
                distance = Vector3.Distance(from, target.position);
            }
        }

        if (useVignette)
        {
            TickVignette(active, distance);
        }

        if (useDistanceHints && active && target != null)
        {
            TickHints(distance);
        }
    }

    private void TickVignette(bool active, float distance)
    {
        float targetIntensity = (active && !float.IsPositiveInfinity(distance))
            ? IntensityForDistance(distance)
            : 0f;

        currentIntensity = changeSpeed > 0f
            ? Mathf.MoveTowards(currentIntensity, targetIntensity, changeSpeed * Time.deltaTime)
            : targetIntensity;

        ApplyVignette(currentIntensity);
    }

    private float IntensityForDistance(float distance)
    {
        if (distance >= startDistance)
        {
            return 0f;
        }

        // 0 en startDistance, 1 en fullDistance o mas cerca
        float t = Mathf.InverseLerp(startDistance, fullDistance, distance);
        return Mathf.Lerp(minIntensity, maxIntensity, t);
    }

    private void ApplyVignette(float value)
    {
        // evita spamear cuando ya esta apagada y sigue apagada
        if (!vignetteApplied && value <= 0.0001f)
        {
            return;
        }

        vignetteApplied = value > 0.0001f;

        if (ScreenEffectController.Instance != null && !string.IsNullOrEmpty(vignetteEffectId))
        {
            ScreenEffectController.Instance.SetVignetteIntensity(vignetteEffectId, value);
        }
    }

    private void TickHints(float distance)
    {
        if (hintMaxDistance > 0f && distance > hintMaxDistance)
        {
            return;
        }

        if (Time.time < nextHintTime)
        {
            return;
        }

        string message = PickHint(distance);

        if (!string.IsNullOrEmpty(message) && SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(message, hintDuration, hintPriority);
            nextHintTime = Time.time + Mathf.Max(0.1f, hintInterval);
        }
    }

    // Elige la banda mas chica que cumpla distance <= withinDistance. Si ninguna cumple
    // (esta lejos de todas), usa la banda mas grande como "frio".
    private string PickHint(float distance)
    {
        if (hints == null || hints.Length == 0)
        {
            return null;
        }

        DistanceHint chosen = null;
        DistanceHint largest = null;

        foreach (DistanceHint hint in hints)
        {
            if (hint == null)
            {
                continue;
            }

            if (largest == null || hint.withinDistance > largest.withinDistance)
            {
                largest = hint;
            }

            if (distance <= hint.withinDistance)
            {
                if (chosen == null || hint.withinDistance < chosen.withinDistance)
                {
                    chosen = hint;
                }
            }
        }

        if (chosen != null)
        {
            return chosen.message;
        }

        return largest != null ? largest.message : null;
    }
}
