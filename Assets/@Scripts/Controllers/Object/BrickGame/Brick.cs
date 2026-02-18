using UnityEngine;
using TMPro;
using Unity.Assets.Scripts.Objects;

namespace Unity.Assets.Scripts.Objects
{
    // Brick 오브젝트의 동작을 정의합니다.
    // PhysicsObject를 상속받아 기본적인 물리 상호작용 기능을 가집니다.
    public class Brick : PhysicsObject
    {
        // 게임 오버가 발생하는 Y 경계선 (ObjectPlacement와 동일한 값 사용)
        private const float bottomBoundary = -2.3f;
        private bool isGameOverTriggered = false; // 게임 오버 중복 호출 방지

        // 특수 벽돌 타입 (기본: Normal)
        protected BrickType brickType = BrickType.Normal;
        private bool isItemBrick = false; // Item 벽돌 파워업 드롭 플래그

        // BricksWave 로직 통합
        protected int wave = 1;
        protected int originalWave = 1; // 원래 wave 값 저장 (점수 계산용)
        protected TextMeshPro waveText;
        private AudioSource brickHitSound;
        [SerializeField] protected Renderer brickRenderer; // Reference to the brick's renderer for color changes

        #region Public Properties (총알 시스템용)
        /// <summary>
        /// 벽돌 타입 (Normal, Explosion, Steel, Item, Gold)
        /// </summary>
        public BrickType Type => brickType;

        /// <summary>
        /// 벽돌 소유자 Client ID (NetworkObject에서 가져옴)
        /// </summary>
        public ulong OwnerClientId
        {
            get
            {
                var netObj = GetComponent<Unity.Netcode.NetworkObject>();
                return netObj != null ? netObj.OwnerClientId : 0;
            }
        }

        /// <summary>
        /// 현재 체력 (wave)
        /// </summary>
        public int Health => wave;
        #endregion
        
        protected virtual void Start()
        {


            // 필요한 컴포넌트 캐싱
            if (brickRenderer == null)
            {
                brickRenderer = GetComponent<Renderer>();
            }
            
            Transform textTransform = transform.Find("brickWaveText");
            if (textTransform != null)
            {
                waveText = textTransform.GetComponent<TextMeshPro>();
                
                // 레벨에 따라 벽돌을 부수는데 필요한 타격 횟수 결정
                wave = CommonVars.level < 10 ? 
                    Random.Range(1, 3) : 
                    Random.Range(CommonVars.level / 5, CommonVars.level / 2);
                
                // 원래 wave 값 저장 (점수 계산용)
                originalWave = wave;
                
                waveText.text = wave.ToString();
            }
            
            // 색상 업데이트
            ColorBrick();
        }
        
        // PhysicsObject에서 상속받은 Update 또는 FixedUpdate 사용
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 로직 호출
            
            // 게임 오버 상태가 아니고, 오브젝트가 경계선 아래로 내려갔는지 확인
            if (!isGameOverTriggered && transform.position.y < bottomBoundary)
            {
                TriggerGameOver();
            }
        }
        
        private void TriggerGameOver()
        {
            isGameOverTriggered = true;
            Debug.LogError($"[Brick] 게임 오버: 벽돌 {gameObject.name}이 바닥 경계선({bottomBoundary})에 도달했습니다!");

            // ✅ MultiplayerUtil 사용
            if (MultiplayerUtil.IsMultiplayer())
            {
                // 멀티플레이어 모드: 해당 플레이어의 게임만 오버
                Unity.Netcode.NetworkObject brickNetObj = GetComponent<Unity.Netcode.NetworkObject>();
                if (brickNetObj != null)
                {
                    ulong ownerClientId = brickNetObj.OwnerClientId;
                    var playerGame = Managers.Game?.GetPlayerGame(ownerClientId);
                    playerGame?.GameOver();
                    GameLogger.Warning("Brick", $"[Player {ownerClientId}] 게임 오버!");
                }
            }
            else
            {
                // 싱글플레이어 모드
                Managers.Game?.BrickGame?.GameOver();
            }
        }
        
        // 충돌 처리 (BricksWave 로직 통합)
        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);

            HandleBallCollision(collision);
        }

        /// <summary>
        /// 멀티플레이어: 벽돌과 공의 소유권이 일치하는지 확인
        /// ✅ MultiplayerUtil 사용
        /// </summary>
        private bool CheckOwnership(GameObject ballObject)
        {
            // 싱글플레이어: 항상 충돌 허용
            if (MultiplayerUtil.IsSinglePlayer())
            {
                return true;
            }

            // NetworkObject 컴포넌트 확인
            Unity.Netcode.NetworkObject brickNetObj = GetComponent<Unity.Netcode.NetworkObject>();
            Unity.Netcode.NetworkObject ballNetObj = ballObject.GetComponent<Unity.Netcode.NetworkObject>();

            // NetworkObject가 없으면 충돌 허용
            if (brickNetObj == null || ballNetObj == null)
            {
                return true;
            }

            // 멀티플레이어: OwnerClientId 비교
            bool isSameOwner = brickNetObj.OwnerClientId == ballNetObj.OwnerClientId;

            if (!isSameOwner)
            {
                GameLogger.Info("Brick", $"충돌 무시: Brick Owner={brickNetObj.OwnerClientId}, Ball Owner={ballNetObj.OwnerClientId}");
            }

            return isSameOwner;
        }
        
        // 공과 충돌 시 처리
        protected virtual void HandleBallCollision(Collision2D collision)
        {
            // ✅ 멀티플레이어: 소유권 확인
            if (!CheckOwnership(collision.gameObject))
            {
                // 소유권이 다른 플레이어의 공이면 충돌 무시
                return;
            }

            // 효과음 재생 (필요한 경우)
            /*
            if (brickHitSound != null && !brickHitSound.isPlaying)
            {
                brickHitSound.Play();
            }
            */

            // 체력(wave) 감소 및 시각적 업데이트
            // wave--;
                // PhysicsBall 컴포넌트 참조 얻기
            PhysicsBall ball = collision.gameObject.GetComponent<PhysicsBall>();

            // 현재 공의 공격력 (없으면 기본값 1 사용)
            int attackPower = ball != null ? ball.AttackPower : 1;
            
            // 체력(wave) 감소 - 공격력만큼 차감
            wave -= attackPower;


            ColorBrick();
            
            if (waveText != null)
            {
                waveText.text = wave.ToString();
            }
            
            // 체력이 0이 되면 벽돌 파괴
            if (wave <= 0)
            {
                // ✅ 원래 wave 값에 따른 점수 추가 (플레이어별)
                AddScoreToPlayer(originalWave);

                HandleBrickDestruction();
                DestroyBrickSafely();
            }
        }
        
        /// <summary>
        /// 플레이어별 점수 추가
        /// ✅ MultiplayerUtil 사용
        /// </summary>
        protected virtual void AddScoreToPlayer(int score)
        {
            // ✅ Gold 벽돌: 점수 5배 보정
            int finalScore = (brickType == BrickType.Gold) ? score * 5 : score;

            if (MultiplayerUtil.IsMultiplayer())
            {
                // 멀티플레이어 모드: 해당 플레이어에게 점수 추가
                Unity.Netcode.NetworkObject brickNetObj = GetComponent<Unity.Netcode.NetworkObject>();
                if (brickNetObj != null)
                {
                    ulong ownerClientId = brickNetObj.OwnerClientId;
                    var playerGame = Managers.Game?.GetPlayerGame(ownerClientId);
                    playerGame?.AddScore(finalScore);
                    GameLogger.Info("Brick", $"[Player {ownerClientId}] 점수 +{finalScore}{(brickType == BrickType.Gold ? " (Gold 5x)" : "")}");
                }
            }
            else
            {
                // 싱글플레이어 모드
                Managers.Game?.BrickGame?.AddScore(finalScore);
            }
        }

        #region Public Methods (총알 시스템용)
        /// <summary>
        /// 외부에서 데미지를 주는 메서드 (총알 충돌 등)
        /// </summary>
        /// <param name="damage">데미지 양</param>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            wave -= damage;
            ColorBrick();

            if (waveText != null)
            {
                waveText.text = wave.ToString();
            }

            GameLogger.DevLog("Brick", $"[{gameObject.name}] 데미지 {damage} 받음, 남은 체력: {wave}");

            if (wave <= 0)
            {
                // 점수 추가 (원래 wave 값 기준)
                AddScoreToPlayer(originalWave);
                HandleBrickDestruction();
                DestroyBrickSafely();
            }
        }
        #endregion

        #region Special Brick Type Initialization
        /// <summary>
        /// 스폰 시 벽돌 타입 초기화 (BrickManager/ObjectPlacement에서 호출)
        /// Gold: wave=1, originalWave=1 (점수 5배 보정)
        /// Item: 파워업 드롭 플래그 설정
        /// </summary>
        public virtual void InitializeBrickType(BrickType type)
        {
            brickType = type;

            switch (type)
            {
                case BrickType.Gold:
                    // Gold 벽돌: HP=1, 점수 5배
                    wave = 1;
                    originalWave = 1;
                    if (waveText != null)
                        waveText.text = wave.ToString();
                    break;

                case BrickType.Item:
                    // Item 벽돌: 파괴 시 Star 파워업 드롭
                    isItemBrick = true;
                    break;
            }

            // 타입에 맞는 색상 업데이트
            ColorBrick();
        }
        #endregion

        /// <summary>
        /// 벽돌이 파괴될 때 호출되는 로직
        /// </summary>
        protected virtual void HandleBrickDestruction()
        {
            // 업적 및 점수 추적
            int bricksDestroyed = PlayerPrefs.GetInt("numberOfBricksDestroyed", 0) + 1;
            PlayerPrefs.SetInt("numberOfBricksDestroyed", bricksDestroyed);

            // 업적 확인 (필요한 경우)
            // CheckAndUnlockAchievement(bricksDestroyed, 100, "destroy100bricks", "destroy 100 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 1000, "destroy1000bricks", "destroy 1000 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 10000, "destroy10000bricks", "destroy 10000 bricks");

            // ✅ Item 벽돌: 파괴 시 Star 파워업 드롭
            if (isItemBrick)
            {
                SpawnStarAtPosition();
            }
        }

        /// <summary>
        /// Item 벽돌 파괴 시 Star 파워업 아이템 스폰
        /// Server Authority: 서버에서만 스폰
        /// </summary>
        private void SpawnStarAtPosition()
        {
            // 서버 권한 확인 (멀티플레이어에서는 서버만 스폰)
            var netObj = GetComponent<Unity.Netcode.NetworkObject>();
            bool isNetworkMode = netObj != null && netObj.IsSpawned;
            if (isNetworkMode && Unity.Netcode.NetworkManager.Singleton != null &&
                !Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                return; // 클라이언트에서는 스폰하지 않음
            }

            // Star 프리팹 로드
            GameObject starPrefab = Managers.Resource.Load<GameObject>("star");
            if (starPrefab == null)
            {
                GameLogger.Warning("Brick", "Star 프리팹을 로드할 수 없습니다 (Item 벽돌 파워업 드롭 실패)");
                return;
            }

            // Star 스폰 (벽돌 위치에)
            GameObject star = Object.Instantiate(starPrefab, transform.position, Quaternion.identity);

            // ✅ 멀티플레이어: NetworkObject 스폰 처리
            if (isNetworkMode)
            {
                var starNetObj = star.GetComponent<Unity.Netcode.NetworkObject>();
                if (starNetObj != null)
                {
                    starNetObj.SpawnWithOwnership(netObj.OwnerClientId);
                    GameLogger.Info("Brick", $"[Item 벽돌] Star 파워업 스폰 완료 (Owner: {netObj.OwnerClientId})");
                }
                else
                {
                    GameLogger.Warning("Brick", "Star 프리팹에 NetworkObject 컴포넌트가 없습니다");
                }
            }
            else
            {
                GameLogger.Info("Brick", "[Item 벽돌] Star 파워업 스폰 완료 (싱글플레이어)");
            }
        }

        /// <summary>
        /// 벽돌 안전하게 파괴 (NetworkObject 지원)
        /// ✅ NetworkObject는 Despawn() 먼저 호출해야 클라이언트 동기화 오류 방지
        /// ✅ SERVER에서만 Despawn 가능!
        /// </summary>
        protected void DestroyBrickSafely()
        {
            var netObj = GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                // ✅ SERVER에서만 Despawn 가능!
                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsServer)
                {
                    netObj.Despawn(); // 네트워크 정리 후 자동 Destroy
                }
                // CLIENT에서는 아무것도 안 함 - SERVER가 Despawn하면 자동으로 동기화됨
            }
            else
            {
                Destroy(gameObject); // 로컬 전용 (싱글플레이어)
            }
        }

        /// <summary>
        /// 업적 해금 확인 및 처리
        /// </summary>
        private void CheckAndUnlockAchievement(int bricksDestroyed, int threshold, string achievementKey, string achievementName)
        {
            if (bricksDestroyed >= threshold && PlayerPrefs.GetInt(achievementKey, 0) != 1)
            {
                PlayerPrefs.SetInt(achievementKey, 1);
                // 업적 UI 표시 (필요한 경우)
                /*
                AchievementUnlocked achievementUI = GameObject.Find("Canvas").GetComponent<AchievementUnlocked>();
                if (achievementUI != null)
                {
                    achievementUI.enabled = true;
                    achievementUI.NameOfTheAchievement(achievementName);
                }
                */
            }
        }
        
        /// <summary>
        /// 남은 체력(wave)에 따라 벽돌 색상 조정
        /// </summary>
        // 특수 벽돌 색상 상수
        private static readonly Color GoldBrickColor = new Color(1f, 0.84f, 0f);        // 금색
        private static readonly Color ItemBrickColor = new Color(0.6f, 0.2f, 0.9f);     // 무지개/보라색

        protected virtual void ColorBrick()
        {
            if (brickRenderer == null) return;

            // ✅ 특수 벽돌 타입 색상 우선 적용
            switch (brickType)
            {
                case BrickType.Gold:
                    brickRenderer.material.color = GoldBrickColor;
                    return;
                case BrickType.Item:
                    brickRenderer.material.color = ItemBrickColor;
                    return;
            }

            // 기본 벽돌 (Normal): 남은 체력(wave)에 따라 색상 전환
            if (wave <= 30)
            {
                brickRenderer.material.color = new Color(1, 1 - (wave / 30f), 0); // 노란색에서 빨간색으로 전환
            }
            else if (wave <= 60)
            {
                brickRenderer.material.color = new Color(1, 0, (wave - 30) / 30f); // 빨간색에서 보라색으로 전환
            }
            else
            {
                float redColorValue = 1 - ((wave - 60) / 30f);
                brickRenderer.material.color = new Color(Mathf.Max(redColorValue, 0), 0, 1); // 보라색에서 파란색으로 전환
            }
        }
    }
}