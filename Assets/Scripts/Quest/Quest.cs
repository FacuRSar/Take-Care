using UnityEngine;

public class Quest : MonoBehaviour
{
    [SerializeField] private string description;
    private bool isCompleted;
    private float timer;
    private bool isActive;
    [SerializeField] private int timerDuration = 4; // Duración del temporizador en segundos
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void FixedUpdate()
    {
        if (isActive)
        {
            timer -= Time.deltaTime; // Decrementa el temporizador con el tiempo transcurrido
            if (timer==timerDuration/4 && !isCompleted)
            {
                markObjective();
            }
        }
    }
    public string getDescription()
    {
        if(description != null)
            return description;
        else
            return "No description";
    }
    public bool getIsCompleted()
    {
        return isCompleted;
    }
    public void setDescription(string newDescription)
    {
        description = newDescription;
    }
    public void setIsCompleted(bool newIsCompleted)
    {
        isCompleted = newIsCompleted;
    }
    public void setActive(bool newIsActive)
    {
        isActive = newIsActive;
        if(isActive)
        {
            setTimer();
        }
    }
    public bool getIsActive()
    {
         return isActive;
    }
    public void setTimer()
    {
         timer = 4;
    }
    public float getTimer()
    {
         return timer;
    }
    public void markObjective()
    {
        
    }
}