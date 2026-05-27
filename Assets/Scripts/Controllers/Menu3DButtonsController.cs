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
        Exit
    }

    [Header("Camara")]
    [SerializeField] private Camera menuCamera;

    [Header("Botones")]
    [SerializeField] private MenuButtonData[] buttons;

    [Header("Escenas")]
    [SerializeField] private string introSceneName = "Intro";

    [Header("Raycast")]
    [SerializeField] private LayerMask buttonMask;
    [SerializeField] private float rayDistance = 100f;

    private Menu3DButtonVisual currentVisual;
    private MenuButtonData currentButton;

    private void Awake()
    {
        if (menuCamera == null)
        {
            menuCamera = Camera.main;
        }
    }

    private void Update()
    {
        CheckButtons();
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
                SceneManager.LoadScene(introSceneName);
                break;

            case MenuButtonAction.Credits:
                Debug.Log("Creditos todavia no implementado.");
                break;

            case MenuButtonAction.Exit:
                Application.Quit();
                Debug.Log("Salir del juego.");
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