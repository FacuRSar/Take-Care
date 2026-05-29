using System.Collections.Generic;
using System;
using UnityEngine;

// controla estados simples globales del juego
public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance;
    public static event Action<string, bool> OnFlagChanged;

    public bool IntroActivated { get; private set; }
    // estado de solo lectura desde afuera para que no se pueda romper

    private Dictionary<string, bool> flags = new Dictionary<string, bool>();
    // Flags personalizadas para eventos futuros, lo meti por el DoorInteractable

    private void Awake()
    {
        // aseguramos una sola instancia para evitar conflictos
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ActivateIntro()
    {
        IntroActivated = true;
    }

    public void SetFlag(string flagName, bool value = true)
    {
        // crea o actualiza una flag personalizada.
        if (string.IsNullOrEmpty(flagName))
        {
            return;
        }

        bool previousValue = GetFlag(flagName);
        flags[flagName] = value;

        if (previousValue != value)
        {
            OnFlagChanged?.Invoke(flagName, value);
        }
    }

    public bool GetFlag(string flagName)
    {
        // devuelve true si existe y esta activada.
        if (string.IsNullOrEmpty(flagName))
        {
            return false;
        }

        return flags.ContainsKey(flagName) && flags[flagName];
    }

    public void ClearFlag(string flagName)
    {
        // limpia una flag puntual sin tocar el resto del estado global
        if (string.IsNullOrEmpty(flagName))
        {
            return;
        }

        if (flags.ContainsKey(flagName))
        {
            flags.Remove(flagName);
            OnFlagChanged?.Invoke(flagName, false);
        }
    }
    public void RemoveFlag(string flagName)
    {
        ClearFlag(flagName);
    }
    // anado un metodo para resetear estados por si se reinicia una escena para no destruir el objeto o para reusar algo (por si acaso)
    public void ResetState()
    {
        List<string> activeFlags = new List<string>(flags.Keys);

        IntroActivated = false;
        flags.Clear();

        foreach (string flagName in activeFlags)
        {
            OnFlagChanged?.Invoke(flagName, false);
        }
    }
}
