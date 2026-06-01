using UnityEngine;
using UnityEngine.SceneManagement;

/* Controller principal del menu 3D.
 * Detecta hover/click sobre los botones asignados desde Inspector.
 */
public class Menu3DButtonsController : MonoBehaviour
{
    [System.Serializable]
    public class MenuButtonData
    {
        public string buttonName;
        public Menu3DButtonVisual visual;
        public MenuButtonAction action;
    }

    public enum MenuButtonAction
    {
        Play,
        Credits,
        Exit,
        Settings
    }

    [Header("Camara")]
    [SerializeField] private Camera menuCamera;

    [Header("Botones")]
    [SerializeField] private MenuButtonData[] buttons;

    [Header("Escenas")]
    [SerializeField] private string introSceneName = "Intro";

    [Header("Referencias")]
    // SettingsSystem (PauseMenuController en menuMode). Solo se usa para la accion Settings.
    [SerializeField] private PauseMenuController settingsMenuController;
    // Panel/visual opcional que se apaga cuando se abre Settings y se vuelve a prender al cerrarlo.
    // Sirve para ocultar el visual del menu (mesa, fondo, lo que sea). Puede quedar vacio.
    [SerializeField] private GameObject menuVisualPanel;
    // Secuencia opcional para el boton Play. Si esta asignada, en lugar de cargar la escena
    // directo se dispara la secuencia (titileo + swap de texturas + risa + portazo) y al
    // terminar ella misma carga la escena.
    [SerializeField] private MenuPlaySequence playSequence;

    [Header("Raycast")]
    [SerializeField] private LayerMask buttonMask;
    [SerializeField] private float rayDistance = 100f;

    private Menu3DButtonVisual currentVisual;
    private MenuButtonData currentButton;
    private bool subscribedToSettings;

    private void Awake()
    {
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        SubscribeToSettings();
    }

    private void OnDisable()
    {
        UnsubscribeFromSettings();
    }

    private void Update()
    {
        CheckButtons();
    }

    private void SubscribeToSettings()
    {
        if (settingsMenuController == null || subscribedToSettings)
        {
            return;
        }

        settingsMenuController.OnSettingsOpened += HandleSettingsOpened;
        settingsMenuController.OnSettingsClosed += HandleSettingsClosed;
        subscribedToSettings = true;
    }

    private void UnsubscribeFromSettings()
    {
        if (settingsMenuController == null || !subscribedToSettings)
        {
            return;
        }

        settingsMenuController.OnSettingsOpened -= HandleSettingsOpened;
        settingsMenuController.OnSettingsClosed -= HandleSettingsClosed;
        subscribedToSettings = false;
    }

    private void HandleSettingsOpened()
    {
        //Debug.Log("[Menu3D] HandleSettingsOpened llamado. menuVisualPanel asignado: " + (menuVisualPanel != null) + (menuVisualPanel != null ? (" | nombre: " + menuVisualPanel.name) : ""));

        SetButtonsActive(false);

        if (menuVisualPanel != null)
        {
            menuVisualPanel.SetActive(false);
        }
    }

    private void HandleSettingsClosed()
    {
        //Debug.Log("[Menu3D] HandleSettingsClosed llamado. menuVisualPanel asignado: " + (menuVisualPanel != null));

        SetButtonsActive(true);

        if (menuVisualPanel != null)
        {
            menuVisualPanel.SetActive(true);
        }
    }

    // Activa/desactiva los GameObjects de todos los botones 3D del menu.
    // Cuando se apaga limpia el hover por las dudas.
    private void SetButtonsActive(bool active)
    {
        if (!active)
        {
            ClearHover();
        }

        if (buttons == null)
        {
            return;
        }

        foreach (MenuButtonData button in buttons)
        {
            if (button != null && button.visual != null)
            {
                button.visual.gameObject.SetActive(active);
            }
        }
    }

    private void CheckButtons()
    {
        Ray ray = menuCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, buttonMask))
        {
            Menu3DButtonVisual hitVisual = hit.collider.GetComponentInParent<Menu3DButtonVisual>();
            MenuButtonData hitButton = GetButtonByVisual(hitVisual);

            if (hitButton != null)
            {
                if (currentVisual != hitVisual)
                {
                    ClearHover();

                    currentVisual = hitVisual;
                    currentButton = hitButton;
                    currentVisual.SetHover(true);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    ExecuteButton(currentButton);
                }

                return;
            }
        }

        ClearHover();
    }

    private MenuButtonData GetButtonByVisual(Menu3DButtonVisual visual)
    {
        if (visual == null || buttons == null)
        {
            return null;
        }

        foreach (MenuButtonData button in buttons)
        {
            if (button != null && button.visual == visual)
            {
                return button;
            }
        }

        return null;
    }

    private void ExecuteButton(MenuButtonData button)
    {
        if (button == null)
        {
            return;
        }

        switch (button.action)
        {
            case MenuButtonAction.Play:
                if (playSequence != null)
                {
                    // Bloqueamos mas clicks durante la secuencia desactivando este controller.
                    SetButtonsActive(false);
                    enabled = false;
                    playSequence.Run();
                }
                else if (GameController.Instance != null)
                {
                    GameController.Instance.GoToScene(introSceneName);
                }
                else
                {
                    SceneManager.LoadScene(introSceneName);
                }
                break;

            case MenuButtonAction.Credits:
                Debug.Log("Creditos todavia no implementado.");
                break;

            case MenuButtonAction.Exit:
                Application.Quit();
                Debug.Log("Salir del juego.");
                break;

            case MenuButtonAction.Settings:
                if (settingsMenuController != null)
                {
                    settingsMenuController.OpenSettings();
                }
                else
                {
                    Debug.LogWarning("Menu3DButtonsController: el boton Settings no tiene asignado el SettingsMenuController.");
                }
                break;
        }
    }

    private void ClearHover()
    {
        if (currentVisual != null)
        {
            currentVisual.SetHover(false);
            currentVisual = null;
            currentButton = null;
        }
    }
}