using System;
using TMPro;
using UnityEngine;

// tipos de cosas que puede hacer un evento de ambiente
public enum AmbientActionType
{
    EnablePhysics,
    SetActive,
    MoveTo,
    PlaySfx,
    PlayDialogue,
    ShakeCamera,
    SpawnPrefab,
    SetFlag,
    ScreenEffect,
    SetText,
    SetTextMass
}

/* una accion suelta dentro de un AmbientEvent.
*  no todos los campos se usan en cada tipo, el inspector los muestra todos
*  pero solo importan los que van con el AmbientActionType elegido.
*/
[Serializable]
public class AmbientEventAction
{
    public AmbientActionType type;

    [Header("General")]
    // espera estos segundos antes de ejecutar esta accion (0 = al instante). util para escalonar el shake, un sonido, etc.
    public float delay = 0f;

    [Header("Objetivo (EnablePhysics / SetActive / MoveTo)")]
    public GameObject target;

    [Header("SetActive")]
    public bool setActiveValue = true;

    [Header("MoveTo")]
    public Vector3 moveTarget;
    // si esta activo el moveTarget es local, si no es en world
    public bool moveLocal = true;
    public bool alsoRotate;
    public Vector3 rotateTarget;
    public float moveDuration = 1f;

    [Header("EnablePhysics")]
    // empujon opcional al soltar el rigidbody, dejalo en 0 si no queres
    public Vector3 physicsImpulse;

    [Header("PlaySfx")]
    public string sfxId;
    public bool sfx3D = true;

    [Header("PlayDialogue")]
    public string dialogueId;

    [Header("ShakeCamera")]
    // id de un efecto tipo CameraShake configurado en el ScreenEffectController
    public string shakeEffectId;

    [Header("ScreenEffect (cualquier efecto del ScreenEffectController)")]
    // id de un efecto del ScreenEffectController (imagen, vignette, animator, shake, etc.)
    public string screenEffectId;
    // si esta activo, en vez de reproducir el efecto lo apaga
    public bool stopScreenEffect = false;
    // si es mayor a 0, el efecto se apaga solo despues de esos segundos (ej: flash de susto 0.01)
    public float screenEffectAutoStop = 0f;

    [Header("SpawnPrefab")]
    public GameObject prefab;
    public Transform spawnPoint;

    [Header("SetFlag")]
    public string flagName;
    public bool flagValue = true;

    [Header("SetText")]
    // texto objetivo al que le seteamos el contenido
    public TMP_Text textTarget;
    // texto a poner. si lo dejas vacio, borra el texto
    [TextArea]
    public string textValue;
    // segundos del barrido tipo tiza. 0 = usa el default del WallTextReveal (4s). Borrar texto = instantaneo.
    public float textWriteDuration = 4f;
    // id del pool en SFXManager. vacio = usa el default del WallTextReveal (WallScratch).
    public string textScratchSfxId = "WallScratch";
    // si esta activo, el rayado suena en 3D desde la posicion del texto
    public bool textScratchSfx3D = true;

    [Header("SetTextMass")]
    // lista de TMP a los que se les pone el mismo texto de una
    public TMP_Text[] textTargets;
    // sonido 2D que suena una vez al aplicar el texto masivo (pool de SFXManager)
    public string massTextSfxId;
}
