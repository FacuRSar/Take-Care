using System;
using UnityEngine;

public class Obj_Damage : MonoBehaviour
{
    [Header("Damage Settings")]

    [SerializeField] private int damage;


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(damage);
        }
    }

}
