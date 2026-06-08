using System.Collections.Generic;
using UnityEngine;
public class Animations : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] PlayerMovement player;


    [Header("List")]

    List<Animator> AnimatorList = new List<Animator>();
    void Start()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None); // Encuentra todos los objetos con el componente Animator

        foreach (var animator in animators) // Agrega cada Animator a la lista
        {
            AnimatorList.Add(animator);
        }
    }

    // Update is called once per frame
    void Update()
    {
        AnimationsCharacters();

    }

    void AnimationsCharacters()
    {
        
        foreach (var anim in AnimatorList)
        {
          if (anim != null)
          {
                // Aca agregar los componentes
                //                       ||
                //                       ||    
                //                       \/    
                // por ej: player = anim.GetComponent<Player>(); y si asi no podes angel, sos un boludo (Mentira)

                player = anim.GetComponent<PlayerMovement>();

                //aca los separar por Referencia
                //                       ||
                //                       ||    
                //                       \/    
                // por ej: if(player != null)
                //{
                //  anim.SetFloat("Run", player.x);
                //}

                if (player != null)
                {
                    anim.SetBool("WalkX", player.movingX);
                    anim.SetBool("WalkY", player.movingY);
                    anim.SetBool("Sprint", player.IsSprinting);
                    anim.SetBool("Crouch", player.IsCrouching);
                }


            }
        }
    }
}
