/*
 * 스탯 수정자 (StatModifier)
 * 
 * 역할:
 * 1. 캐릭터나 몬스터의 스탯(공격력, 방어력 등)을 수정하는 역할을 담당
 * 2. 세 가지 타입의 스탯 수정 방식 지원:
 *    - Add: 기본 값에 수치를 더함
 *    - PercentAdd: 모든 덧셈이 끝난 후 퍼센트로 더함
 *    - PercentMult: 최종 값에 퍼센트로 곱함
 * 3. 스킬, 아이템, 버프 등에서 스탯 변경이 필요할 때 사용
 * 4. Source를 통해 어떤 객체가 이 수정자를 추가했는지 추적 가능
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class StatModifier
{
	public readonly float Value;
	public readonly EStatModType Type;
	public readonly int Order;
	public readonly object Source;

	public StatModifier(float value, EStatModType type, int order, object source)
	{
		Value = value;
		Type = type;
		Order = order;
		Source = source;
	}

	public StatModifier(float value, EStatModType type) : this(value, type, (int)type, null) { }

	public StatModifier(float value, EStatModType type, int order) : this(value, type, order, null) { }

	public StatModifier(float value, EStatModType type, object source) : this(value, type, (int)type, source) { }
}