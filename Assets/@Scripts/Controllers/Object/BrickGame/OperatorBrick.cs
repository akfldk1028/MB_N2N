using UnityEngine;
using TMPro;
using Unity.Assets.Scripts.Objects;

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
            // 컴포넌트 캐싱
            if (brickRenderer == null)
                brickRenderer = GetComponent<Renderer>();

            // 텍스트 찾기
            if (operatorText == null)
            {
                Transform textTransform = transform.Find("brickWaveText");
                if (textTransform != null)
                    operatorText = textTransform.GetComponent<TextMeshPro>();
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
