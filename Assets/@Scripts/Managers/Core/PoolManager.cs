/*
 * 풀 매니저 (PoolManager)
 * 
 * 역할:
 * 1. 게임 오브젝트 풀링 시스템 구현 - 빈번하게 생성/삭제되는 오브젝트의 메모리 관리 최적화
 * 2. 각 프리팹 타입별로 별도의 풀(Pool)을 생성하여 효율적인 오브젝트 관리
 * 3. 오브젝트 재사용을 통한 가비지 컬렉션 부하 감소 및 게임 성능 향상
 * 4. Pop(): 풀에서 오브젝트를 꺼내 활성화하여 반환
 * 5. Push(): 사용이 끝난 오브젝트를 비활성화하고 풀에 반환
 * 6. 자동으로 필요한 풀을 생성하고 관리하는 기능 제공
 * 7. 내부 Pool 클래스를 통해 각 프리팹 타입별 오브젝트 풀을 관리
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

internal class Pool
{
	private GameObject _prefab;
	private IObjectPool<GameObject> _pool;

	private Transform _root;
	private Transform Root
	{
		get
		{
			if (_root == null)
			{
				GameObject go = new GameObject() { name = $"@{_prefab.name}Pool" };
				_root = go.transform;
			}

			return _root;
		}
	}

	public Pool(GameObject prefab)
	{
		_prefab = prefab;
		_pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
	}

	public void Push(GameObject go)
	{
		if (go.activeSelf)
			_pool.Release(go);
	}

	public GameObject Pop()
	{
		return _pool.Get();
	}

	#region Funcs
	private GameObject OnCreate()
	{
		GameObject go = GameObject.Instantiate(_prefab);
		go.transform.SetParent(Root);
		go.name = _prefab.name;
		return go;
	}

	private void OnGet(GameObject go)
	{
		go.SetActive(true);
	}

	private void OnRelease(GameObject go)
	{
		go.SetActive(false);
	}

	private void OnDestroy(GameObject go)
	{
		GameObject.Destroy(go);
	}
	#endregion
}

public class PoolManager
{
	public PoolManager()
	{
		Debug.Log("<color=green>[PoolManager]</color> 생성됨");
	}
	
	private Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

	public GameObject Pop(GameObject prefab)
	{
		if (_pools.ContainsKey(prefab.name) == false)
			CreatePool(prefab);

		return _pools[prefab.name].Pop();
	}

	public bool Push(GameObject go)
	{
		if (_pools.ContainsKey(go.name) == false)
			return false;

		_pools[go.name].Push(go);
		return true;
	}

	public void Clear()
	{
		_pools.Clear();
	}

	private void CreatePool(GameObject original)
	{
		Pool pool = new Pool(original);
		_pools.Add(original.name, pool);
	}
}
