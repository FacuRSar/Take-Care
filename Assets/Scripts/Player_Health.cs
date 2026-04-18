using System;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    [Header("Player Health Settings")]

    [SerializeField] private int MaxHealth;
    [SerializeField] private int MinHealth;
    [SerializeField] private int CurrentHealth;

    
    private int damage;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TakeHit();
    }


    private void TakeHit()
    {
        int TemporalyHealth = CurrentHealth - damage;

        TemporalyHealth = Math.Clamp(TemporalyHealth, MinHealth, MaxHealth); 

        CurrentHealth = TemporalyHealth;

        if (CurrentHealth == 0)
        {
          Destroy(gameObject);
        }   
    }


}
