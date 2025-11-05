using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class Hero : Creature
{

	public override ECreatureState CreatureState
	{
		get { return _creatureState; }
		set
		{
			if (_creatureState != value)
			{
				base.CreatureState = value;
			}
		}
	}

	EHeroMoveState _heroMoveState = EHeroMoveState.None;
	public EHeroMoveState HeroMoveState
	{
		get { return _heroMoveState; }
		private set
		{
			_heroMoveState = value;
			switch (value)
			{
				case EHeroMoveState.CollectEnv:
					break;
				case EHeroMoveState.TargetMonster:
					break;
				case EHeroMoveState.ForceMove:
					break;
			}
		}
	}

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		ObjectType = EObjectType.Hero;



		// Map
		Collider.isTrigger = true;
		RigidBody.simulated = false;

		StartCoroutine(CoUpdateAI());

		return true;
	}

	public override void SetInfo(int templateID)
	{
		base.SetInfo(templateID);

		// State
		CreatureState = ECreatureState.Idle;
	}


	#region AI
	protected override void UpdateIdle() 
	{
		// 0. 이동 상태라면 강제 변경
		if (HeroMoveState == EHeroMoveState.ForceMove)
		{
			CreatureState = ECreatureState.Move;
			return;
		}

	

	}






	

	protected override void UpdateSkill() 
	{
		base.UpdateSkill();

		if (HeroMoveState == EHeroMoveState.ForceMove)
		{
			CreatureState = ECreatureState.Move;
			return;
		}

		if (Target.IsValid() == false)
		{
			CreatureState = ECreatureState.Move;
			return;
		}
	}

	protected override void UpdateDead() 
	{

	}
	#endregion




}
