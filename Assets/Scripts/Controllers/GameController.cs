using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private GameStateController gameStateController;
    private DollState currentState;
    private PlayerSanity playerSanity;
    private DollEmotionSystem dollEmotionSystem;
    private void Awake()
    {
        gameStateController = GetComponent<GameStateController>();
    }
    public void Play()
    {
        SceneManager.LoadScene("Play");
    }
    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void setPlayerSanity(PlayerSanity playerSanity)
    {
        this.playerSanity = playerSanity;
    }
    public void setDollEmotionSystem(DollEmotionSystem dollEmotionSystem)
    {
        this.dollEmotionSystem = dollEmotionSystem;
    }
}
