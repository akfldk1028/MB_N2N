using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 폭탄 컴포넌트 - 범위 내 블록들을 점령
///
/// 사용법:
/// 1. GameObject에 BombComponent 추가
/// 2. Managers.Map.Components.Register(bombComponent, playerID)
/// 3. Managers.Map.Components.Use(playerID, "bomb")
/// </summary>
public class BombComponent : MonoBehaviour, IActivatableComponent
{
    #region 설정

    [Header("Bomb Settings")]
    [SerializeField] private float bombRadius = 3f;
    [SerializeField] private int maxBlocksToCapture = 10;
    [SerializeField] private float cooldownTime = 10f;

    [Header("Effect Settings")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f, 1f); // 주황색
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private float effectMaxScale = 5f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip explosionSound;

    #endregion

    #region 상태

    private IMap _map;
    private int _ownerPlayerID = -1;
    private bool _isActive = true;
    private float _remainingCooldown = 0f;

    #endregion

    #region IMapComponent 구현

    public string ComponentID => "bomb";
    public string DisplayName => "BOMB";

    public int OwnerPlayerID
    {
        get => _ownerPlayerID;
        set => _ownerPlayerID = value;
    }

    public bool IsActive => _isActive;
    public bool CanUse => _remainingCooldown <= 0f && _isActive;

    public void Initialize(IMap map, int ownerPlayerID)
    {
        _map = map;
        _ownerPlayerID = ownerPlayerID;
        _remainingCooldown = 0f;
        Debug.Log($"<color=orange>[BombComponent] 초기화 - Player {ownerPlayerID}</color>");
    }

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public void Use()
    {
        if (!CanUse)
        {
            Debug.Log($"<color=yellow>[BombComponent] 사용 불가 - 쿨다운: {_remainingCooldown:F1}s</color>");
            return;
        }

        // 폭발 실행
        ExecuteBomb();

        // 쿨다운 시작
        _remainingCooldown = cooldownTime;
    }

    public void OnBlockCaptured(GameObject block, int oldOwnerID, int newOwnerID)
    {
        // 폭탄은 블록 점령 이벤트에 반응하지 않음
    }

    public void OnTick(float deltaTime)
    {
        // 쿨다운 감소
        if (_remainingCooldown > 0f)
        {
            _remainingCooldown -= deltaTime;
            if (_remainingCooldown < 0f)
                _remainingCooldown = 0f;
        }
    }

    #endregion

    #region IActivatableComponent 구현

    public float Cooldown => cooldownTime;
    public float RemainingCooldown => _remainingCooldown;

    #endregion

    #region 폭탄 로직

    /// <summary>
    /// 폭탄 실행 (Server에서 호출됨)
    /// </summary>
    private void ExecuteBomb()
    {
        // 폭발 위치 결정 (플레이어 캐논 위치)
        Vector3 explosionCenter = GetExplosionCenter();

        Debug.Log($"<color=orange>[BombComponent] 폭발! 위치: {explosionCenter}, 반경: {bombRadius}</color>");

        // 범위 내 블록 찾기 및 점령
        int capturedCount = CaptureBlocksInRadius(explosionCenter, bombRadius);

        // 네트워크: 모든 클라이언트에 이펙트 동기화
        var spawner = UnityEngine.Object.FindObjectOfType<BrickGameMultiplayerSpawner>();
        if (spawner != null)
        {
            // ClientRpc로 모든 클라이언트에 이펙트 재생
            spawner.PlayComponentEffectClientRpc(
                ComponentID,
                explosionCenter.x,
                explosionCenter.y,
                explosionCenter.z,
                _ownerPlayerID
            );
        }
        else
        {
            // 싱글플레이어: 로컬에서 이펙트 재생
            PlayExplosionEffect(explosionCenter);
            PlayExplosionSound(explosionCenter);
        }

        Debug.Log($"<color=green>[BombComponent] {capturedCount}개 블록 점령 완료!</color>");
    }

    /// <summary>
    /// 폭발 중심점 결정
    /// </summary>
    private Vector3 GetExplosionCenter()
    {
        // 플레이어 캐논 위치 사용
        if (_map != null)
        {
            var cannons = _map.GetAllCannons();
            if (cannons != null)
            {
                foreach (var cannon in cannons)
                {
                    if (cannon != null && cannon.playerID == _ownerPlayerID)
                    {
                        return cannon.transform.position;
                    }
                }
            }
        }

        // 캐논 없으면 이 오브젝트 위치
        return transform.position;
    }

    /// <summary>
    /// 반경 내 블록들 점령
    /// </summary>
    private int CaptureBlocksInRadius(Vector3 center, float radius)
    {
        int capturedCount = 0;
        Color playerColor = _map?.GetPlayerColor(_ownerPlayerID) ?? Color.white;

        // Physics.OverlapSphere로 범위 내 콜라이더 찾기
        Collider[] colliders = Physics.OverlapSphere(center, radius);

        List<GameObject> blocksToCapture = new List<GameObject>();

        foreach (var collider in colliders)
        {
            // 블록인지 확인 (Cube_ 또는 IsometricCube 태그/이름)
            if (IsBlock(collider.gameObject))
            {
                // 이미 내 블록이 아닌 것만
                int currentOwner = _map?.GetBlockOwner(collider.gameObject) ?? -1;
                if (currentOwner != _ownerPlayerID)
                {
                    blocksToCapture.Add(collider.gameObject);
                }
            }
        }

        // 최대 개수 제한
        int countToCapture = Mathf.Min(blocksToCapture.Count, maxBlocksToCapture);

        // 거리순 정렬 (가까운 것부터)
        blocksToCapture.Sort((a, b) =>
        {
            float distA = Vector3.Distance(center, a.transform.position);
            float distB = Vector3.Distance(center, b.transform.position);
            return distA.CompareTo(distB);
        });

        // 블록 점령 (네트워크 동기화)
        var spawner = UnityEngine.Object.FindObjectOfType<BrickGameMultiplayerSpawner>();

        for (int i = 0; i < countToCapture; i++)
        {
            var block = blocksToCapture[i];
            if (block == null) continue;

            // 네트워크 동기화 (BrickGameMultiplayerSpawner가 있으면 ClientRpc로)
            if (spawner != null)
            {
                spawner.ChangeBlockOwnerClientRpc(
                    block.name,
                    _ownerPlayerID,
                    playerColor.r,
                    playerColor.g,
                    playerColor.b
                );
                capturedCount++;
            }
            else if (Managers.Map != null)
            {
                // 싱글플레이어 폴백
                Managers.Map.SetBlockOwner(block, _ownerPlayerID, playerColor);
                capturedCount++;
            }
        }

        return capturedCount;
    }

    /// <summary>
    /// 블록인지 확인
    /// </summary>
    private bool IsBlock(GameObject obj)
    {
        // 이름으로 확인
        if (obj.name.StartsWith("Cube_") || obj.name.StartsWith("IsometricCube"))
            return true;

        // 태그로 확인
        if (obj.CompareTag("Block") || obj.CompareTag("Cube"))
            return true;

        return false;
    }

    #endregion

    #region 이펙트

    /// <summary>
    /// 로컬에서 이펙트만 재생 (ClientRpc에서 호출)
    /// </summary>
    public void PlayEffectLocal(Vector3 position)
    {
        PlayExplosionEffect(position);
        PlayExplosionSound(position);
    }

    /// <summary>
    /// 폭발 이펙트 재생
    /// </summary>
    private void PlayExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null)
        {
            // 프리팹이 있으면 사용
            var effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, effectDuration + 1f);
        }
        else
        {
            // 프리팹 없으면 코드로 생성
            StartCoroutine(CreateCodeBasedExplosion(position));
        }
    }

    /// <summary>
    /// 코드 기반 폭발 이펙트
    /// </summary>
    private IEnumerator CreateCodeBasedExplosion(Vector3 position)
    {
        // 폭발 구체 생성
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.name = "BombExplosion";
        explosion.transform.position = position;
        explosion.transform.localScale = Vector3.one * 0.5f;

        // 콜라이더 제거
        var collider = explosion.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // 머티리얼 설정
        var renderer = explosion.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = explosionColor;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        // 링 이펙트들 생성
        List<GameObject> rings = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            var ring = CreateExplosionRing(position, i * 0.1f);
            rings.Add(ring);
        }

        // 애니메이션
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * effectMaxScale;

        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / effectDuration;

            // 이징 (ease out)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // 스케일 확대
            explosion.transform.localScale = Vector3.Lerp(startScale, endScale, easedT);

            // 페이드 아웃
            if (renderer != null)
            {
                Color c = explosionColor;
                c.a = 1f - easedT;
                renderer.material.color = c;
            }

            // 링 애니메이션
            for (int i = 0; i < rings.Count; i++)
            {
                if (rings[i] != null)
                {
                    float ringT = Mathf.Clamp01((elapsed - i * 0.05f) / effectDuration);
                    float ringEased = 1f - Mathf.Pow(1f - ringT, 2f);
                    rings[i].transform.localScale = Vector3.one * (effectMaxScale * 0.8f * ringEased + 0.5f);

                    var ringRenderer = rings[i].GetComponent<Renderer>();
                    if (ringRenderer != null)
                    {
                        Color rc = explosionColor;
                        rc.a = (1f - ringT) * 0.5f;
                        ringRenderer.material.color = rc;
                    }
                }
            }

            yield return null;
        }

        // 정리
        Destroy(explosion);
        foreach (var ring in rings)
        {
            if (ring != null) Destroy(ring);
        }
    }

    /// <summary>
    /// 폭발 링 생성
    /// </summary>
    private GameObject CreateExplosionRing(Vector3 position, float delay)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "ExplosionRing";
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(1f, 0.05f, 1f);

        var collider = ring.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        var renderer = ring.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.8f, 0f, 0.5f);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        return ring;
    }

    /// <summary>
    /// 폭발 사운드 재생
    /// </summary>
    private void PlayExplosionSound(Vector3 position)
    {
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, position);
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        // MapComponentManager가 Tick을 호출하므로 여기서는 불필요
        // 하지만 독립 테스트용으로 남겨둠
    }

    #endregion
}
