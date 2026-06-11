using System;
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
    SetFlag
}

/* una accion suelta dentro de un AmbientEvent.
*  no todos los campos se usan en cada tipo, el inspector los muestra todos
*  pero solo importan los que van con el AmbientActionType elegido.
*/
[Serializable]
public class AmbientEventAction
{
    public AmbientActionType type;

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

    [Header("SpawnPrefab")]
    public GameObject prefab;
    public Transform spawnPoint;

    [Header("SetFlag")]
    public string flagName;
    public bool flagValue = true;
}
