using UnityEngine;
using System.Collections;

public class Quest : GrabbableObject
{
    [SerializeField] private string description;
    private bool isCompleted;
    private float timer;
    private bool isActive;
    [SerializeField] private int timerDuration = 4;
    [SerializeField] private Player_Health playerHealth;
    private Renderer rend;
    private Material originalMaterial;
    [SerializeField] private Material transparentMaterial;

    private void Awake()
    {
        setIsCompleted(false);
        setActive(false);
        rend = GetComponent<MeshRenderer>();
        originalMaterial = GetComponent<MeshRenderer>().material;
    }
    private void Update()
    {
        if(isActive)
        {
            timer += Time.deltaTime;
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
         timer = 0;
    }
    public float getTimer()
    {
        return timer;
    }
    public bool checkTimer()
    {
        if(getTimer() >= timerDuration)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public float getTimerDuration()
    {
        return timerDuration;
    }
    IEnumerator TempVisibility()
    {
        rend.material = transparentMaterial;
        yield return new WaitForSeconds(2);
        rend.material = originalMaterial;
    }
    public void failQuest()
    {
        playerHealth.TakeDamage(1);
    }
    public void markObjective()
    {
        StartCoroutine(TempVisibility());
    }
}