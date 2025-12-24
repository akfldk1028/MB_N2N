using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// BrickGame용 총알 (상대방 벽돌을 향해 발사)
///
/// 동작:
/// 1. 화면 하단에서 상단으로 이동
/// 2. 상대방 벽돌(Brick)과 충돌 시 파괴
/// 3. 화면 밖으로 나가면 자동 소멸
///
/// 멀티플레이어:
/// - Server에서 스폰 및 위치 동기화
/// - Client에서 시각적 표시
/// </summary>
public class BrickGameBullet : NetworkBehaviour
{
    #region Settings
    [Header("총알 설정")]
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private Color _bulletColor = Color.yellow;
    #endregion

    #region State
    private Vector3 _direction = Vector3.up;
    private bool _isActive = false;
    private bool _isDestroying = false;
    private ulong _ownerClientId;
    private ulong _targetClientId; // 총알이 향하는 상대방
    #endregion

    #region Components
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private CircleCollider2D _collider;
    #endregion

    private void Awake()
    {
        // 컴포넌트 참조
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<CircleCollider2D>();

        // Rigidbody2D 설정 (없으면 추가)
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }
        _rb.gravityScale = 0f;
        _rb.isKinematic = true;

        // CircleCollider2D 설정 (없으면 추가)
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<CircleCollider2D>();
        }
        _collider.isTrigger = true;
        _collider.radius = 0.2f;

        // SpriteRenderer 설정 (없으면 추가)
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            // 기본 원형 스프라이트 없으면 색상만 설정
        }
    }

    /// <summary>
    /// 총알 초기화 및 발사
    /// </summary>
    /// <param name="ownerClientId">발사한 플레이어 ID</param>
    /// <param name="targetClientId">총알이 향하는 상대방 ID</param>
    /// <param name="direction">발사 방향</param>
    /// <param name="color">총알 색상</param>
    public void Fire(ulong ownerClientId, ulong targetClientId, Vector3 direction, Color color)
    {
        _ownerClientId = ownerClientId;
        _targetClientId = targetClientId;
        _direction = direction.normalized;
        _bulletColor = color;

        // 색상 적용
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _bulletColor;
        }

        _isActive = true;
        _isDestroying = false;

        // 수명 후 자동 소멸
        Invoke(nameof(DestroyBullet), _lifetime);

        GameLogger.DevLog("BrickGameBullet", $"총알 발사: Owner={ownerClientId}, Target={targetClientId}, Dir={direction}");
    }

    private void Update()
    {
        if (!_isActive || _isDestroying) return;

        // 이동
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDestroying) return;

        // Brick 충돌 검사
        var brick = other.GetComponent<Brick>();
        if (brick != null)
        {
            HandleBrickHit(brick);
            return;
        }

        // 벽(경계) 충돌 검사
        if (other.CompareTag("Wall") || other.CompareTag("TopBorder"))
        {
            DestroyBullet();
            return;
        }
    }

    /// <summary>
    /// 벽돌 충돌 처리
    /// </summary>
    private void HandleBrickHit(Brick brick)
    {
        if (_isDestroying) return;

        // 상대방 벽돌인지 확인
        if (brick.OwnerClientId != _targetClientId)
        {
            GameLogger.DevLog("BrickGameBullet", $"내 벽돌이 아님 - 무시 (Brick Owner={brick.OwnerClientId}, Target={_targetClientId})");
            return;
        }

        GameLogger.Info("BrickGameBullet", $"상대방 벽돌 명중! Brick={brick.name}");

        // 벽돌 데미지 처리 (Server-side)
        if (IsServer)
        {
            brick.TakeDamage(_damage);
        }

        // 총알 소멸
        DestroyBullet();
    }

    /// <summary>
    /// 총알 소멸
    /// </summary>
    private void DestroyBullet()
    {
        if (_isDestroying) return;

        _isDestroying = true;
        _isActive = false;

        CancelInvoke(nameof(DestroyBullet));

        // NetworkObject가 있으면 Despawn
        if (IsSpawned && IsServer)
        {
            GetComponent<NetworkObject>()?.Despawn();
        }

        Destroy(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isActive = false;
        _isDestroying = true;
    }
}
