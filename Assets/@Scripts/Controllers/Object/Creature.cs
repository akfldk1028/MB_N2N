// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering;
// using static Define;
// using VContainer;
// using Unity.Assets.Scripts.Data;
// using Unity.Netcode;
// using Unity.VisualScripting;

// namespace Unity.Assets.Scripts.Objects
// {

    
// public class Creature : BaseObject, ITargetable
// {
// 	public CharacterTypeEnum CreatureType { get; protected set; } = CharacterTypeEnum.None;
	
// 	[SerializeField]
// 	private bool _isNpc = false; // NPC 여부를 나타내는 필드 (private으로 변경)
// 	private bool _isValidTarget = false; // NPC 여부를 나타내는 필드 (private으로 변경)

// 	// ITargetable 인터페이스 구현
// 	public bool IsNpc 
// 	{ 
// 		get { return _isNpc; } 
// 		set { _isNpc = value; }
// 	}
// 	public SkillComponent Skills { get; protected set; }
// 	public BaseObject Target { get; protected set; }

//     public bool IsValidTarget => LifeState != LifeState.Dead;

// 	// public EffectComponent Effects { get; set; }

// 	#region Stats
// 	public float Hp { get; set; }
	
// 	[Header("===== 기본 스탯 =====")]
// 	[Space(5)]
//     [SerializeField]protected Guid creatureGuid; // 캐릭터의 GUID
// 	[SerializeField] public CreatureStat MaxHp = new CreatureStat(0);
// 	[SerializeField] public CreatureStat Atk = new CreatureStat(0);
// 	[SerializeField] public CreatureStat AtkRange = new CreatureStat(0);      
// 	[SerializeField] public CreatureStat AtkBonus = new CreatureStat(0);
// 	[SerializeField] public CreatureStat MoveSpeed = new CreatureStat(0);
// 	[SerializeField] public CreatureStat CriRate = new CreatureStat(0);
// 	[SerializeField] public CreatureStat CriDamage = new CreatureStat(0);
// 	[SerializeField] public CreatureStat ReduceDamageRate = new CreatureStat(0);
	

//     public CreatureStat LifeStealRate;
//     public CreatureStat ThornsDamageRate; // 쏜즈
//     public CreatureStat AttackSpeedRate;
//     public NetworkLifeState NetLifeState { get; private set; }

//  	//ECreatureState 와 LifeState 통합해야함
// 	public LifeState LifeState
//         {
//             get => NetLifeState.LifeState.Value;
//             private set => NetLifeState.LifeState.Value = value;
//         }

// 	protected NetworkVariable<ECreatureState> _creatureState = new NetworkVariable<ECreatureState>(ECreatureState.None);

// 	public NetworkVariable<ECreatureState> NetworkCreatureState => _creatureState;

//     private ECreatureState _pendingCreatureState = ECreatureState.None;
//     public virtual ECreatureState CreatureState
//     {
//         get { return _creatureState.Value; }
//         set
//         {
//             if (_creatureState.Value != value)
//             {
//                 _pendingCreatureState = value;
//                 if (IsSpawned && IsServer) // 스폰된 경우에만 NetworkVariable 수정
//                 {
//                     _creatureState.Value = value;
//                     OnCreatureStateChanged(_creatureState.Value);
//                 }
//             }
//         }
//     }

//     public override void OnNetworkSpawn()
//     {
//         base.OnNetworkSpawn();
        
//         if (IsServer && _pendingCreatureState != ECreatureState.None)
//         {
//             _creatureState.Value = _pendingCreatureState;
//             OnCreatureStateChanged(_creatureState.Value);
//         }
//     }
//         #endregion


    
// #if UNITY_EDITOR
//         [Header("===== 디버그 정보 =====")]
//         [Space(5)]
//         [SerializeField] private float _dbgMaxHp;
//         [SerializeField] private float _dbgAtk;
//         [SerializeField] private float _dbgAtkRange;
//         [SerializeField] private float _dbgAtkBonus;
//         [SerializeField] private float _dbgMoveSpeed;
//         [SerializeField] private float _dbgCriRate;
//         [SerializeField] private float _dbgCriDamage;
// #endif
//         public virtual void Update()
//         {
// #if UNITY_EDITOR
//             // 런타임에만 디버그 정보 업데이트
//             if (Application.isPlaying)
//             {
//                 _dbgMaxHp = MaxHp.Value;
//                 _dbgAtk = Atk.Value;
//                 _dbgAtkRange = AtkRange.Value;
//                 _dbgAtkBonus = AtkBonus.Value;
//                 _dbgMoveSpeed = MoveSpeed.Value;
//                 _dbgCriRate = CriRate.Value;
//                 _dbgCriDamage = CriDamage.Value;
//             }
// #endif

//             // 기존 Update 코드 (있다면)
//         }

//         protected void Awake()
//         {
//             ObjectType = EObjectType.Creature;
// 			NetLifeState = GetComponent<NetworkLifeState>();
//         }

// 	        public override void OnNetworkDespawn()
//         {
//             // if (IsServer)
//             // {
//             //     NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
//             //     m_DamageReceiver.DamageReceived -= ReceiveHP;
//             //     m_DamageReceiver.CollisionEntered -= CollisionEntered;
//             // }
//         }
     


// 	public override bool Init()
// 	{
// 		if (base.Init() == false)
// 			return false;

// 		Debug.Log($"<color=blue>[Creature] Init: 현재 CreatureType = {CreatureType}</color>");
// 		return true;
// 	}

//     public virtual void SetInfo<T>(int templateID, Data.CreatureData creatureData, T clientCreature) 
//     where T : class    {
// 		DataTemplateID = templateID;
//         Debug.Log($"<color=blue>[Creature] SetInfo 시작: 현재 CreatureType = {CreatureType}</color>");
//         Hp = creatureData.MaxHp;
//         MaxHp = new CreatureStat(creatureData.MaxHp);
//         Atk = new CreatureStat(creatureData.Atk);
//         CriRate = new CreatureStat(creatureData.CriRate);
//         CriDamage = new CreatureStat(creatureData.CriDamage);
//         ReduceDamageRate = new CreatureStat(0);
//         LifeStealRate = new CreatureStat(0);
//         ThornsDamageRate = new CreatureStat(0);
//         MoveSpeed = new CreatureStat(creatureData.MoveSpeed);
//         AttackSpeedRate = new CreatureStat(1);
//         AtkRange = new CreatureStat(creatureData.AtkRange);
//         AtkBonus = new CreatureStat(creatureData.AtkBonus);
//         CriRate = new CreatureStat(creatureData.CriRate);

//         Debug.Log($"<color=blue>[Creature] SetInfo 완료: 현재 CreatureType = {CreatureType}</color>");
        
//         CreatureState = ECreatureState.Idle;

//         try
//         {
//                 Skills = gameObject.GetComponent<SkillComponent>();
//                 if (Skills == null)
//                 {
//                     Skills = gameObject.AddComponent<SkillComponent>();
//                     Debug.Log($"[{name}] SkillComponent가 추가되었습니다.");
//                 }
                
//                 Skills.SetInfo(this, creatureData, clientCreature as ClientCreature);
//         }
//         catch (System.Exception e)
//         {
//                 Debug.LogError($"[{name}] SkillComponent 추가 중 오류: {e.Message}");
//         }

//         if (Collider != null)
//         {
//             Collider.offset = new Vector2(creatureData.ColliderOffsetX, creatureData.ColliderOffsetY);
//             Collider.radius = creatureData.ColliderRadius;
//         }


//             // // RigidBody 추가	
//             // RigidBody.mass = 0;


//             // // if (CreatureData.GetType().GetProperty("IsValidTarget") != null)
//             // // {
//             // // 	IsValidTarget = (bool)CreatureData.GetType().GetProperty("IsValidTarget").GetValue(CreatureData);
//             // // }

//             // // IsValidTarget = LifeState != LifeState.Dead;


//             //// Effect
//             //Effects = gameObject.AddComponent<EffectComponent>();
//             //Effects.SetInfo(this);

//             //// Map
//             //StartCoroutine(CoLerpToCellPos());
//         }

//     protected virtual void OnCreatureStateChanged(ECreatureState newState)
// 	{
// 		// Client로 대충 이동함
// 		// 서버에서 상태 변경 시 필요한 로직
// 	}
//     protected virtual void UpdateAnimation(){}


// 	float DistToTargetSqr
// 	{
// 		get
// 		{
// 			Vector3 dir = (Target.transform.position - transform.position);
// 			float distToTarget = Math.Max(0, dir.magnitude - Target.ExtraCells * 1f - ExtraCells * 1f); // TEMP
// 			return distToTarget * distToTarget;
// 		}
// 	}


//     // Creature.cs에서 수정해야 할 ChaseOrAttackTarget 함수
//     protected void ChaseOrAttackTarget(float chaseRange, float attackRange)
//     {
//         if (Target == null) return;
        
//         float distToTargetSqr = DistToTargetSqr;
//         float attackDistanceSqr = attackRange * attackRange;
        
//         if (distToTargetSqr <= attackDistanceSqr)
//         {
//             // 공격 범위 이내로 들어왔다면 공격 상태로
//             if (IsServer)
//             {
//                 CreatureState = ECreatureState.Skill;
//             }
//         }
//         else
//         {
//             // 공격 범위 밖이면 계속 이동 상태를 유지하며 추적
//             // 실제 이동 로직은 별도 구현 필요
//         }
//     }

//    public float UpdateAITick { get; protected set; } = 0.0f;
//    protected IEnumerator CoUpdateAI()
//         {
//             while (true)
//             {
//                 switch (CreatureState)
//                 {
//                     case ECreatureState.Idle:
//                         UpdateIdle();
//                         break;
//                     case ECreatureState.Move:
//                         UpdateMove();
//                         break;
//                     case ECreatureState.Skill:
//                         UpdateSkill();
//                         break;
//                     case ECreatureState.OnDamaged:
//                         UpdateOnDamaged();
//                         break;
//                     case ECreatureState.Dead:
//                         UpdateDead();
//                         break;
//                 }

//                 if (UpdateAITick > 0)
//                     yield return new WaitForSeconds(UpdateAITick);
//                 else
//                     yield return null;
//             }
//         }



//         // protected BaseObject FindClosestInRange(Vector3 centerPosition, float range, IEnumerable<BaseObject> objs, Func<BaseObject, bool> func = null)
//         // {
//         //     BaseObject target = null;
//         //     float bestDistanceSqr = float.MaxValue;
//         //     float searchDistanceSqr = range * range;

//         //     foreach (BaseObject obj in objs)
//         //     {
//         //         Vector3 dir = obj.transform.position - centerPosition;
//         //         float distToTargetSqr = dir.sqrMagnitude;

//         //         if (distToTargetSqr > searchDistanceSqr)
//         //             continue;

//         //         if (distToTargetSqr > bestDistanceSqr)
//         //             continue;

//         //         if (func != null && func.Invoke(obj) == false)
//         //             continue;

//         //         target = obj;
//         //         bestDistanceSqr = distToTargetSqr;
//         //     }

//         //     return target;
//         // }

//         protected virtual void UpdateIdle() { }
//    		protected virtual void UpdateMove() { }

//         protected virtual void UpdateSkill() { }

//         protected virtual void UpdateOnDamaged() { }

//         protected virtual void UpdateDead() { }
       
//        	public override void OnDamaged(BaseObject attacker, SkillBase skill)
// 	    {
// 		base.OnDamaged(attacker, skill);

// 		if (attacker.IsValid() == false)
// 			return;
// 		Creature creature = attacker as Creature;
// 		if (creature == null)
// 			return;

// 		float finalDamage = creature.Atk.Value;
// 		Hp = Mathf.Clamp(Hp - finalDamage, 0, MaxHp.Value);

// 		_objectManager.ShowDamageFont(CenterPosition, finalDamage, transform, false);

// 		if (Hp <= 0)
// 		{
// 			OnDead(attacker, skill);
// 			CreatureState = ECreatureState.Dead;
// 			return;
// 		}

// 		// // 스킬에 따른 Effect 적용
// 		// if (skill.SkillData.EffectIds != null)
// 		// 	Effects.GenerateEffects(skill.SkillData.EffectIds.ToArray(), EEffectSpawnType.Skill, skill);

// 		// // AOE
// 		// if (skill != null && skill.SkillData.AoEId != 0)
// 		// 	skill.GenerateAoE(transform.position);
// 	}

//         public override void OnDead(BaseObject attacker, SkillBase skill)
//         {
//             base.OnDead(attacker, skill);
//         }
       
       
       
//         // protected virtual void UpdateSkill()
//         // {
//         //     //if (_coWait != null)
//         //     //    return;

//         //     //if (Target.IsValid() == false || Target.ObjectType == EObjectType.HeroCamp)
//         //     //{
//         //     //    CreatureState = ECreatureState.Idle;
//         //     //    return;
//         //     //}

//         //     //float distToTargetSqr = DistToTargetSqr;
//         //     //float attackDistanceSqr = AttackDistance * AttackDistance;
//         //     //if (distToTargetSqr > attackDistanceSqr)
//         //     //{
//         //     //    CreatureState = ECreatureState.Idle;
//         //     //    return;
//         //     //}

//         //     //// DoSkill
//         //     //Skills.CurrentSkill.DoSkill();

//         //     //LookAtTarget(Target);

//         //     //var trackEntry = SkeletonAnim.state.GetCurrent(0);
//         //     //float delay = trackEntry.Animation.Duration;

//         //     //StartWait(delay);
//         // }



//     /// <summary>
//     /// 가장 가까운 적을 찾는 메서드 (2D)
//     /// </summary>
//     /// <param name="origin">탐지 시작 위치</param>
//     /// <param name="radius">탐지 범위</param>
//     /// <param name="targetLayer">대상 레이어</param>
//     /// <param name="debugDraw">디버그 시각화 여부</param>
//     /// <returns>가장 가까운 BaseObject</returns>
//     public BaseObject FindNearestTarget2D(Vector3 origin, float radius, int targetLayer)
//     {
//         // 레이어 마스크 생성
//         int layerMask = 1 << targetLayer;
        
//         Debug.Log($"[Creature] FindNearestTarget2D - 레이어: {targetLayer}, 마스크: {layerMask}, 범위: {radius}");
//         Debug.Log($"[Creature] FindNearestTarget2D - 시작 위치: {origin}");
        
        
//         // 범위 내 모든 콜라이더 찾기
//         Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(origin, radius, layerMask);
        
//         Debug.Log($"[Creature] FindNearestTarget2D - 범위 내 대상 수: {targetsInRange.Length}");
        
        
//         // 가장 가까운 대상 찾기
//         BaseObject nearestTarget = null;
//         float closestDistance = float.MaxValue;
        
//         foreach (Collider2D targetCollider in targetsInRange)
//         {
//             if (targetCollider == null || targetCollider.gameObject == null) continue;
            
//             BaseObject targetObject = targetCollider.GetComponent<BaseObject>();
//             if (targetObject == null || !targetObject.IsValid() || targetObject == this) continue;
            
//             float distance = Vector2.Distance(origin, targetCollider.transform.position);
            
//             Debug.Log($"[Creature] FindNearestTarget2D - 대상 발견: {targetCollider.name}, 거리: {distance}");
            
            
//             if (distance < closestDistance)
//             {
//                 closestDistance = distance;
//                 nearestTarget = targetObject;
//             }
//         }
        
//         return nearestTarget;
//     }

//     /// <summary>
//     /// 가장 가까운 적을 찾는 메서드 (3D)
//     /// </summary>
//     /// <param name="origin">탐지 시작 위치</param>
//     /// <param name="radius">탐지 범위</param>
//     /// <param name="targetLayer">대상 레이어</param>
//     /// <param name="debugDraw">디버그 시각화 여부</param>
//     /// <returns>가장 가까운 BaseObject</returns>
//     public BaseObject FindNearestTarget3D(Vector3 origin, float radius, int targetLayer)
//     {
//         // 레이어 마스크 생성
//         int layerMask = 1 << targetLayer;
        
//         Debug.Log($"[Creature] FindNearestTarget3D - 레이어: {targetLayer}, 마스크: {layerMask}, 범위: {radius}");
//         Debug.Log($"[Creature] FindNearestTarget3D - 시작 위치: {origin}");
        
//         // 범위 내 모든 콜라이더 찾기
//         Collider[] targetsInRange = Physics.OverlapSphere(origin, radius, layerMask);
//         Debug.Log($"[Creature] FindNearestTarget3D - 범위 내 대상 수: {targetsInRange.Length}");

        
//         // 가장 가까운 대상 찾기
//         BaseObject nearestTarget = null;
//         float closestDistance = float.MaxValue;
        
//         foreach (Collider targetCollider in targetsInRange)
//         {
//             if (targetCollider == null || targetCollider.gameObject == null) continue;
            
//             BaseObject targetObject = targetCollider.GetComponent<BaseObject>();
//             if (targetObject == null || !targetObject.IsValid() || targetObject == this) continue;
            
//             float distance = Vector3.Distance(origin, targetCollider.transform.position);
            

//             Debug.Log($"[Creature] FindNearestTarget3D - 대상 발견: {targetCollider.name}, 거리: {distance}");
            
//             if (distance < closestDistance)
//             {
//                 closestDistance = distance;
//                 nearestTarget = targetObject;
//             }
//         }
        
//         return nearestTarget;
//     }

//     /// <summary>
//     /// 타깃 필터링 옵션
//     /// </summary>
//     [System.Flags]
//     public enum TargetFilterOptions
//     {
//         None = 0,
//         NeedsLineOfSight = 1 << 0,     // 시야 확인 필요
//         MustBeAlive = 1 << 1,          // 살아있는 대상만
//         IgnoreSelf = 1 << 2            // 자기 자신 무시
//     }

//     /// <summary>
//     /// 범위 내 모든 타겟 찾기 (옵션 필터링 지원)
//     /// </summary>
//     /// <param name="origin">탐지 시작 위치</param>
//     /// <param name="radius">탐지 범위</param>
//     /// <param name="targetLayer">대상 레이어</param>
//     /// <param name="filterOptions">필터링 옵션</param>
//     /// <param name="maxResults">최대 결과 수 (0 = 무제한)</param>
//     /// <returns>범위 내 타겟 리스트</returns>
//     public List<BaseObject> FindTargetsInRange(Vector3 origin, float radius, int targetLayer, 
//                                                TargetFilterOptions filterOptions = TargetFilterOptions.IgnoreSelf | TargetFilterOptions.MustBeAlive, 
//                                                int maxResults = 0)
//     {
//         // 레이어 마스크 생성
//         int layerMask = 1 << targetLayer;
//         List<BaseObject> results = new List<BaseObject>();
        
//         // 2D 또는 3D 기반으로 적절한 콜라이더 검색 메서드 사용
//         if (Physics2D.OverlapCircleNonAlloc(origin, 0.1f, new Collider2D[1], layerMask) > 0)
//         {
//             // 2D 환경으로 판단
//             Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(origin, radius, layerMask);
            
//             foreach (Collider2D targetCollider in targetsInRange)
//             {
//                 if (targetCollider == null || targetCollider.gameObject == null) continue;
                
//                 BaseObject targetObject = targetCollider.GetComponent<BaseObject>();
//                 if (targetObject == null) continue;
                
//                 // 필터링 적용
//                 if ((filterOptions & TargetFilterOptions.IgnoreSelf) != 0 && targetObject == this) continue;
//                 if ((filterOptions & TargetFilterOptions.MustBeAlive) != 0 && !targetObject.IsValid()) continue;
                
//                 // 시야 확인이 필요한 경우
//                 if ((filterOptions & TargetFilterOptions.NeedsLineOfSight) != 0)
//                 {
//                     Vector2 direction = targetCollider.transform.position - origin;
//                     RaycastHit2D hit = Physics2D.Raycast(origin, direction, radius, layerMask);
                    
//                     if (hit.collider != targetCollider) continue;
//                 }
                
//                 results.Add(targetObject);
                
//                 // 최대 결과 수 체크
//                 if (maxResults > 0 && results.Count >= maxResults) break;
//             }
//         }
        
//         return results;
//     }

//     /// <summary>
//     /// 공격 범위 내에 있는 가장 가까운 타겟 찾기 (간편 버전)
//     /// </summary>
//     /// <param name="targetLayer">대상 레이어</param>
//     /// <param name="rangeMultiplier">AtkRange의 배수 (기본값: 1)</param>
//     /// <param name="debugDraw">디버그 표시 여부</param>
//     /// <returns>가장 가까운 타겟</returns>
//     public BaseObject FindNearestTargetInAttackRange(int targetLayer, float rangeMultiplier = 1f)
//     {
//         float searchRange = AtkRange.Value * rangeMultiplier;
        

//         Debug.Log($"[Creature] FindNearestTargetInAttackRange - 레이어: {targetLayer}, 범위: {searchRange}");
        
//         return FindNearestTarget2D(transform.position, searchRange, targetLayer);
//     }
    
//     /// <summary>
//     /// 공격 범위 내에 있는 모든 타겟 찾기 (간편 버전)
//     /// </summary>
//     /// <param name="targetLayer">대상 레이어</param>
//     /// <param name="rangeMultiplier">AtkRange의 배수 (기본값: 1)</param>
//     /// <param name="maxResults">최대 결과 수 (0 = 무제한)</param>
//     /// <returns>공격 범위 내 타겟 목록</returns>
//     public List<BaseObject> FindAllTargetsInAttackRange(int targetLayer, float rangeMultiplier = 1f, int maxResults = 0)
//     {
//         float searchRange = AtkRange.Value * rangeMultiplier;
//         return FindTargetsInRange(
//             transform.position,
//             searchRange,
//             targetLayer,
//             TargetFilterOptions.IgnoreSelf | TargetFilterOptions.MustBeAlive,
//             maxResults
//         );
//     }

// }
// }