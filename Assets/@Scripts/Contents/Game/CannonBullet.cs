using UnityEngine;

public class CannonBullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float speed = 20f;
    public float lifetime = 5f;
    public int damage = 10;
    public GameObject hitEffect;

    // 캐논 소유자 추적을 위한 변수
    [HideInInspector] public Cannon ownerCannon;
    [HideInInspector] public Color ownerColor;
    [HideInInspector] public int ownerPlayerID = -1; // 멀티플레이어를 위한 ID 준비

    private Vector3 direction;
    private bool isActive = false;
    private bool isDestroying = false;

    private void Awake()
    {
        // 리지드바디 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        
        // 콜라이더 설정
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        isActive = false;
        isDestroying = false;
        Destroy(gameObject, lifetime);
    }

    // 총알 발사 메서드
    public void Fire(Vector3 dir, float spd = 0)
    {
        direction = dir.normalized;
        if (spd > 0) speed = spd;
        isActive = true;
    }

    // 소유자 설정 메서드
    public void SetOwner(Cannon cannon, Color color, int playerID = -1)
    {
        // 소유자 정보 설정
        ownerCannon = cannon;
        ownerColor = color;
        ownerPlayerID = playerID;
        
        // 디버그 정보 출력
        Debug.Log($"<color=yellow>[CannonBullet] 총알 소유자 설정 - ID: {playerID}, 색상: R:{color.r}, G:{color.g}, B:{color.b}</color>");
        
        // 총알 자체에 색상 적용
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
            Debug.Log($"<color=green>[CannonBullet] 총알 색상 적용 완료</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>[CannonBullet] 총알에 렌더러가 없어서 색상을 적용할 수 없습니다</color>");
        }
    }

    private void Update()
    {
        if (!isActive || isDestroying) return;
        
        // 단순하게 방향으로 이동
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroying) return;
        
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (isDestroying || hitObject == null) return;
        
        // 다른 총알과 충돌 무시
        if (hitObject.GetComponent<CannonBullet>() != null) {
            DestroyBullet();
            return;
        }
        
        // 벽과 충돌 시 파괴
        if (hitObject.CompareTag("Wall"))
        {
            DestroyBullet();
            return;
        }
        
        // 그리드 블록과 충돌 처리
        if (hitObject.CompareTag("GridBlock") || (hitObject.transform.parent != null && hitObject.transform.parent.GetComponent<IsometricGridGenerator>() != null))
        {
            // IsometricGridGenerator가 있는지 확인
            if (IsometricGridGenerator.Instance == null)
            {
                Debug.LogError("<color=red>오류: IsometricGridGenerator Instance 없음!</color>");
                DestroyBullet();
                return;
            }
            
            // 블록 소유자 확인
            int blockOwnerID = IsometricGridGenerator.Instance.GetBlockOwner(hitObject);
            
            // *** 상세 로그 추가 ***
            Debug.Log($"<color=#FFA500>충돌 정보: 총알 소유자 ID={ownerPlayerID}, 충돌 블록 이름={hitObject.name}, 블록 소유자 ID={blockOwnerID}</color>");
            
            // 자신의 블록인지 확인
            if (blockOwnerID == ownerPlayerID && ownerPlayerID >= 0)
            {
                // *** 상세 로그 추가 ***
                Debug.Log("<color=yellow>내 블록 충돌! 통과 처리.</color>");
                // 자신의 블록이면 아무것도 하지 않고 통과 (return 전에 isDestroying=false 필요 없음)
                return;
            }
            
            // 중립(-1) 또는 상대방 블록인 경우에만 색상 변경
            Debug.Log("<color=cyan>상대방 또는 중립 블록 충돌! 색상/소유권 변경 처리 시작.</color>");
            Renderer blockRenderer = hitObject.GetComponent<Renderer>();
            if (blockRenderer != null)
            {
                Color oldColor = blockRenderer.material.color;
                blockRenderer.material.color = ownerColor;
                Debug.Log($"<color=green>블록 색상 변경 완료: {oldColor} -> {ownerColor}</color>");
            }
            
            // 소유권 변경
            IsometricGridGenerator.Instance.SetBlockOwner(hitObject, ownerPlayerID, ownerColor);
            Debug.Log($"<color=magenta>블록 소유권 변경 완료: {blockOwnerID} -> {ownerPlayerID}</color>");
            
            // 충돌 효과 생성 (이펙트가 있다면)
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // 상대방/중립 블록과 충돌 후 총알 파괴
            DestroyBullet();
        }
    }
    
    private void DestroyBullet()
    {
        if (!isDestroying)
        {
            isDestroying = true;
            isActive = false;
            
            // 이펙트 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
} 