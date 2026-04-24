using UnityEngine;

public class Quest : GrabbableObject
{
    [SerializeField] private string description;
    private bool isCompleted;
    private float timer;
    private bool isActive;
    [SerializeField] private int timerDuration = 4;
    [SerializeField] private Player_Health player;

    private void Awake()
    {
        setIsCompleted(false);
        setActive(false);
    }
    private void FixedUpdate()
    {
        if (isActive)
        {
            timer -= 1;
            if (timer==(timerDuration/4) && !isCompleted)
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
        setIsCompleted(false);
        isActive = newIsActive;
        if (isActive)
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
         timer = timerDuration;
    }
    public float getTimer()
    {
         return timer;
    }
    public void markObjective()
    {
        GetComponent<MeshRenderer>().material.color = Color.red;
    }
    public void failQuest()
    {
        player.TakeDamage(1);
    }
}