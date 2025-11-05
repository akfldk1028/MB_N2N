using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;

public class Creature : BaseObject
{
	public BaseObject Target { get; protected set; }

	public Data.CreatureData CreatureData { get; private set; }


	float DistToTargetSqr
	{
		get
		{
			Vector3 dir = (Target.transform.position - transform.position);
			float distToTarget = Math.Max(0, dir.magnitude - Target.ExtraCells * 1f - ExtraCells * 1f); // TEMP
			return distToTarget * distToTarget;
		}
	}

	#region Stats
	public float Hp { get; set; }
	public CreatureStat MaxHp;
	public CreatureStat Atk;
	public CreatureStat CriRate;
	public CreatureStat CriDamage;
	public CreatureStat ReduceDamageRate;
	public CreatureStat LifeStealRate;
	public CreatureStat ThornsDamageRate; // 쏜즈
	public CreatureStat MoveSpeed;
	public CreatureStat AttackSpeedRate;
	#endregion

	protected float AttackDistance
	{
		get
		{
			float env = 2.2f;
			if (Target != null && Target.ObjectType == EObjectType.Env)
				return Mathf.Max(env, Collider.radius + Target.Collider.radius + 0.1f);

			float baseValue = CreatureData.AtkRange;
			return baseValue;
		}
	}

	protected ECreatureState _creatureState = ECreatureState.None;
	public virtual ECreatureState CreatureState
	{
		get { return _creatureState; }
		set
		{
			if (_creatureState != value)
			{
				_creatureState = value;
				UpdateAnimation();
			}
		}
	}

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		return true;
	}

	public virtual void SetInfo(int templateID)
	{
		DataTemplateID = templateID;

		if (ObjectType == EObjectType.Hero)
			CreatureData = Managers.Data.HeroDic[templateID];
		else
			CreatureData = Managers.Data.MonsterDic[templateID];

		gameObject.name = $"{CreatureData.DataId}_{CreatureData.DescriptionTextID}";

		// Collider
		Collider.offset = new Vector2(CreatureData.ColliderOffsetX, CreatureData.ColliderOffsetY);
		Collider.radius = CreatureData.ColliderRadius;

		// RigidBody
		RigidBody.mass = 0;

		// Spine
		SetSpineAnimation(CreatureData.SkeletonDataID, SortingLayers.CREATURE);

		// Skills
		// Skills = gameObject.GetOrAddComponent<SkillComponent>();
		// Skills.SetInfo(this, CreatureData);

		// Stat
		Hp = CreatureData.MaxHp;
		MaxHp = new CreatureStat(CreatureData.MaxHp);
		Atk = new CreatureStat(CreatureData.Atk);
		CriRate = new CreatureStat(CreatureData.CriRate);
		CriDamage = new CreatureStat(CreatureData.CriDamage);
		ReduceDamageRate = new CreatureStat(0);
		LifeStealRate = new CreatureStat(0);
		ThornsDamageRate = new CreatureStat(0);
		MoveSpeed = new CreatureStat(CreatureData.MoveSpeed);
		AttackSpeedRate = new CreatureStat(1);

		// State
		CreatureState = ECreatureState.Idle;


	}

	protected override void UpdateAnimation()
	{
		switch (CreatureState)
		{
			case ECreatureState.Idle:
				// PlayAnimation(0, AnimName.IDLE, true);
				break;
			case ECreatureState.Skill:
				//PlayAnimation(0, AnimName.ATTACK_A, true);
				break;
			case ECreatureState.Move:
				// PlayAnimation(0, AnimName.MOVE, true);
				break;
			case ECreatureState.OnDamaged:
				// PlayAnimation(0, AnimName.IDLE, true);
				// Skills.CurrentSkill.CancelSkill();
				break;
			case ECreatureState.Dead:
				// PlayAnimation(0, AnimName.DEAD, true);
				RigidBody.simulated = false;
				break;
			default:
				break;
		}
	}

	#region AI
	public float UpdateAITick { get; protected set; } = 0.0f;

	protected IEnumerator CoUpdateAI()
	{
		while (true)
		{
			switch (CreatureState)
			{
				case ECreatureState.Idle:
					UpdateIdle();
					break;
				case ECreatureState.Move:
					UpdateMove();
					break;
				case ECreatureState.Skill:
					UpdateSkill();
					break;
				case ECreatureState.OnDamaged:
					UpdateOnDamaged();
					break;
				case ECreatureState.Dead:
					UpdateDead();
					break;
			}

			if (UpdateAITick > 0)
				yield return new WaitForSeconds(UpdateAITick);
			else
				yield return null;
		}
	}

	protected virtual void UpdateIdle() { }
	protected virtual void UpdateMove() { }
	
	protected virtual void UpdateSkill() 
	{
		if (_coWait != null)
			return;

		if (Target.IsValid() == false || Target.ObjectType == EObjectType.HeroCamp)
		{
			CreatureState = ECreatureState.Idle;
			return;
		}

		float distToTargetSqr = DistToTargetSqr;
		float attackDistanceSqr = AttackDistance * AttackDistance;
		if (distToTargetSqr > attackDistanceSqr)
		{
			CreatureState = ECreatureState.Idle;
			return;
		}

		// DoSkill
		// Skills.CurrentSkill.DoSkill();

		LookAtTarget(Target);


	}

	protected virtual void UpdateOnDamaged() { }

	protected virtual void UpdateDead() { }
	#endregion

	#region Wait
	protected Coroutine _coWait;

	protected void StartWait(float seconds)
	{
		CancelWait();
		_coWait = StartCoroutine(CoWait(seconds));
	}

	IEnumerator CoWait(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		_coWait = null;
	}

	protected void CancelWait()
	{
		if (_coWait != null)
			StopCoroutine(_coWait);
		_coWait = null;
	}
	#endregion

	#region Battle


	// public override void OnDamaged(BaseObject attacker, SkillBase skill)
	// {
	// 	base.OnDamaged(attacker, skill);

	// 	if (attacker.IsValid() == false)
	// 		return;

	// 	Creature creature = attacker as Creature;
	// 	if (creature == null)
	// 		return;

	// 	float finalDamage = creature.Atk.Value;
	// 	Hp = Mathf.Clamp(Hp - finalDamage, 0, MaxHp.Value);



	// 	// 스킬에 따른 Effect 적용

	// }

	// public override void OnDead(BaseObject attacker, SkillBase skill)
	// {
	// 	base.OnDead(attacker, skill);
	// }


	#endregion

	#region Misc
	protected bool IsValid(BaseObject bo)
	{
		return bo.IsValid();
	}
	#endregion

}
