using System.Collections.Generic;
using UnityEngine;

// controla estados simples globales del juego
public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance;

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

        flags[flagName] = value;
    }

    public bool GetFlag(string flagName)
    {
        // devuelve true si existe y está activada.
        if (string.IsNullOrEmpty(flagName))
        {
            return false;
        }

        return flags.ContainsKey(flagName) && flags[flagName];
    }

    // Añado un metodo para resetear estados por si se reinicia una escena para no destruir el objeto o para reusar algo (por si acaso)
    public void ResetState()
    {
        IntroActivated = false;
        flags.Clear();
    }
}