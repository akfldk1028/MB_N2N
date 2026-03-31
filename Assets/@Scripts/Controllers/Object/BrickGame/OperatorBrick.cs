using UnityEngine;
using TMPro;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 연산자 블록 - 충돌 시 점수에 연산 적용 (점수 = 총알 개수)
    /// + : 점수 더하기
    /// × : 점수 곱하기
    /// </summary>
    public class OperatorBrick : PhysicsObject
    {
        public enum OperatorType
        {
            Add,        // +
            Multiply    // ×
        }

        // NetworkVariable로 동기화
        private NetworkVariable<int> _networkOpType = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> _networkOpValue = new NetworkVariable<int>(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Operator Settings")]
        [SerializeField] private OperatorType operatorType = OperatorType.Add;
        [SerializeField] private int operatorValue = 2;

        [Header("Visual")]
        [SerializeField] private TextMeshPro operatorText;
        [SerializeField] private Renderer brickRenderer;

        // 색상 설정
        private static readonly Color AddColor = new Color(0.2f, 0.8f, 0.2f);      // 초록 (+)
        private static readonly Color MultiplyColor = new Color(1f, 0.8f, 0f);     // 금색 (×)

        #region Public Properties
        public OperatorType Type => operatorType;
        public int Value => operatorValue;

        public ulong OwnerClientId
        {
            get
            {
                var netObj = GetComponent<Unity.Netcode.NetworkObject>();
                return netObj != null ? netObj.OwnerClientId : 0;
            }
        }
        #endregion

        private void Start()
        {
            // 3DBrick 렌더러 우선, 없으면 SpriteRenderer
            var brick3D = transform.Find("3DBrick");
            if (brick3D != null)
            {
                brickRenderer = brick3D.GetComponent<Renderer>();
                // SpriteRenderer 비활성화 (3DBrick이 시각 담당, 겹침 방지)
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
            else if (brickRenderer == null)
            {
                brickRenderer = GetComponent<Renderer>();
            }

            // 텍스트 찾기
            if (operatorText == null)
            {
                Transform textTransform = transform.Find("brickWaveText");
                if (textTransform != null)
                    operatorText = textTransform.GetComponent<TextMeshPro>();
            }

            // BrickGame 레이어 설정 (Territory 카메라에서 제외용)
            int brickLayer = LayerMask.NameToLayer("BrickGame");
            if (brickLayer >= 0)
            {
                gameObject.layer = brickLayer;
                for (int i = 0; i < transform.childCount; i++)
                    transform.GetChild(i).gameObject.layer = brickLayer;
            }

            // NetworkVariable 콜백 등록 + 현재 값 적용
            _networkOpType.OnValueChanged += (prev, cur) => { operatorType = (OperatorType)cur; UpdateVisual(); };
            _networkOpValue.OnValueChanged += (prev, cur) => { operatorValue = cur; UpdateVisual(); };

            // 초기 동기화 값 적용 (Client에서 이미 복제된 값)
            if (_networkOpType.Value != 0 || _networkOpValue.Value != 2)
            {
                operatorType = (OperatorType)_networkOpType.Value;
                operatorValue = _networkOpValue.Value;
            }

            // 비주얼 업데이트
            UpdateVisual();
        }

        /// <summary>
        /// 연산자 타입과 값 설정 (스폰 시 호출)
        /// </summary>
        public void SetOperator(OperatorType type, int value)
        {
            operatorType = type;
            operatorValue = Mathf.Max(1, value); // 최소 1

            // 서버: NetworkVariable에 동기화
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                _networkOpType.Value = (int)type;
                _networkOpValue.Value = operatorValue;
            }

            UpdateVisual();
        }

        /// <summary>
        /// 비주얼 업데이트 (텍스트 + 색상)
        /// </summary>
        private void UpdateVisual()
        {
            // 텍스트 표시: "+5" 또는 "×3"
            if (operatorText != null)
            {
                string symbol = operatorType == OperatorType.Add ? "+" : "×";
                operatorText.text = $"{symbol}{operatorValue}";
            }

            // 색상 설정
            if (brickRenderer != null)
            {
                Color targetColor = operatorType == OperatorType.Add ? AddColor : MultiplyColor;
                brickRenderer.material.color = targetColor;
            }
        }

        // 충돌 처리
        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);
            HandleBallCollision(collision);
        }

        /// <summary>
        /// 소유권 확인 (멀티플레이어)
        /// </summary>
        private bool CheckOwnership(GameObject ballObject)
        {
            if (MultiplayerUtil.IsSinglePlayer())
                return true;

            var brickNetObj = GetComponent<Unity.Netcode.NetworkObject>();
            var ballNetObj = ballObject.GetComponent<Unity.Netcode.NetworkObject>();

            if (brickNetObj == null || ballNetObj == null)
                return true;

            return brickNetObj.OwnerClientId == ballNetObj.OwnerClientId;
        }

        /// <summary>
        /// 공 충돌 처리 - 점수 연산 적용
        /// </summary>
        private void HandleBallCollision(Collision2D collision)
        {
            // 소유권 확인
            if (!CheckOwnership(collision.gameObject))
                return;

            // Ball/PhysicsBall 확인
            PhysicsBall ball = collision.gameObject.GetComponent<PhysicsBall>();
            if (ball == null)
                return;

            // 점수 연산 적용
            ApplyOperatorToScore();

            // 이펙트 (선택사항)
            PlayHitEffect();

            // 블록 파괴
            GameLogger.Success("OperatorBrick", $"[{operatorType}] {operatorValue} 적용! 블록 파괴");
            Destroy(gameObject);
        }

        /// <summary>
        /// 점수에 연산 적용 (Score = 총알 개수)
        /// </summary>
        private void ApplyOperatorToScore()
        {
            if (MultiplayerUtil.IsMultiplayer())
            {
                // 멀티플레이어: 해당 플레이어 점수에 적용
                var netObj = GetComponent<Unity.Netcode.NetworkObject>();
                if (netObj != null)
                {
                    ulong ownerClientId = netObj.OwnerClientId;
                    var playerGame = Managers.Game?.GetPlayerGame(ownerClientId);

                    if (playerGame != null)
                    {
                        int currentScore = playerGame.Score;
                        int newScore = CalculateNewScore(currentScore);
                        int scoreToAdd = newScore - currentScore;

                        if (scoreToAdd > 0)
                        {
                            playerGame.AddScore(scoreToAdd);
                            GameLogger.Info("OperatorBrick",
                                $"[Player {ownerClientId}] Score: {currentScore} → {newScore} ({GetOperatorString()})");
                        }
                    }
                }
            }
            else
            {
                // 싱글플레이어
                var brickGame = Managers.Game?.BrickGame;
                if (brickGame != null)
                {
                    int currentScore = brickGame.Score;
                    int newScore = CalculateNewScore(currentScore);
                    int scoreToAdd = newScore - currentScore;

                    if (scoreToAdd > 0)
                    {
                        brickGame.AddScore(scoreToAdd);
                        GameLogger.Info("OperatorBrick",
                            $"Score: {currentScore} → {newScore} ({GetOperatorString()})");
                    }
                }
            }
        }

        /// <summary>
        /// 새 점수 계산
        /// </summary>
        private int CalculateNewScore(int currentScore)
        {
            switch (operatorType)
            {
                case OperatorType.Add:
                    return currentScore + operatorValue;

                case OperatorType.Multiply:
                    return currentScore * operatorValue;

                default:
                    return currentScore;
            }
        }

        /// <summary>
        /// 연산자 문자열 (로그용)
        /// </summary>
        private string GetOperatorString()
        {
            string symbol = operatorType == OperatorType.Add ? "+" : "×";
            return $"{symbol}{operatorValue}";
        }

        /// <summary>
        /// 히트 이펙트 (추후 확장)
        /// </summary>
        private void PlayHitEffect()
        {
            // TODO: 파티클 이펙트, 사운드 등
        }
    }
}
