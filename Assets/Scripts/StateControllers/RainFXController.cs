using UnityEngine;

/* Controla los efectos visuales de lluvia/tormenta.
*  Sirve para activar o desactivar particulas y fondos de tormenta cuando ya no se ven.
*/
public class RainFXController : MonoBehaviour
{
    [Header("Particulas de lluvia")]
    [SerializeField] private ParticleSystem[] rainParticles;
    // Sistemas de particulas que simulan lluvia visible.

    [Header("Visual de tormenta")]
    [SerializeField] private GameObject[] stormVisuals;
    // Fondos, planos con nubes, quads u otros objetos visuales de tormenta.

    [Header("Configuracion")]
    [SerializeField] private bool activeOnStart = true;
    // Define si la lluvia arranca activa al comenzar la escena.

    private void Start()
    {
        if (activeOnStart)
        {
            ActivateRain();
        }
        else
        {
            DeactivateRain();
        }
    }

    public void ActivateRain()
    {
        foreach (ParticleSystem rain in rainParticles)
        {
            if (rain == null)
            {
                continue;
            }

            rain.gameObject.SetActive(true);

            if (!rain.isPlaying)
            {
                rain.Play();
            }
        }

        SetStormVisuals(true);
    }

    public void DeactivateRain()
    {
        foreach (ParticleSystem rain in rainParticles)
        {
            if (rain == null)
            {
                continue;
            }

            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rain.gameObject.SetActive(false);
        }

        SetStormVisuals(false);
    }

    private void SetStormVisuals(bool active)
    {
        foreach (GameObject visual in stormVisuals)
        {
            if (visual == null)
            {
                continue;
            }

            visual.SetActive(active);
        }
    }
}