using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 수확 컴포넌트 - 시간에 따라 주변 블록 자동 점령
///
/// 특징:
/// - 충전식 (IChargeableComponent) - 블록 점령할 때마다 충전
/// - 최대 충전 시 자동 발동 또는 수동 발동
/// - 주변 적 블록을 서서히 내 블록으로 변환
///
/// 사용법:
/// 1. GameObject에 HarvestComponent 추가
/// 2. Managers.Map.Components.Register(harvestComponent, playerID)
/// 3. 자동 충전 또는 Managers.Map.Components.Use(playerID, "harvest")
/// </summary>
public class HarvestComponent : MonoBehaviour, IChargeableComponent
{
    #region 설정

    [Header("Harvest Settings")]
    [SerializeField] private float harvestRadius = 2f;
    [SerializeField] private int blocksPerHarvest = 3;
    [SerializeField] private float harvestInterval = 0.5f; // 블록당 수확 간격

    [Header("Charge Settings")]
    [SerializeField] private int maxCharge = 10;
    [SerializeField] private int chargePerCapture = 1; // 블록 점령 시 충전량
    [SerializeField] private bool autoActivateOnFull = true;

    [Header("Effect Settings")]
    [SerializeField] private GameObject harvestEffectPrefab;
    [SerializeField] private Color harvestColor = new Color(0.2f, 0.8f, 0.2f, 1f); // 초록색
    [SerializeField] private float effectDuration = 0.3f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip harvestSound;

    #endregion

    #region 상태

    private IMap _map;
    private int _ownerPlayerID = -1;
    private bool _isActive = true;
    private int _currentCharge = 0;
    private bool _isHarvesting = false;

    #endregion

    #region IMapComponent 구현

    public string ComponentID => "harvest";
    public string DisplayName => "HARVEST";

    public int OwnerPlayerID
    {
        get => _ownerPlayerID;
        set => _ownerPlayerID = value;
    }

    public bool IsActive => _isActive;
    public bool CanUse => _currentCharge >= maxCharge && _isActive && !_isHarvesting;

    public void Initialize(IMap map, int ownerPlayerID)
    {
        _map = map;
        _ownerPlayerID = ownerPlayerID;
        _currentCharge = 0;
        Debug.Log($"<color=green>[HarvestComponent] 초기화 - Player {ownerPlayerID}</color>");
    }

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
        StopAllCoroutines();
        _isHarvesting = false;
    }

    public void Use()
    {
        if (!CanUse)
        {
            Debug.Log($"<color=yellow>[HarvestComponent] 사용 불가 - 충전: {_currentCharge}/{maxCharge}</color>");
            return;
        }

        // 수확 실행
        StartCoroutine(ExecuteHarvest());

        // 충전 소모
        _currentCharge = 0;
    }

    public void OnBlockCaptured(GameObject block, int oldOwnerID, int newOwnerID)
    {
        // 내가 블록을 점령했을 때 충전
        if (newOwnerID == _ownerPlayerID && oldOwnerID != _ownerPlayerID)
        {
            AddCharge(chargePerCapture);
        }
    }

    public void OnTick(float deltaTime)
    {
        // 자동 발동 체크
        if (autoActivateOnFull && CanUse)
        {
            Use();
        }
    }

    #endregion

    #region IChargeableComponent 구현

    public int CurrentCharge => _currentCharge;
    public int MaxCharge => maxCharge;

    public void AddCharge(int amount)
    {
        _currentCharge = Mathf.Min(_currentCharge + amount, maxCharge);
        Debug.Log($"<color=green>[HarvestComponent] 충전: {_currentCharge}/{maxCharge}</color>");
    }

    #endregion

    #region 수확 로직

    /// <summary>
    /// 수확 실행 (코루틴)
    /// </summary>
    private IEnumerator ExecuteHarvest()
    {
        _isHarvesting = true;

        // 수확 위치 결정 (플레이어 캐논 위치)
        Vector3 harvestCenter = GetHarvestCenter();

        Debug.Log($"<color=green>[HarvestComponent] 수확 시작! 위치: {harvestCenter}, 반경: {harvestRadius}</color>");

        // 수확 가능한 블록 찾기
        List<GameObject> blocksToHarvest = FindHarvestableBlocks(harvestCenter);

        // 수확할 블록 수 결정
        int harvestCount = Mathf.Min(blocksToHarvest.Count, blocksPerHarvest);

        Color playerColor = _map?.GetPlayerColor(_ownerPlayerID) ?? Color.white;
        var spawner = Object.FindObjectOfType<BrickGameMultiplayerSpawner>();

        // 순차적으로 수확 (시각적 효과)
        for (int i = 0; i < harvestCount; i++)
        {
            var block = blocksToHarvest[i];
            if (block == null) continue;

            // 수확 이펙트
            PlayHarvestEffect(block.transform.position);

            // 블록 점령 (네트워크 동기화)
            if (spawner != null)
            {
                spawner.ChangeBlockOwnerClientRpc(
                    block.name,
                    _ownerPlayerID,
                    playerColor.r,
                    playerColor.g,
                    playerColor.b
                );
            }
            else if (Managers.Map != null)
            {
                Managers.Map.SetBlockOwner(block, _ownerPlayerID, playerColor);
            }

            yield return new WaitForSeconds(harvestInterval);
        }

        Debug.Log($"<color=green>[HarvestComponent] 수확 완료! {harvestCount}개 블록</color>");

        _isHarvesting = false;
    }

    /// <summary>
    /// 수확 중심점 결정
    /// </summary>
    private Vector3 GetHarvestCenter()
    {
        // 내 블록 중 가장 적진에 가까운 블록 찾기
        if (_map != null)
        {
            // 우선 캐논 위치 시도
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

        return transform.position;
    }

    /// <summary>
    /// 수확 가능한 블록 찾기 (내 블록과 인접한 적 블록)
    /// </summary>
    private List<GameObject> FindHarvestableBlocks(Vector3 center)
    {
        List<GameObject> harvestable = new List<GameObject>();

        // 내 블록들의 주변에서 적 블록 찾기
        Collider[] colliders = Physics.OverlapSphere(center, harvestRadius * 2f);

        // 먼저 내 블록 목록 수집
        List<GameObject> myBlocks = new List<GameObject>();
        foreach (var col in colliders)
        {
            if (IsBlock(col.gameObject))
            {
                int owner = _map?.GetBlockOwner(col.gameObject) ?? -1;
                if (owner == _ownerPlayerID)
                {
                    myBlocks.Add(col.gameObject);
                }
            }
        }

        // 내 블록 주변의 적 블록 찾기
        foreach (var myBlock in myBlocks)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(myBlock.transform.position, harvestRadius);

            foreach (var nearbyCol in nearbyColliders)
            {
                if (!IsBlock(nearbyCol.gameObject)) continue;

                int owner = _map?.GetBlockOwner(nearbyCol.gameObject) ?? -1;

                // 적 블록이거나 중립 블록
                if (owner != _ownerPlayerID && !harvestable.Contains(nearbyCol.gameObject))
                {
                    harvestable.Add(nearbyCol.gameObject);
                }
            }
        }

        // 거리순 정렬 (내 캐논에서 가까운 것부터)
        harvestable.Sort((a, b) =>
        {
            float distA = Vector3.Distance(center, a.transform.position);
            float distB = Vector3.Distance(center, b.transform.position);
            return distA.CompareTo(distB);
        });

        return harvestable;
    }

    /// <summary>
    /// 블록인지 확인
    /// </summary>
    private bool IsBlock(GameObject obj)
    {
        if (obj.name.StartsWith("Cube_") || obj.name.StartsWith("IsometricCube"))
            return true;

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
        PlayHarvestEffect(position);
        PlayHarvestSound(position);
    }

    /// <summary>
    /// 수확 이펙트 재생
    /// </summary>
    private void PlayHarvestEffect(Vector3 position)
    {
        if (harvestEffectPrefab != null)
        {
            var effect = Instantiate(harvestEffectPrefab, position, Quaternion.identity);
            Destroy(effect, effectDuration + 0.5f);
        }
        else
        {
            StartCoroutine(CreateCodeBasedHarvestEffect(position));
        }
    }

    /// <summary>
    /// 코드 기반 수확 이펙트
    /// </summary>
    private IEnumerator CreateCodeBasedHarvestEffect(Vector3 position)
    {
        // 위로 올라가는 파티클 효과 (간단한 구현)
        GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        particle.name = "HarvestParticle";
        particle.transform.position = position;
        particle.transform.localScale = Vector3.one * 0.3f;

        // 콜라이더 제거
        var collider = particle.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // 머티리얼 설정
        var renderer = particle.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = harvestColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        // 위로 올라가면서 사라지는 애니메이션
        float elapsed = 0f;
        Vector3 startPos = position;
        Vector3 endPos = position + Vector3.up * 1.5f;

        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / effectDuration;

            // 위치 이동
            particle.transform.position = Vector3.Lerp(startPos, endPos, t);

            // 크기 축소 및 페이드
            float scale = Mathf.Lerp(0.3f, 0.1f, t);
            particle.transform.localScale = Vector3.one * scale;

            if (renderer != null)
            {
                Color c = harvestColor;
                c.a = 1f - t;
                renderer.material.color = c;
            }

            yield return null;
        }

        Destroy(particle);
    }

    /// <summary>
    /// 수확 사운드 재생
    /// </summary>
    private void PlayHarvestSound(Vector3 position)
    {
        if (harvestSound != null)
        {
            AudioSource.PlayClipAtPoint(harvestSound, position);
        }
    }

    #endregion
}
