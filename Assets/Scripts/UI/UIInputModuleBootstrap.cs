using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Cablea las acciones del mapa UIInputActions al InputSystemUIInputModule.
/// En build el modulo no siempre resuelve acciones automaticamente si quedaron en null en el prefab.
/// </summary>
[DefaultExecutionOrder(-200)]
public class UIInputModuleBootstrap : MonoBehaviour
{
    private const string UiMapName = "UIInputActions";

    private void Awake()
    {
        WireModule(GetComponent<InputSystemUIInputModule>());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WireAllSceneModules()
    {
        InputSystemUIInputModule[] modules = FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
        for (int i = 0; i < modules.Length; i++)
        {
            WireModule(modules[i]);
        }
    }

    private static void WireModule(InputSystemUIInputModule module)
    {
        if (module == null || module.actionsAsset == null)
        {
            return;
        }

        InputActionMap uiMap = module.actionsAsset.FindActionMap(UiMapName, false);
        if (uiMap == null)
        {
            return;
        }

        if (module.point == null)
        {
            module.point = InputActionReference.Create(uiMap.FindAction("Point", true));
        }

        if (module.leftClick == null)
        {
            module.leftClick = InputActionReference.Create(uiMap.FindAction("LeftClick", true));
        }

        if (module.rightClick == null)
        {
            module.rightClick = InputActionReference.Create(uiMap.FindAction("RightClick", true));
        }

        if (module.middleClick == null)
        {
            module.middleClick = InputActionReference.Create(uiMap.FindAction("MiddleClick", true));
        }

        if (module.scrollWheel == null)
        {
            module.scrollWheel = InputActionReference.Create(uiMap.FindAction("ScrollWheel", true));
        }

        if (module.move == null)
        {
            module.move = InputActionReference.Create(uiMap.FindAction("Navigate", true));
        }

        if (module.submit == null)
        {
            module.submit = InputActionReference.Create(uiMap.FindAction("Submit", true));
        }

        if (module.cancel == null)
        {
            module.cancel = InputActionReference.Create(uiMap.FindAction("Cancel", true));
        }

        if (!uiMap.enabled)
        {
            uiMap.Enable();
        }
    }
}
