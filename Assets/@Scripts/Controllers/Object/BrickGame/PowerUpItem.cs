using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 파워업 아이템 - 벽돌 파괴 시 드롭되어 아래로 낙하하는 아이템
    /// Plank(패들)에 닿으면 효과 발동, BottomBoundary에 닿으면 소멸
    /// 기존 Star/BonusBall과 다르게 공이 아닌 Plank에서 수집됨
    /// </summary>
    public class PowerUpItem : PhysicsObject
    {
        [Header("PowerUp Settings")]
        [SerializeField] private float fallSpeed = 3f; // 낙하 속도
        [SerializeField] private int powerIncrease = 1; // Star 효과: 공격력 증가량
        [SerializeField] private float powerDuration = 15f; // Star 효과: 지속 시간 (초)
        [SerializeField] private AudioClip collectSound; // 획득 효과음
        [SerializeField] private GameObject collectEffect; // 획득 이펙트

        private PowerUpType _powerUpType = PowerUpType.None;
        private bool isCollected = false;
        private ulong _ownerClientId; // 멀티플레이어: 이 아이템이 속한 플레이어
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// 파워업 종류
        /// </summary>
        public PowerUpType Type => _powerUpType;

        /// <summary>
        /// 파워업 아이템 초기화 - 스폰 후 호출
        /// </summary>
        /// <param name="type">파워업 종류 (Star, BonusBall)</param>
        /// <param name="ownerClientId">소유 플레이어 ID (멀티플레이어용)</param>
        public void Initialize(PowerUpType type, ulong ownerClientId = 0)
        {
            _powerUpType = type;
            _ownerClientId = ownerClientId;

            // 시각적 구분: 타입별 색상 설정
            ApplyVisualByType();
        }

        /// <summary>
        /// 파워업 아이템 초기화 - 간단 버전
        /// </summary>
        /// <param name="type">파워업 종류</param>
        public void Initialize(PowerUpType type)
        {
            Initialize(type, 0);
        }

        protected override void Awake()
        {
            base.Awake();

            // BrickGame 레이어 설정 (Territory 카메라에서 제외)
            int brickLayer = LayerMask.NameToLayer("BrickGame");
            if (brickLayer >= 0)
                gameObject.layer = brickLayer;

            // SpriteRenderer 캐시
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // 트리거 모드 강제 설정 (Plank/BottomBoundary와 겹치면서 감지)
            isTrigger = true;

            // Stuck 감지 비활성화 (낙하 아이템은 항상 이동 중)
            enableStuckDetection = false;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // 서버에서만 낙하 속도 적용
            if (!HasAuthorityToModifyPhysics()) return;

            // Rigidbody2D를 통한 일정 속도 낙하
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector2.down * fallSpeed;
            }
        }

        #region 충돌 처리

        /// <summary>
        /// 트리거 충돌 감지 - Plank 획득 및 BottomBoundary 소멸
        /// </summary>
        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // Server에서만 충돌 처리
            if (!IsServer && IsSpawned) return;

            if (isCollected) return;

            // Plank(패들) 충돌 → 획득
            if (collision.gameObject.CompareTag("Plank") ||
                collision.gameObject.GetComponent<PhysicsPlank>() != null)
            {
                HandlePlankCollision(collision.gameObject);
                return;
            }

            // BottomBoundary 충돌 → 소멸 (효과 없이)
            if (collision.gameObject.CompareTag("BottomBoundary"))
            {
                HandleBottomBoundaryDespawn();
                return;
            }
        }

        /// <summary>
        /// Plank(패들) 충돌 시 처리 - 타입에 따른 효과 발동
        /// </summary>
        private void HandlePlankCollision(GameObject plankObject)
        {
            if (isCollected) return;

            // 멀티플레이어: 소유권 확인 - 같은 플레이어의 Plank인지 검증
            var plank = plankObject.GetComponent<PhysicsPlank>();
            if (plank != null && _ownerClientId != 0)
            {
                if (plank.OwnerClientId != _ownerClientId)
                {
                    return; // 다른 플레이어의 Plank와 충돌 시 무시
                }
            }

            isCollected = true;

            Debug.Log($"<color=green>[PowerUpItem] {_powerUpType} 획득! Plank: {plankObject.name}</color>");

            // 효과음 재생
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // 이펙트 생성
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // 타입별 효과 적용
            switch (_powerUpType)
            {
                case PowerUpType.Star:
                    ApplyStarEffect(plank);
                    break;
                case PowerUpType.BonusBall:
                    ApplyBonusBallEffect(plank);
                    break;
            }

            // 아이템 제거
            StartCoroutine(DestroyAfterDelay(0.1f));
        }

        /// <summary>
        /// BottomBoundary 충돌 시 효과 없이 소멸
        /// </summary>
        private void HandleBottomBoundaryDespawn()
        {
            if (isCollected) return;

            isCollected = true;

            Debug.Log($"<color=yellow>[PowerUpItem] {_powerUpType} BottomBoundary 도달 - 소멸</color>");

            StartCoroutine(DestroyAfterDelay(0.1f));
        }

        #endregion

        #region 효과 적용

        /// <summary>
        /// Star 효과: 같은 플레이어의 공 공격력 부스트
        /// </summary>
        private void ApplyStarEffect(PhysicsPlank plank)
        {
            // 활성 공 찾기 - 같은 플레이어 소유의 공에 PowerUp 적용
            var allBalls = FindObjectsOfType<PhysicsBall>();
            bool applied = false;

            foreach (var ball in allBalls)
            {
                if (ball == null || !ball.IsSpawned) continue;

                // 멀티플레이어: 같은 플레이어 소유의 공만 강화
                if (_ownerClientId != 0 && ball.OwnerClientId != _ownerClientId) continue;

                ball.PowerUp(powerIncrease, powerDuration);
                applied = true;
            }

            if (applied)
            {
                Debug.Log($"<color=cyan>[PowerUpItem] Star 효과 적용! 공격력 +{powerIncrease}, 지속시간: {powerDuration}초</color>");
            }
        }

        /// <summary>
        /// BonusBall 효과: Plank 위치에서 추가 공 생성
        /// </summary>
        private void ApplyBonusBallEffect(PhysicsPlank plank)
        {
            if (plank == null)
            {
                Debug.LogWarning("[PowerUpItem] BonusBall 효과 - Plank 참조 없음, 기존 공 복제로 대체");
                SpawnBonusBallFromExisting();
                return;
            }

            SpawnBonusBallAtPlank(plank);
        }

        /// <summary>
        /// Plank 위치에서 보너스 공 생성 (서버)
        /// </summary>
        private void SpawnBonusBallAtPlank(PhysicsPlank plank)
        {
            // 기존 공 찾기 (복제 템플릿으로 사용)
            var existingBall = FindExistingBallForOwner();
            if (existingBall == null)
            {
                Debug.LogWarning("[PowerUpItem] 보너스 공 생성 실패 - 기존 공을 찾을 수 없음");
                return;
            }

            // Plank 위에서 생성
            Vector3 spawnPosition = plank.transform.position + Vector3.up * 0.5f;
            GameObject newBallObj = Instantiate(existingBall.gameObject, spawnPosition, Quaternion.identity);

            // 태그 설정
            if (string.IsNullOrEmpty(newBallObj.tag) || newBallObj.tag == "Untagged")
            {
                newBallObj.tag = "Ball";
            }

            // PhysicsBall 설정 (Spawn 전에!)
            PhysicsBall newBall = newBallObj.GetComponent<PhysicsBall>();
            if (newBall != null)
            {
                newBall.SetPlankReference(plank);
            }

            // Configure → Spawn
            var netObj = newBallObj.GetComponent<NetworkObject>();
            MB.Infrastructure.Network.NetworkSpawnUtil.SpawnAsServer(netObj);

            // 발사 (Spawn 후 — 물리 적용)
            if (newBall != null)
            {
                float randomAngle = Random.Range(-45f, 45f);
                Vector2 launchDir = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                launchDir.Normalize();
                newBall.LaunchBall(launchDir);

                Debug.Log($"<color=cyan>[PowerUpItem] BonusBall 효과 - 보너스 공 발사! 방향: {launchDir}</color>");
            }
        }

        /// <summary>
        /// Plank 참조 없이 기존 공 위치에서 보너스 공 생성 (폴백)
        /// </summary>
        private void SpawnBonusBallFromExisting()
        {
            var existingBall = FindExistingBallForOwner();
            if (existingBall == null)
            {
                Debug.LogWarning("[PowerUpItem] 보너스 공 생성 실패 - 기존 공을 찾을 수 없음");
                return;
            }

            Vector3 spawnPosition = existingBall.transform.position + Vector3.up * 0.3f;
            GameObject newBallObj = Instantiate(existingBall.gameObject, spawnPosition, Quaternion.identity);

            if (string.IsNullOrEmpty(newBallObj.tag) || newBallObj.tag == "Untagged")
            {
                newBallObj.tag = "Ball";
            }

            PhysicsBall newBall = newBallObj.GetComponent<PhysicsBall>();
            if (newBall != null && existingBall.Plank != null)
            {
                newBall.SetPlankReference(existingBall.Plank);
            }

            var netObj2 = newBallObj.GetComponent<NetworkObject>();
            MB.Infrastructure.Network.NetworkSpawnUtil.SpawnAsServer(netObj2);

            if (newBall != null)
            {
                float randomAngle = Random.Range(-45f, 45f);
                Vector2 launchDir = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
                launchDir.Normalize();
                newBall.LaunchBall(launchDir);
            }
        }

        /// <summary>
        /// 같은 플레이어의 기존 공 찾기 (복제 템플릿용)
        /// </summary>
        private PhysicsBall FindExistingBallForOwner()
        {
            var allBalls = FindObjectsOfType<PhysicsBall>();

            // 먼저 같은 소유자의 공 찾기
            foreach (var ball in allBalls)
            {
                if (ball == null || !ball.IsSpawned) continue;
                if (_ownerClientId != 0 && ball.OwnerClientId != _ownerClientId) continue;
                return ball;
            }

            // 소유자 매칭 실패 시 아무 공이나 반환
            foreach (var ball in allBalls)
            {
                if (ball != null) return ball;
            }

            return null;
        }

        #endregion

        #region 시각 효과

        /// <summary>
        /// 파워업 타입에 따른 시각적 색상 적용
        /// </summary>
        private void ApplyVisualByType()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer == null) return;

            switch (_powerUpType)
            {
                case PowerUpType.Star:
                    _spriteRenderer.color = Color.yellow; // 노란색 - Star
                    break;
                case PowerUpType.BonusBall:
                    _spriteRenderer.color = Color.green; // 초록색 - BonusBall
                    break;
                default:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }

        #endregion

        #region 정리

        /// <summary>
        /// 지연 후 오브젝트 제거 (이펙트/사운드 재생 시간 확보)
        /// </summary>
        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // NetworkObject는 Server에서만 Despawn
            if (IsServer || !IsSpawned)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        #endregion
    }
}
