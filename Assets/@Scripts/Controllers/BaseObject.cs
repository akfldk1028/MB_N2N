
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;

public class BaseObject : InitBase
{
	public int ExtraCells { get; set; } = 0;

	public EObjectType ObjectType { get; protected set; } = EObjectType.None;
	public CircleCollider2D Collider { get; private set; }
	public Rigidbody2D RigidBody { get; private set; }

	public float ColliderRadius { get { return Collider != null ? Collider.radius : 0.0f; } }
	public Vector3 CenterPosition { get { return transform.position + Vector3.up * ColliderRadius; } }

	public int DataTemplateID { get; set; }

	bool _lookLeft = true;
	public bool LookLeft
	{
		get { return _lookLeft; }
		set
		{
			_lookLeft = value;
		}
	}

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		Collider = gameObject.GetOrAddComponent<CircleCollider2D>();
		// SkeletonAnim = GetComponent<SkeletonAnimation>();
		RigidBody = GetComponent<Rigidbody2D>();
		// HurtFlash = gameObject.GetOrAddComponent<HurtFlashEffect>();

		return true;
	}

	protected virtual void OnDisable()
	{

	}

	public void LookAtTarget(BaseObject target)
	{
		Vector2 dir = target.transform.position - transform.position;
		if (dir.x < 0)
			LookLeft = true;
		else
			LookLeft = false;
	}

	public static Vector3 GetLookAtRotation(Vector3 dir)
	{
		// Mathf.Atan2를 사용해 각도를 계산하고, 라디안에서 도로 변환
		float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;

		// Z축을 기준으로 회전하는 Vector3 값을 리턴
		return new Vector3(0, 0, angle);
	}

	#region Battle
	// public virtual void OnDamaged(BaseObject attacker, SkillBase skill)
	// {
	// }

	// public virtual void OnDead(BaseObject attacker, SkillBase skill)
	// {

	// }
	#endregion

	#region Spine
	protected virtual void SetSpineAnimation(string dataLabel, int sortingOrder)
	{

	


		// Spine SkeletonAnimation은 SpriteRenderer 를 사용하지 않고 MeshRenderer을 사용함
		// 그렇기떄문에 2D Sort Axis가 안먹히게 되는데 SortingGroup을 SpriteRenderer,MeshRenderer을 같이 계산함.
		SortingGroup sg = Util.GetOrAddComponent<SortingGroup>(gameObject);
		sg.sortingOrder = sortingOrder;
	}

	protected virtual void UpdateAnimation()
	{
	}

	public void AddAnimation(int trackIndex, string AnimName, bool loop, float delay)
	{

	}

	// public virtual void OnAnimEventHandler(TrackEntry trackEntry, Spine.Event e)
	// {
	// 	Debug.Log("OnAnimEventHandler");
	// }
	#endregion

}
