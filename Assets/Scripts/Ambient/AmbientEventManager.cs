using System.Collections.Generic;
using UnityEngine;

/* manager para disparar eventos de ambiente por id.
*  sirve cuando un evento esta en modo Manual y lo queres lanzar desde codigo:
*  AmbientEventManager.Instance.Raise("portazo_sotano");
*  los eventos se registran solos en su Awake.
*/
public class AmbientEventManager : MonoBehaviour
{
    public static AmbientEventManager Instance;

    private readonly Dictionary<string, AmbientEvent> events = new Dictionary<string, AmbientEvent>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Register(AmbientEvent ambientEvent)
    {
        if (ambientEvent == null || string.IsNullOrEmpty(ambientEvent.EventId))
        {
            return;
        }

        events[ambientEvent.EventId] = ambientEvent;
    }

    public void Unregister(AmbientEvent ambientEvent)
    {
        if (ambientEvent == null || string.IsNullOrEmpty(ambientEvent.EventId))
        {
            return;
        }

        if (events.ContainsKey(ambientEvent.EventId) && events[ambientEvent.EventId] == ambientEvent)
        {
            events.Remove(ambientEvent.EventId);
        }
    }

    public void Raise(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        if (events.TryGetValue(eventId, out AmbientEvent ambientEvent) && ambientEvent != null)
        {
            ambientEvent.TriggerManually();
        }
        else
        {
            Debug.LogWarning("[AmbientEventManager] No hay evento registrado con id: " + eventId);
        }
    }
}
