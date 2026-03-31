using UnityEngine;
using TMPro;
using MB.Infrastructure.Messages;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Objects
{
    // Brick 오브젝트의 동작을 정의합니다.
    // PhysicsObject를 상속받아 기본적인 물리 상호작용 기능을 가집니다.
    public class Brick : PhysicsObject
    {
        // 게임 오버가 발생하는 Y 경계선 (ObjectPlacement와 동일한 값 사용)
        private const float bottomBoundary = -2.3f;
        private bool isGameOverTriggered = false; // 게임 오버 중복 호출 방지

        // BricksWave 로직 통합 — NetworkVariable로 동기화
        private NetworkVariable<int> _networkWave = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private int wave = 1;
        private int originalWave = 1; // 원래 wave 값 저장 (점수 계산용)
        private TextMeshPro waveText;
        private AudioSource brickHitSound;
        [SerializeField] private SpriteRenderer brickSpriteRenderer; // SpriteRenderer for color changes
        private MeshRenderer _3dBrickRenderer; // 3DBrick 메시 렌더러

        #region Public Properties (총알 시스템용)
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
        public int Health => _networkWave?.Value ?? wave;
        #endregion
        
        private void Start()
        {


            // SpriteRenderer 캐싱 (프리팹 직렬화 값이 잘못된 경우 대비)
            if (brickSpriteRenderer == null)
            {
                brickSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            // 3DBrick 렌더러 캐싱 + SpriteRenderer 비활성화 (3DBrick이 시각 담당)
            var brick3D = transform.Find("3DBrick");
            if (brick3D != null)
            {
                _3dBrickRenderer = brick3D.GetComponent<MeshRenderer>();
                // 3DBrick이 있으면 SpriteRenderer는 렌더링하지 않음 (겹침 방지)
                if (brickSpriteRenderer != null)
                    brickSpriteRenderer.enabled = false;
            }

            // BrickGame 레이어 설정 (Territory 카메라에서 제외용)
            int brickLayer = LayerMask.NameToLayer("BrickGame");
            if (brickLayer >= 0)
                SetLayerRecursively(gameObject, brickLayer);
            
            Transform textTransform = transform.Find("brickWaveText");
            if (textTransform != null)
            {
                waveText = textTransform.GetComponent<TextMeshPro>();
            }

            // 서버: wave 결정 → NetworkVariable에 설정
            var nm = NetworkManager.Singleton;
            bool isServer = nm != null && nm.IsServer;

            if (isServer)
            {
                int newWave = CommonVars.level < 10 ?
                    Random.Range(1, 3) :
                    Random.Range(CommonVars.level / 5, CommonVars.level / 2);
                _networkWave.Value = newWave;
            }

            // 현재 값 적용 (서버에서 이미 설정됐거나, 클라이언트에서 복제된 값)
            ApplyWave(_networkWave.Value);

            // 색상 업데이트
            ColorBrick();

            // BrickManager에 등록
            Managers.Game?.BrickGame?.Brick?.RegisterBrick(this);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkWave.OnValueChanged += OnWaveChanged;
            // Spawn 시 현재 값 즉시 적용
            ApplyWave(_networkWave.Value);
        }

        public override void OnNetworkDespawn()
        {
            _networkWave.OnValueChanged -= OnWaveChanged;
            base.OnNetworkDespawn();
        }

        // PhysicsObject에서 상속받은 Update 또는 FixedUpdate 사용
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 로직 호출
            
            // 오브젝트가 경계선 아래로 내려갔는지 확인
            if (!isGameOverTriggered && transform.position.y < bottomBoundary)
            {
                // ✅ 멀티플레이어: 벽돌 바닥 도달 = 게임오버 아님 (승리 조건은 대포 파괴)
                if (MultiplayerUtil.IsMultiplayer())
                {
                    // 바닥 도달 벽돌은 Despawn만
                    isGameOverTriggered = true;
                    var netObj = GetComponent<Unity.Netcode.NetworkObject>();
                    if (netObj != null && netObj.IsSpawned && MultiplayerUtil.IsServer())
                        netObj.Despawn();
                    else
                        Destroy(gameObject);
                }
                else
                {
                    TriggerGameOver();
                }
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
            // ✅ 서버 권한: Client는 충돌 처리 스킵 (OnWaveChanged로 비주얼 동기화)
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening && !nm.IsServer)
                return;

            // ✅ 멀티플레이어: 소유권 확인
            if (!CheckOwnership(collision.gameObject))
                return;

            PhysicsBall ball = collision.gameObject.GetComponent<PhysicsBall>();
            int attackPower = ball != null ? ball.AttackPower : 1;

            // 서버: NetworkVariable로 wave 감소 → Client에 자동 동기화
            _networkWave.Value -= attackPower;
            wave = _networkWave.Value;

            ColorBrick();
            if (waveText != null)
                waveText.text = wave.ToString();

            if (wave <= 0)
            {
                AddScoreToPlayer(originalWave);
                HandleBrickDestruction();
                DestroyBrickSafely();
            }
        }
        
        /// <summary>
        /// 플레이어별 점수 추가
        /// ✅ MultiplayerUtil 사용
        /// </summary>
        private void AddScoreToPlayer(int score)
        {
            if (MultiplayerUtil.IsMultiplayer())
            {
                // 멀티플레이어 모드: 해당 플레이어에게 점수 추가
                Unity.Netcode.NetworkObject brickNetObj = GetComponent<Unity.Netcode.NetworkObject>();
                if (brickNetObj != null)
                {
                    ulong ownerClientId = brickNetObj.OwnerClientId;
                    var playerGame = Managers.Game?.GetPlayerGame(ownerClientId);
                    playerGame?.AddScore(score);
                    GameLogger.Info("Brick", $"[Player {ownerClientId}] 점수 +{score}");
                }
            }
            else
            {
                // 싱글플레이어 모드
                Managers.Game?.BrickGame?.AddScore(score);
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

            // ✅ 서버 권한: Client는 스킵 (OnWaveChanged로 동기화)
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening && !nm.IsServer)
                return;

            _networkWave.Value -= damage;
            wave = _networkWave.Value;
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

        /// <summary>
        /// 벽돌이 파괴될 때 호출되는 로직
        /// </summary>
        private void HandleBrickDestruction()
        {
            // BrickManager에 파괴 통보
            Managers.Game?.BrickGame?.Brick?.NotifyBrickDestroyed(this, originalWave);

            // 업적 및 점수 추적
            int bricksDestroyed = PlayerPrefs.GetInt("numberOfBricksDestroyed", 0) + 1;
            PlayerPrefs.SetInt("numberOfBricksDestroyed", bricksDestroyed);

            // 업적 확인 (필요한 경우)
            // CheckAndUnlockAchievement(bricksDestroyed, 100, "destroy100bricks", "destroy 100 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 1000, "destroy1000bricks", "destroy 1000 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 10000, "destroy10000bricks", "destroy 10000 bricks");

            // ✅ 벽돌 파괴 이벤트 발행 (위치 + 소유자 정보 포함)
            // PowerUpDropManager 등이 구독하여 아이템 드롭 등을 처리
            Managers.PublishAction(ActionId.BrickGame_BrickDestroyed,
                new BrickGameBrickDestroyedPayload(originalWave, transform.position, OwnerClientId));
        }

        /// <summary>
        /// 벽돌 안전하게 파괴 (NetworkObject 지원)
        /// ✅ NetworkObject는 Despawn() 먼저 호출해야 클라이언트 동기화 오류 방지
        /// ✅ SERVER에서만 Despawn 가능!
        /// </summary>
        private void DestroyBrickSafely()
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
        protected virtual void ColorBrick()
        {
            Color brickColor;
            if (wave <= 30)
            {
                brickColor = new Color(1, 1 - (wave / 30f), 0); // 노란색에서 빨간색으로 전환
            }
            else if (wave <= 60)
            {
                brickColor = new Color(1, 0, (wave - 30) / 30f); // 빨간색에서 보라색으로 전환
            }
            else
            {
                float redColorValue = 1 - ((wave - 60) / 30f);
                brickColor = new Color(Mathf.Max(redColorValue, 0), 0, 1); // 보라색에서 파란색으로 전환
            }

            // 3DBrick 메시에 색상 적용
            if (_3dBrickRenderer != null)
            {
                _3dBrickRenderer.material.color = brickColor;
            }
            // fallback: 3DBrick 없으면 SpriteRenderer 사용
            else if (brickSpriteRenderer != null)
            {
                brickSpriteRenderer.color = brickColor;
            }
        }

        private void OnWaveChanged(int previousValue, int newValue)
        {
            ApplyWave(newValue);
        }

        private void ApplyWave(int newWave)
        {
            wave = newWave;
            originalWave = Mathf.Max(originalWave, newWave); // 최초 값 유지
            if (waveText != null)
                waveText.text = wave.ToString();
            ColorBrick();
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
        }
    }
}