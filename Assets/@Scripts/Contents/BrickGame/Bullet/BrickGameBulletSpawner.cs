using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// BrickGame 총알 스포너
///
/// 역할:
/// 1. BrickGame_BulletFired 이벤트 수신
/// 2. 상대방 영역에 총알 스폰
/// 3. Server Authority 방식 (Server에서만 스폰)
///
/// 사용법:
/// - BrickGameMultiplayerSpawner와 같은 GameObject에 붙이거나
/// - 별도 GameObject에 배치
/// </summary>
public class BrickGameBulletSpawner : NetworkBehaviour
{
    #region Settings
    [Header("총알 설정")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _spawnInterval = 0.1f; // 연속 발사 간격
    [SerializeField] private float _spawnYOffset = 2f; // 패들 위로 얼마나 떨어진 곳에서 발사
    [SerializeField] private float _bulletSpread = 0.5f; // 총알 퍼짐 정도
    #endregion

    #region State
    private IDisposable _bulletFiredSubscription;
    private Dictionary<ulong, Vector3> _playerSpawnPositions = new Dictionary<ulong, Vector3>();
    #endregion

    #region Colors
    private readonly Color[] _playerColors = new Color[]
    {
        new Color(1f, 0.5f, 0f, 1f), // Player 0: 주황색
        new Color(0f, 0.8f, 1f, 1f)  // Player 1: 하늘색
    };
    #endregion

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 이벤트 구독
        _bulletFiredSubscription = Managers.Subscribe(
            ActionId.BrickGame_BulletFired,
            OnBulletFired
        );

        // 총알 Prefab 생성 (없으면)
        if (_bulletPrefab == null)
        {
            CreateBulletPrefab();
        }

        GameLogger.Success("BrickGameBulletSpawner", "초기화 완료 - BulletFired 이벤트 구독됨");
    }

    public override void OnNetworkDespawn()
    {
        _bulletFiredSubscription?.Dispose();
        _bulletFiredSubscription = null;

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// 총알 Prefab 런타임 생성 (Addressable 없을 때)
    /// </summary>
    private void CreateBulletPrefab()
    {
        // 런타임에 빈 Prefab 생성
        _bulletPrefab = new GameObject("BrickGameBulletPrefab");
        _bulletPrefab.SetActive(false);

        // 필수 컴포넌트 추가
        var bullet = _bulletPrefab.AddComponent<BrickGameBullet>();
        var networkObject = _bulletPrefab.AddComponent<NetworkObject>();

        // 시각적 표현 (간단한 원)
        var spriteRenderer = _bulletPrefab.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite();
        spriteRenderer.color = Color.yellow;
        spriteRenderer.sortingOrder = 10;

        // Transform 스케일
        _bulletPrefab.transform.localScale = Vector3.one * 0.3f;

        // NetworkManager에 Prefab 등록
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.AddNetworkPrefab(_bulletPrefab);
        }

        GameLogger.Success("BrickGameBulletSpawner", "총알 Prefab 런타임 생성 완료");
    }

    /// <summary>
    /// 간단한 원형 스프라이트 생성
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

    /// <summary>
    /// BrickGame_BulletFired 이벤트 핸들러
    /// </summary>
    private void OnBulletFired(ActionMessage message)
    {
        if (!IsServer)
        {
            // Client는 Server에 발사 요청만 전송
            if (message.TryGetPayload<BulletFiredPayload>(out var payload))
            {
                RequestFireServerRpc(payload.FiringClientId, payload.BulletCount);
            }
            return;
        }

        // Server: 직접 총알 스폰
        if (message.TryGetPayload<BulletFiredPayload>(out var serverPayload))
        {
            SpawnBulletsForPlayer(serverPayload.FiringClientId, serverPayload.BulletCount);
        }
    }

    /// <summary>
    /// Client → Server: 발사 요청
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestFireServerRpc(ulong firingClientId, int bulletCount, ServerRpcParams rpcParams = default)
    {
        // 요청한 클라이언트 검증
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != firingClientId)
        {
            GameLogger.Warning("BrickGameBulletSpawner", $"발사 요청 거부: Sender={senderId} != FiringClient={firingClientId}");
            return;
        }

        SpawnBulletsForPlayer(firingClientId, bulletCount);
    }

    /// <summary>
    /// 플레이어별 총알 스폰 (Server-side)
    /// </summary>
    private void SpawnBulletsForPlayer(ulong firingClientId, int bulletCount)
    {
        if (!IsServer) return;

        // 상대방 ID 계산 (2인 플레이 기준)
        ulong targetClientId = firingClientId == 0 ? 1UL : 0UL;

        // 발사 위치 계산 (상대방 영역 하단)
        Vector3 spawnBase = GetSpawnPositionForTarget(targetClientId);

        // 발사자 색상
        Color bulletColor = GetPlayerColor(firingClientId);

        GameLogger.Info("BrickGameBulletSpawner",
            $"[Player {firingClientId}] {bulletCount}발 발사 → Target={targetClientId}");

        // 총알 스폰
        for (int i = 0; i < bulletCount; i++)
        {
            // 약간의 랜덤 오프셋
            float xOffset = UnityEngine.Random.Range(-_bulletSpread, _bulletSpread) * (i + 1);
            Vector3 spawnPos = spawnBase + new Vector3(xOffset, 0, 0);

            SpawnSingleBullet(spawnPos, firingClientId, targetClientId, bulletColor);
        }
    }

    /// <summary>
    /// 단일 총알 스폰
    /// </summary>
    private void SpawnSingleBullet(Vector3 position, ulong ownerClientId, ulong targetClientId, Color color)
    {
        if (_bulletPrefab == null)
        {
            GameLogger.Error("BrickGameBulletSpawner", "총알 Prefab이 없습니다!");
            return;
        }

        // 총알 생성
        GameObject bulletObj = Instantiate(_bulletPrefab, position, Quaternion.identity);
        bulletObj.SetActive(true);
        bulletObj.name = $"Bullet_P{ownerClientId}_{Time.frameCount}";

        // NetworkObject 스폰
        var networkObject = bulletObj.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }

        // 총알 발사
        var bullet = bulletObj.GetComponent<BrickGameBullet>();
        if (bullet != null)
        {
            bullet.Fire(ownerClientId, targetClientId, Vector3.up, color);
        }
    }

    /// <summary>
    /// 상대방 영역의 총알 스폰 위치 계산
    /// </summary>
    private Vector3 GetSpawnPositionForTarget(ulong targetClientId)
    {
        // 캐시된 위치가 있으면 사용
        if (_playerSpawnPositions.TryGetValue(targetClientId, out var cached))
        {
            return cached;
        }

        // 플레이어별 X 오프셋 계산
        float xOffset = (targetClientId == 0) ? -15f : 15f; // PlankSpacing과 동일

        // Y 위치: 패들 위치 + offset
        float yPosition = -4f + _spawnYOffset;

        Vector3 spawnPos = new Vector3(xOffset, yPosition, 0);
        _playerSpawnPositions[targetClientId] = spawnPos;

        GameLogger.Info("BrickGameBulletSpawner", $"[Target {targetClientId}] 스폰 위치 설정: {spawnPos}");

        return spawnPos;
    }

    /// <summary>
    /// 플레이어 색상 반환
    /// </summary>
    private Color GetPlayerColor(ulong clientId)
    {
        int index = (int)(clientId % (ulong)_playerColors.Length);
        return _playerColors[index];
    }

    /// <summary>
    /// 플레이어별 스폰 위치 업데이트 (BrickGameMultiplayerSpawner에서 호출)
    /// </summary>
    public void SetPlayerSpawnPosition(ulong clientId, Vector3 position)
    {
        _playerSpawnPositions[clientId] = position;
        GameLogger.Info("BrickGameBulletSpawner", $"[Player {clientId}] 스폰 위치 업데이트: {position}");
    }
}
