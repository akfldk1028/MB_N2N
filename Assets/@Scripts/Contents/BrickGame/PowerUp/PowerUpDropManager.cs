using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.Objects;
using MB.Infrastructure.Messages;

/// <summary>
/// 파워업 드롭 매니저 (POCO - MonoBehaviour 아님)
///
/// 역할:
/// 1. BrickGame_BrickDestroyed 이벤트 구독
/// 2. 확률 테이블 기반 드롭 결정 (Star 5%, BonusBall 3%, 없음 92%)
/// 3. PowerUpItem NetworkObject 서버에서 스폰
///
/// 접근: Managers.Game.BrickGame.PowerUpDrop
/// </summary>
public class PowerUpDropManager
{
    #region 드롭 확률 설정
    private const float StarDropRate = 0.05f;       // Star 5%
    private const float BonusBallDropRate = 0.03f;   // BonusBall 3%
    // None = 92% (1 - 0.05 - 0.03)
    #endregion

    #region 상태
    private IDisposable _brickDestroyedSubscription;
    private GameObject _powerUpPrefab;
    private bool _isInitialized = false;
    #endregion

    #region 이벤트
    /// <summary>
    /// 파워업 아이템이 드롭될 때 발생
    /// </summary>
    public event Action<PowerUpType, Vector3> OnPowerUpDropped;
    #endregion

    #region 생성자
    public PowerUpDropManager()
    {
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 매니저 초기화 - ActionBus 구독 및 런타임 Prefab 생성
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            GameLogger.Warning("PowerUpDropManager", "이미 초기화됨 (무시)");
            return;
        }

        // ActionBus 이벤트 구독
        _brickDestroyedSubscription = Managers.Subscribe(
            ActionId.BrickGame_BrickDestroyed,
            OnBrickDestroyed
        );

        // 런타임 Prefab 생성
        CreatePowerUpPrefab();

        _isInitialized = true;
        GameLogger.Success("PowerUpDropManager", "초기화 완료 - BrickDestroyed 이벤트 구독됨");
    }

    /// <summary>
    /// 매니저 정리 - ActionBus 구독 해제
    /// </summary>
    public void Dispose()
    {
        _brickDestroyedSubscription?.Dispose();
        _brickDestroyedSubscription = null;

        if (_powerUpPrefab != null)
        {
            UnityEngine.Object.Destroy(_powerUpPrefab);
            _powerUpPrefab = null;
        }

        _isInitialized = false;
        GameLogger.Info("PowerUpDropManager", "정리 완료");
    }
    #endregion

    #region 런타임 Prefab 생성
    /// <summary>
    /// PowerUpItem 런타임 Prefab 생성 (BrickGameBulletSpawner.CreateBulletPrefab 패턴)
    /// </summary>
    private void CreatePowerUpPrefab()
    {
        // 런타임에 빈 Prefab 생성
        _powerUpPrefab = new GameObject("PowerUpItemPrefab");
        _powerUpPrefab.SetActive(false);

        // 필수 컴포넌트 추가
        var powerUpItem = _powerUpPrefab.AddComponent<PowerUpItem>();
        var networkObject = _powerUpPrefab.AddComponent<NetworkObject>();

        // Rigidbody2D (PhysicsObject가 필요로 함)
        var rb = _powerUpPrefab.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // PowerUpItem.FixedUpdate에서 직접 속도 제어
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // CircleCollider2D (트리거 모드 - Plank/BottomBoundary와 겹침 감지)
        var collider = _powerUpPrefab.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // 시각적 표현 (원형 스프라이트)
        var spriteRenderer = _powerUpPrefab.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite();
        spriteRenderer.color = Color.white; // 스폰 시 타입별 색상 설정
        spriteRenderer.sortingOrder = 10;

        // Transform 스케일
        _powerUpPrefab.transform.localScale = Vector3.one * 0.4f;

        // NetworkManager에 Prefab 등록
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.AddNetworkPrefab(_powerUpPrefab);
        }

        GameLogger.Success("PowerUpDropManager", "PowerUpItem Prefab 런타임 생성 완료");
    }

    /// <summary>
    /// 간단한 원형 스프라이트 생성 (BrickGameBulletSpawner 패턴)
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
    #endregion

    #region 이벤트 핸들러
    /// <summary>
    /// BrickGame_BrickDestroyed 이벤트 핸들러
    /// 확률 테이블 롤링 후 파워업 아이템 스폰
    /// </summary>
    private void OnBrickDestroyed(ActionMessage message)
    {
        // Server Authority: 드롭 결정은 서버에서만
        if (!MultiplayerUtil.HasServerAuthority()) return;

        // 페이로드에서 위치 및 소유자 정보 추출
        if (!message.TryGetPayload<BrickGameBrickDestroyedPayload>(out var payload))
        {
            GameLogger.Warning("PowerUpDropManager", "BrickDestroyed 이벤트에 페이로드 없음");
            return;
        }

        // 확률 테이블 롤링
        PowerUpType dropType = RollDropTable();

        if (dropType == PowerUpType.None) return;

        // 파워업 아이템 스폰
        SpawnPowerUpItem(payload.Position, dropType, payload.OwnerClientId);
    }
    #endregion

    #region 확률 테이블
    /// <summary>
    /// 드롭 확률 테이블 롤링
    /// Star: 5%, BonusBall: 3%, None: 92%
    /// </summary>
    private PowerUpType RollDropTable()
    {
        float roll = UnityEngine.Random.value; // 0.0 ~ 1.0

        if (roll < StarDropRate)
        {
            return PowerUpType.Star;
        }
        else if (roll < StarDropRate + BonusBallDropRate)
        {
            return PowerUpType.BonusBall;
        }

        return PowerUpType.None;
    }
    #endregion

    #region 스폰
    /// <summary>
    /// 파워업 아이템 스폰 (Server-side)
    /// </summary>
    /// <param name="position">스폰 위치 (파괴된 벽돌 위치)</param>
    /// <param name="type">파워업 종류</param>
    /// <param name="ownerClientId">소유 플레이어 ID</param>
    private void SpawnPowerUpItem(Vector3 position, PowerUpType type, ulong ownerClientId)
    {
        if (!MultiplayerUtil.HasServerAuthority()) return;

        // 멀티플레이어: 런타임 프리팹(hash=0) 문제로 PowerUp 스킵
        if (MultiplayerUtil.IsMultiplayer()) return;

        if (_powerUpPrefab == null)
        {
            GameLogger.Error("PowerUpDropManager", "PowerUpItem Prefab이 없습니다!");
            return;
        }

        // Prefab 인스턴스 생성
        GameObject powerUpObj = UnityEngine.Object.Instantiate(_powerUpPrefab, position, Quaternion.identity);
        powerUpObj.SetActive(true);
        powerUpObj.name = $"PowerUp_{type}_{Time.frameCount}";

        // ✅ 런타임 생성 프리팹은 globalObjectIdHash=0이라 NetworkObject.Spawn() 하면 Client에서 실패
        // 멀티에서는 서버 로컬로만 동작 (Client에 동기화 불필요 — 파워업은 서버 물리로 처리)
        var networkObject = powerUpObj.GetComponent<NetworkObject>();
        if (networkObject != null && !MultiplayerUtil.IsMultiplayer())
        {
            networkObject.Spawn();
        }
        else if (networkObject != null)
        {
            // 멀티: NetworkObject 제거 (Spawn 안 하면 경고 발생 방지)
            UnityEngine.Object.Destroy(networkObject);
        }

        // PowerUpItem 초기화 (타입 및 소유자 설정)
        var powerUpItem = powerUpObj.GetComponent<PowerUpItem>();
        if (powerUpItem != null)
        {
            powerUpItem.Initialize(type, ownerClientId);
        }

        // 이벤트 발생
        OnPowerUpDropped?.Invoke(type, position);

        GameLogger.Info("PowerUpDropManager",
            $"파워업 드롭! Type={type}, Position={position}, Owner={ownerClientId}");
    }
    #endregion
}
