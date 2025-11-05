using System.Collections;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
// using Action = Unity.Assets.Scripts.Gameplay.Actions.Action;
using System.Linq;
using System;
// using Unity.Assets.Scripts.Pooling;
using Unity.VisualScripting;
using static Define;
// using Spine.Unity;

namespace Unity.Assets.Scripts.Objects
{
  
    // [RequireComponent(typeof(NetworkHealthState),
    //     typeof(NetworkLifeState),
    //     typeof(NetworkAvatarGuidState))]
    public abstract class BaseObject : NetworkBehaviour
    {


        // // ITargetable 인터페이스 구현
        // private bool _isNpc = false;

        // /// <summary>
        // /// 이 객체가 NPC인지 여부를 나타냅니다.
        // /// </summary>
        // public bool IsNpc 
        // { 
        //     get { return _isNpc; } 
        //     set { _isNpc = value; }
        // }

        // private bool _isValidTarget = true;

        // /// <summary>
        // /// 이 객체가 현재 유효한 타겟인지 여부를 나타냅니다.
        // /// </summary>
        // public bool IsValidTarget
        // {
        //     get { return _isValidTarget; }
        //     set { _isValidTarget = value; }
        // }

        // [SerializeField]
   
        // public NetworkVariable<bool> IsStealthy { get; } = new NetworkVariable<bool>();


        // public NetworkVariable<ulong> TargetId { get; } = new NetworkVariable<ulong>();



        /// <summary>
        /// 캐릭터의 생명 상태를 관리
        /// 디펜스 게임에서: 타워나 유닛의 파괴/생존 상태를 관
        /// </summary>
        // public NetworkLifeState NetLifeState { get; private set; }

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        // public LifeState LifeState
        // {
        //     get => NetLifeState.LifeState.Value;
        //     private set => NetLifeState.LifeState.Value = value;
        // }

        /// <summary>
        /// Returns true if this Character is an NPC.
        /// </summary>
        // public bool IsNpc => GetCreatureStatsSO() != null && GetCreatureStatsSO().IsNpc;


  


        // public int ExtraCells { get; set; } = 0;

	    public EObjectType ObjectType { get; protected set; } = EObjectType.None;
        // public CircleCollider2D Collider { get; private set; }
        // public Rigidbody2D RigidBody { get; private set; }
        // private HurtFlashEffect HurtFlash;
	    // public SkeletonAnimation SkeletonAnim { get; private set; }

        // public float ColliderRadius { get { return Collider != null ? Collider.radius : 0.0f; } }
        // public Vector3 CenterPosition { get { return transform.position + Vector3.up * ColliderRadius; } }

        public int DataTemplateID { get; set; }

        // bool _lookLeft = true;
        // public bool LookLeft
        // {
        //     get { return _lookLeft; }
        //     set
        //     {
        //         _lookLeft = value;
        //         // Flip(!value);
        //     }
        // }
        // 엥 코드욀케 개판임? TODO 정리리

        protected virtual void Awake(){}


        // public virtual void OnDamaged(BaseObject attacker, SkillBase skill)
        // {
        //     // HurtFlash.Flash();
        // }

        // public virtual void OnDead(BaseObject attacker, SkillBase skill)
        // {
        //     // HurtFlash.Flash();
        // }
        // public void Flip(bool flag)
        // {
        //     //Sprite 도 추가향함    
        //     if (SkeletonAnim == null)
        //         return;

        //     SkeletonAnim.Skeleton.ScaleX = flag ? -1 : 1;
        // }
        // public void LookAtTarget(BaseObject target)
        // {
        //     Vector2 dir = target.transform.position - transform.position;
        //     if (dir.x < 0)
        //         LookLeft = true;
        //     else
        //         LookLeft = false;
        // }

       

       


        public virtual bool Init()
        {
            // Collider = gameObject.GetComponent<CircleCollider2D>();
            // SkeletonAnim = GetComponent<SkeletonAnimation>();
            // RigidBody = GetComponent<Rigidbody2D>();
		    // HurtFlash = gameObject.GetOrAddComponent<HurtFlashEffect>();
            return true;
        }

        protected virtual void OnDisable(){}





        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // if (IsServer)
            // {
            //     NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            //     m_DamageReceiver.DamageReceived -= ReceiveHP;
            //     m_DamageReceiver.CollisionEntered -= CollisionEntered;
            // }
        }

        // protected virtual void UpdateAnimation(){}
        // public virtual void OnAnimEventHandler(){}

        // public void AddAnimation(int trackIndex, string AnimName, bool loop, float delay){}
    





    }
}