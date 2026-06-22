using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HintDialogueEntry
{
    [TextArea]
    public string text = "deberia buscar si hay alguna manera de encender la luz";
    public float duration = 3f;
    public string[] requiredFlags;
    public string[] blockedFlags;

    // Audio opcional de la pista. El subtitulo manda: si el clip dura distinto a "duration",
    // se ajusta el pitch para que el audio entre justo en ese tiempo.
    public AudioClip audioClip;
}

public class HintDialogueController : MonoBehaviour
{
    public static HintDialogueController Instance;

    [Header("Referencias")]
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private GameStateController gameStateController;
    // AudioSource para la voz de las pistas. Si queda vacio se crea uno solo.
    [SerializeField] private AudioSource voiceSource;

    [Header("Audio")]
    // limites de pitch para que al sincronizar no quede irreconocible
    [SerializeField] private float minVoicePitch = 0.5f;
    [SerializeField] private float maxVoicePitch = 2.5f;

    [Header("Ayuda")]
    [SerializeField] private float firstHintDelay = 15f;
    [SerializeField] private float repeatInterval = 18f;
    [SerializeField] private HintDialogueEntry[] hintPool;

    [Header("Legacy")]
    [SerializeField] private bool activeOnStart;

    private bool active;
    private float hintTimer;

    // disponibilidad de cada pista en el ultimo chequeo, para detectar cuando aparece una nueva
    private bool[] wasAvailable;
    private bool anyAvailable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (subtitleUI == null)
        {
            subtitleUI = SubtitleUI.Instance;
        }

        if (gameStateController == null)
        {
            gameStateController = GameStateController.Instance;
        }

        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
            }
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        // me engancho a los cambios de flag para saber cuando arranca un ciclo nuevo
        GameStateController.OnFlagChanged += HandleFlagChanged;
    }

    private void OnDisable()
    {
        GameStateController.OnFlagChanged -= HandleFlagChanged;
    }

    private void Start()
    {
        if (activeOnStart)
        {
            StartHints();
        }
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        // mientras no haya ninguna pista disponible, el timer queda en pausa
        if (!anyAvailable)
        {
            return;
        }

        hintTimer -= Time.deltaTime;

        if (hintTimer <= 0f)
        {
            ShowRandomHint();
            hintTimer = repeatInterval;
        }
    }

    public void StartHints()
    {
        active = true;

        // tomo la foto inicial de disponibilidad y arranco el primer ciclo
        RefreshAvailability();
        hintTimer = firstHintDelay;
    }

    public void StopHints()
    {
        active = false;
    }

    // Reinicia el ciclo a mano: vuelve a esperar firstHintDelay antes de la proxima pista.
    // Sirve cuando querer forzar el reinicio sin depender del temporizador compartido.
    public void RestartCycle()
    {
        if (!active)
        {
            StartHints();
            return;
        }

        RefreshAvailability();
        hintTimer = firstHintDelay;
    }

    public void ClearFlag(string flagName)
    {
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;

        if (targetState != null)
        {
            targetState.ClearFlag(flagName);
        }
    }

    public void RemoveFlag(string flagName)
    {
        ClearFlag(flagName);
    }

    private void HandleFlagChanged(string flagName, bool value)
    {
        if (!active)
        {
            return;
        }

        // si por este cambio de flag aparecio una pista que antes no estaba disponible,
        // arranca un ciclo nuevo y se vuelve a esperar firstHintDelay (timer compartido)
        if (RefreshAvailability())
        {
            hintTimer = firstHintDelay;
        }
    }

    // Recalcula que pistas estan disponibles. Devuelve true si alguna paso de
    // no-disponible a disponible desde el ultimo chequeo.
    private bool RefreshAvailability()
    {
        anyAvailable = false;

        if (hintPool == null || hintPool.Length == 0)
        {
            wasAvailable = null;
            return false;
        }

        if (wasAvailable == null || wasAvailable.Length != hintPool.Length)
        {
            wasAvailable = new bool[hintPool.Length];
        }

        bool newAppeared = false;

        for (int i = 0; i < hintPool.Length; i++)
        {
            HintDialogueEntry hint = hintPool[i];
            bool availableNow = hint != null && !string.IsNullOrEmpty(hint.text) && CanUseHint(hint);

            if (availableNow && !wasAvailable[i])
            {
                newAppeared = true;
            }

            wasAvailable[i] = availableNow;

            if (availableNow)
            {
                anyAvailable = true;
            }
        }

        return newAppeared;
    }

    private void ShowRandomHint()
    {
        HintDialogueEntry hint = GetRandomAvailableHint();

        // Si no hay ninguna hint disponible (por flags) no mostramos nada.
        if (hint == null)
        {
            return;
        }

        // la duracion del subtitulo manda; el audio (si hay) se ajusta a ella
        float duration = GetHintDuration(hint);
        PlayHintAudio(hint, duration);
        ShowHint(hint.text, duration);
    }

    // Duracion real de la pista: la configurada, o el largo del clip si no se puso duracion.
    private float GetHintDuration(HintDialogueEntry hint)
    {
        if (hint.duration > 0f)
        {
            return hint.duration;
        }

        if (hint.audioClip != null)
        {
            return hint.audioClip.length;
        }

        return hint.duration;
    }

    private void PlayHintAudio(HintDialogueEntry hint, float duration)
    {
        if (voiceSource == null)
        {
            return;
        }

        if (hint.audioClip == null)
        {
            if (voiceSource.isPlaying)
            {
                voiceSource.Stop();
            }
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = hint.audioClip;

        // pitch para que el clip entre justo en la duracion del subtitulo
        float pitch = 1f;
        if (duration > 0.01f)
        {
            pitch = Mathf.Clamp(hint.audioClip.length / duration, minVoicePitch, maxVoicePitch);
        }

        voiceSource.pitch = pitch;
        voiceSource.Play();
    }

    private HintDialogueEntry GetRandomAvailableHint()
    {
        if (hintPool == null || hintPool.Length == 0)
        {
            return null;
        }

        List<HintDialogueEntry> availableHints = new List<HintDialogueEntry>();

        foreach (HintDialogueEntry hint in hintPool)
        {
            if (hint == null || string.IsNullOrEmpty(hint.text))
            {
                continue;
            }

            if (CanUseHint(hint))
            {
                availableHints.Add(hint);
            }
        }

        if (availableHints.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableHints.Count);
        return availableHints[randomIndex];
    }

    private bool CanUseHint(HintDialogueEntry hint)
    {
        GameStateController targetState = gameStateController != null ? gameStateController : GameStateController.Instance;

        if (targetState == null)
        {
            return true;
        }

        if (hint.requiredFlags != null)
        {
            foreach (string flag in hint.requiredFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !targetState.GetFlag(flag))
                {
                    return false;
                }
            }
        }

        if (hint.blockedFlags != null)
        {
            foreach (string flag in hint.blockedFlags)
            {
                if (!string.IsNullOrEmpty(flag) && targetState.GetFlag(flag))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ShowHint(string text, float duration)
    {
        SubtitleUI targetSubtitle = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;

        if (targetSubtitle != null)
        {
            targetSubtitle.ShowSubtitle(text, duration, SubtitlePriority.Hint);
        }
        else
        {
            //Debug.LogWarning("hintdialoguecontroller: no hay subtitleui disponible");
        }
    }
}
