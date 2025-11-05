using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace Unity.Assets.Scripts.Objects
{
    public enum MovementState
    {
        Idle = 0,
        PathFollowing = 1,
        Charging = 2,
        Knockback = 3,
    }

    /// <summary>
    /// Component responsible for moving a character on the server side based on inputs.
    /// </summary>
    /*[RequireComponent(typeof(NetworkCharacterState), typeof(NavMeshAgent), typeof(ServerCharacter)), RequireComponent(typeof(Rigidbody))]*/
    public class ServerCharacterMovement : NetworkBehaviour
    {
        // [SerializeField]
        // NavMeshAgent m_NavMeshAgent;

        // [SerializeField]
        // Rigidbody m_Rigidbody;

        // private NavigationSystem m_NavigationSystem;

        // private DynamicNavPath m_NavPath;

        private MovementState m_MovementState;

        // MovementStatus m_PreviousState;

        // [SerializeField]
        // private ServerCharacter m_CharLogic;

        // when we are in charging and knockback mode, we use these additional variables
        private float m_ForcedSpeed;
        private float m_SpecialModeDurationRemaining;


        void Awake()
        {
            // disable this NetworkBehavior until it is spawned
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Only enable server component on servers
                enabled = true;

                // On the server enable navMeshAgent and initialize
                // m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
                // m_NavPath = new DynamicNavPath(m_NavMeshAgent, m_NavigationSystem);
            }
        }



        /// <summary>
        /// Returns true if the character is actively moving, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsMoving()
        {
            return m_MovementState != MovementState.Idle;
        }

        /// <summary>
        /// Cancels any moves that are currently in progress.
        /// </summary>
        public void CancelMove()
        {
            // if (m_NavPath != null)
            // {
            //     m_NavPath.Clear();
            // }
            m_MovementState = MovementState.Idle;
        }


        private void FixedUpdate()
        {
            // PerformMovement();

            // var currentState = GetMovementStatus(m_MovementState);
            // if (m_PreviousState != currentState)
            // {
            //     m_CharLogic.MovementStatus.Value = currentState;
            //     m_PreviousState = currentState;
            // }
        }

        public override void OnNetworkDespawn()
        {
            // if (m_NavPath != null)
            // {
            //     m_NavPath.Dispose();
            // }
            if (IsServer)
            {
                // Disable server components when despawning
                enabled = false;
            }
        }



        /// <summary>
        /// Determines the appropriate MovementStatus for the character. The
        /// MovementStatus is used by the client code when animating the character.
        /// </summary>
        // private MovementStatus GetMovementStatus(MovementState movementState)
        // {
        //     switch (movementState)
        //     {
        //         case MovementState.Idle:
        //             return MovementStatus.Idle;
        //         case MovementState.Knockback:
        //             return MovementStatus.Uncontrolled;
        //         default:
        //             return MovementStatus.Normal;
        //     }
        // }
    }
}
