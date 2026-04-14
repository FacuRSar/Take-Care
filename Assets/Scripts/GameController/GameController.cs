using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Play");
    }
    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
}
