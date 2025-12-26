using UnityEngine;

/// <summary>
/// 시각적 총알 (NetworkObject 없음 - 로컬 전용)
///
/// 네트워크 부하 없이 "쏟아지는" 효과를 위한 경량 총알
/// 충돌 감지는 하지 않음 (실제 충돌은 CannonBullet이 처리)
/// </summary>
public class VisualBullet : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 3f;
    public float speed = 25f;

    private Vector3 direction;
    private float spawnTime;
    private bool isActive;
    private float fixedY;  // ✅ Y값 고정 (바닥에 묻히지 않도록)
    private Renderer bulletRenderer;

    // 오브젝트 풀링용
    private static Transform _poolRoot;
    private static System.Collections.Generic.Queue<VisualBullet> _pool
        = new System.Collections.Generic.Queue<VisualBullet>();

    private void Awake()
    {
        bulletRenderer = GetComponent<Renderer>();
        if (bulletRenderer == null)
        {
            bulletRenderer = GetComponentInChildren<Renderer>();
        }
    }

    /// <summary>
    /// 총알 초기화 및 발사
    /// </summary>
    public void Fire(Vector3 dir, float spd, Color color)
    {
        // ✅ Y값 제거하고 XZ 평면에서만 이동
        direction = new Vector3(dir.x, 0, dir.z).normalized;
        speed = spd;
        spawnTime = Time.time;
        isActive = true;

        // ✅ 현재 Y값 저장 (고정 높이 유지)
        fixedY = transform.position.y;

        // 색상 적용
        if (bulletRenderer != null)
        {
            bulletRenderer.material.color = color;
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isActive) return;

        // ✅ XZ 평면에서 이동 (Y값 고정 유지)
        Vector3 newPos = transform.position + direction * speed * Time.deltaTime;
        newPos.y = fixedY;  // Y값 강제 고정
        transform.position = newPos;

        // 수명 체크
        if (Time.time - spawnTime > lifetime)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 풀로 반환
    /// </summary>
    public void ReturnToPool()
    {
        isActive = false;
        gameObject.SetActive(false);

        if (_poolRoot != null)
        {
            transform.SetParent(_poolRoot);
        }

        _pool.Enqueue(this);
    }

    #region Static Pool API

    /// <summary>
    /// 풀에서 시각적 총알 가져오기 또는 새로 생성
    /// </summary>
    public static VisualBullet GetOrCreate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // 풀 루트 생성
        if (_poolRoot == null)
        {
            var rootGO = new GameObject("@VisualBulletPool");
            _poolRoot = rootGO.transform;
        }

        VisualBullet bullet;

        // 풀에서 가져오기
        while (_pool.Count > 0)
        {
            bullet = _pool.Dequeue();
            if (bullet != null)
            {
                bullet.transform.position = position;
                bullet.transform.rotation = rotation;
                return bullet;
            }
        }

        // 풀이 비어있으면 새로 생성
        if (prefab != null)
        {
            GameObject go = Instantiate(prefab, position, rotation, _poolRoot);
            bullet = go.GetComponent<VisualBullet>();
            if (bullet == null)
            {
                bullet = go.AddComponent<VisualBullet>();
            }
            return bullet;
        }

        return null;
    }

    /// <summary>
    /// 간단한 구체 프리팹으로 시각적 총알 생성
    /// </summary>
    public static VisualBullet CreateSimple(Vector3 position, Quaternion rotation, float scale = 0.3f)
    {
        // 풀 루트 생성
        if (_poolRoot == null)
        {
            var rootGO = new GameObject("@VisualBulletPool");
            _poolRoot = rootGO.transform;
        }

        VisualBullet bullet;

        // 풀에서 가져오기
        while (_pool.Count > 0)
        {
            bullet = _pool.Dequeue();
            if (bullet != null)
            {
                bullet.transform.position = position;
                bullet.transform.rotation = rotation;
                bullet.gameObject.SetActive(true);
                return bullet;
            }
        }

        // 새로 생성
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = Vector3.one * scale;
        go.transform.SetParent(_poolRoot);
        go.name = "VisualBullet";

        // 콜라이더 제거 (충돌 필요 없음)
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        bullet = go.AddComponent<VisualBullet>();
        return bullet;
    }

    /// <summary>
    /// 풀 정리
    /// </summary>
    public static void ClearPool()
    {
        while (_pool.Count > 0)
        {
            var bullet = _pool.Dequeue();
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
        }

        if (_poolRoot != null)
        {
            Destroy(_poolRoot.gameObject);
            _poolRoot = null;
        }
    }

    #endregion
}
