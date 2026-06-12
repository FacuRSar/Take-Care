using UnityEngine;

/* punto donde puede aparecer el Pursuer en su modo patrulla.
*  cada punto define como se comporta la aparicion ahi: cuanto dura antes de irse
*  y si puede pasear (lugar grande tipo pasillo/sotano) o si es un rincon chico
*  del que se va rapido.
*/
public class PursuerPatrolPoint : MonoBehaviour
{
    [Header("Tipo de lugar")]
    // si esta activo es un lugar amplio (pasillo, sotano, cocina): pasea varios segundos.
    // si esta apagado es un rincon chico: mira un poco y se va rapido.
    [SerializeField] private bool spaciousArea = true;

    [Header("Duracion de la aparicion")]
    // cuanto se queda dando vueltas si es lugar amplio
    [SerializeField] private float spaciousDuration = 12f;
    // cuanto se queda si es un rincon chico
    [SerializeField] private float smallDuration = 3f;

    [Header("Radio de paseo")]
    // que tan lejos busca el proximo punto al vagar, medido desde donde esta parado
    // (no desde el spawn), asi recorre el lugar. Mas grande = recorre mas espacio.
    [SerializeField] private float wanderRadius = 10f;

    public bool SpaciousArea => spaciousArea;
    public float StayDuration => spaciousArea ? spaciousDuration : smallDuration;
    public float WanderRadius => wanderRadius;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = spaciousArea ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
