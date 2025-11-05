/*
 * 생물체 스탯 (CreatureStat)
 * 
 * 역할:
 * 1. 게임 내 모든 생물체(영웅, 몬스터 등)의 스탯 관리 시스템
 * 2. 기본 스탯 값을 저장하고 여러 수정자(StatModifier)를 적용하여 최종 값 계산
 * 3. 다양한 효과(버프, 디버프, 아이템, 스킬)에 의한 스탯 변화를 동적으로 처리
 * 4. 수정자의 추가/제거 및 특정 소스의 모든 수정자 제거 기능 제공
 * 5. 스탯 계산 최적화: 값이 변경될 때만 재계산(_isDirty 플래그 사용)
 * 6. 스탯 수정자의 적용 순서를 보장하여 정확한 스탯 계산을 수행
 *    (덧셈 -> 퍼센트 덧셈 -> 퍼센트 곱셈 순으로 적용)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;


[Serializable]
public class CreatureStat
{
	public float BaseValue { get; private set; }

	private bool _isDirty = true;

	[SerializeField]
	private float _value;
	public virtual float Value
	{
		get
		{
			if (_isDirty)
			{
				_value = CalculateFinalValue();
				_isDirty = false;
			}
			return _value;
		}

		private set { _value = value; }
	}

	public List<StatModifier> StatModifiers = new List<StatModifier>();

	public CreatureStat()
	{
	}

	public CreatureStat(float baseValue) : this()
	{
		BaseValue = baseValue;
	}

	public virtual void AddModifier(StatModifier modifier)
	{
		_isDirty = true;
		StatModifiers.Add(modifier);
	}

	public virtual bool RemoveModifier(StatModifier modifier)
	{
		if (StatModifiers.Remove(modifier))
		{
			_isDirty = true;
			return true;
		}

		return false;
	}

	public virtual bool ClearModifiersFromSource(object source)
	{
		int numRemovals = StatModifiers.RemoveAll(mod => mod.Source == source);

		if (numRemovals > 0)
		{
			_isDirty = true;
			return true;
		}
		return false;
	}

	private int CompareOrder(StatModifier a, StatModifier b)
	{
		if (a.Order == b.Order)
			return 0;

		return (a.Order < b.Order) ? -1 : 1;
	}


	private float CalculateFinalValue()
	{
		float finalValue = BaseValue;
		float sumPercentAdd = 0;

		StatModifiers.Sort(CompareOrder);

		for (int i = 0; i < StatModifiers.Count; i++)
		{
			StatModifier modifier = StatModifiers[i];

			switch (modifier.Type)
			{
				case EStatModType.Add:
					finalValue += modifier.Value;
					break;
				case EStatModType.PercentAdd:
					sumPercentAdd += modifier.Value;
					if (i == StatModifiers.Count - 1 || StatModifiers[i + 1].Type != EStatModType.PercentAdd)
					{
						finalValue *= 1 + sumPercentAdd;
						sumPercentAdd = 0;
					}
					break;
				case EStatModType.PercentMult:
					finalValue *= 1 + modifier.Value;
					break;
			}
		}

		return (float)Math.Round(finalValue, 4);
	}
}
