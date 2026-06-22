using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Animations : MonoBehaviour
{
    [Header("Animators (arrastrar desde el inspector)")]
    [SerializeField] private Animator[] animators;

    private readonly List<AnimationTarget> targets = new List<AnimationTarget>();

    private void Awake()
    {
        if (animators == null)
        {
            return;
        }

        foreach (Animator animator in animators)
        {
            RegisterAnimator(animator);
        }
    }

    // Para scripts que spawnean despues o no estan en la lista del inspector.
    public void RegisterAnimator(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimationTarget existing in targets)
        {
            if (existing.Animator == animator)
            {
                return;
            }
        }

        targets.Add(new AnimationTarget(animator));
    }

    private void Update()
    {
        AnimationsCharacters();
    }

    private void AnimationsCharacters()
    {
        foreach (AnimationTarget target in targets)
        {
            if (target.Animator == null)
            {
                continue;
            }

            Animator anim = target.Animator;

            // Aca agregar los componentes
            //                       ||
            //                       ||    
            //                       \/
            // por ej: player = anim.GetComponent<Player>(); y si asi no podes angel, sos un boludo (Mentira)

            // aca los separar por Referencia
            //                       ||
            //                       ||    
            //                       \/
            // por ej: if(player != null)
            //{
            //  anim.SetFloat("Run", player.x);
            //}

            if (target.Player != null)
            {
                anim.SetBool("isMoving", target.Player.IsMoving);

                if (target.PlayerInteraction != null && target.PlayerInteraction.ConsumeInteractAnimationRequest())
                {
                    anim.SetTrigger("Interact");
                }
            }

            if (target.IsPursuerActive())
            {
                // Speed maneja Idle/Walk en PursuerAnimator: Walk si Speed > 0.1, Idle si no.
                if (target.Agent != null)
                {
                    anim.SetFloat("Speed", target.Agent.velocity.magnitude, 0.1f, Time.deltaTime);
                }

                if (target.ConsumeAttackRequest())
                {
                    anim.SetTrigger("Attack");
                }
            }
        }
    }

    private class AnimationTarget
    {
        public Animator Animator { get; }
        public PlayerMovement Player { get; }
        public PlayerInteraction PlayerInteraction { get; }
        public NavMeshAgent Agent { get; }
        public PursuerNavMeshController Chase { get; }
        public PursuerPatrolController Patrol { get; }

        public AnimationTarget(Animator animator)
        {
            Animator = animator;
            Player = animator.GetComponentInParent<PlayerMovement>();
            PlayerInteraction = animator.GetComponentInParent<PlayerInteraction>();
            Agent = animator.GetComponent<NavMeshAgent>();
            Chase = animator.GetComponent<PursuerNavMeshController>();
            Patrol = animator.GetComponent<PursuerPatrolController>();
        }

        public bool IsPursuerActive()
        {
            if (Animator == null || !Animator.gameObject.activeInHierarchy)
            {
                return false;
            }

            return (Chase != null && Chase.enabled) || (Patrol != null && Patrol.enabled);
        }

        public bool ConsumeAttackRequest()
        {
            if (Chase != null && Chase.enabled && Chase.ConsumeAttackRequest())
            {
                return true;
            }

            if (Patrol != null && Patrol.enabled && Patrol.ConsumeAttackRequest())
            {
                return true;
            }

            return false;
        }
    }
}
