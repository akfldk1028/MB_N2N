using UnityEngine;
using TMPro;
using Unity.Assets.Scripts.Objects;

namespace Unity.Assets.Scripts.Objects
{
    // Star 아이템 구현 - 공의 공격력을 강화시켜주는 아이템
    public class Star : PhysicsObject
    {
        [Header("Star Settings")]
        [SerializeField] private int powerIncrease = 1; // 공의 공격력 증가량
        [SerializeField] private float powerDuration = 15f; // 강화 효과 지속 시간 (초)
        [SerializeField] private AudioClip collectSound; // 효과음
        [SerializeField] private GameObject collectEffect; // 수집 이펙트
        [SerializeField] private Renderer starRenderer; // Reference to the star's renderer for color changes
        
        private bool isCollected = false;
        
        private void Start()
        {
            // 태그 설정 (없는 경우)
            if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
            {
                gameObject.tag = "Star";
            }
            
            if (starRenderer == null)
            {
                starRenderer = GetComponent<Renderer>();
            }
        }
        
        // PhysicsObject에서 상속받은 Update 또는 FixedUpdate 사용
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 로직 호출
            
            // 추가 로직 - 회전 애니메이션 등을 여기에 추가할 수 있음
            transform.Rotate(0, 0, 30 * Time.deltaTime); // 간단한 회전 효과
        }
        
        // OnTriggerEnter2D 구현 (Star는 트리거로 설정)
        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            // ✅ Server에서만 충돌 처리!
            if (!IsServer && IsSpawned) return;

            Debug.Log($"<color=cyan>[Star] OnTriggerEnter2D: {collision.gameObject.name}, Tag: {collision.gameObject.tag}</color>");

            if (isCollected) return;

            // 공과 충돌 감지 - 여기서 태그 또는 컴포넌트로 확인
            if (collision.gameObject.CompareTag("Ball") ||
                collision.gameObject.GetComponent<PhysicsBall>() != null)
            {
                HandleTriggerCollision(collision.gameObject);
            }
        }
        
        // 일반 충돌도 감지 (PhysicsObject 상속)
        private void HandleTriggerCollision(GameObject ballObject)
        {
            
            if (isCollected) return;

            isCollected = true;

            PhysicsBall ball = ballObject.GetComponent<PhysicsBall>();
            
            if (ball != null)
            {
                // 공격력 증가 (예: 1만큼 증가, 15초 지속)
                ball.PowerUp(1, 15f);
            }
            // 공과 충돌 감지
            StartCoroutine(DestroyAfterDelay(0.1f));

        }
        
        // 공과 충돌 시 처리
        private void HandleStarCollision(GameObject ballObject)
        {
            // 이미 처리됐으면 중복 처리 방지
            if (isCollected) return;
            
            isCollected = true;
            
            Debug.Log($"<color=green>[{gameObject.name}] Star 충돌 처리 시작! 공 공격력 {powerIncrease} 증가, 지속시간: {powerDuration}초</color>");
            
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
            
            // 공격력 증가 적용
            IncreaseGlobalBallPower();
            
            // 통계 업데이트
            PlayerPrefs.SetInt("numberOfStars", PlayerPrefs.GetInt("numberOfStars") + 1);
            
            // 업적 체크 (주석 해제하여 사용)
            /*
            if (PlayerPrefs.GetInt("numberOfStars") - PlayerPrefs.GetInt("starsSpent") >= 1000)
            {
                if (PlayerPrefs.GetInt("collect1000Stars") != 1)
                {
                    PlayerPrefs.SetInt("collect1000Stars", 1);
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null)
                    {
                        var achievement = canvas.GetComponent<AchievementUnlocked>();
                        if (achievement != null)
                        {
                            achievement.enabled = true;
                            achievement.NameOfTheAchievement("collect 1000 stars");
                        }
                    }
                }
            }
            */
            
            // 오브젝트 제거
            StartCoroutine(DestroyAfterDelay(0.1f));
        }
        
        // 약간의 지연 후에 오브젝트 제거 (이펙트 및 사운드가 재생될 시간 확보)
        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // ✅ NetworkObject는 Server에서만 Despawn!
            if (IsServer || !IsSpawned)
            {
                var netObj = GetComponent<Unity.Netcode.NetworkObject>();
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
        
        // 글로벌 공 공격력 증가
        private void IncreaseGlobalBallPower()
        {
            // 공격력 증가
            BallPowerManager.IncreasePower(powerIncrease, powerDuration);
            
            // UI 업데이트 (있을 경우)
            UpdatePowerUI();
        }
        
        // 파워 UI 업데이트 (필요시 구현)
        private void UpdatePowerUI()
        {
            // 예: 게임 내 파워 표시 UI 업데이트
            TextMeshPro powerText = GameObject.Find("PowerLevelText")?.GetComponent<TextMeshPro>();
            if (powerText != null)
            {
                powerText.text = $"Power: {BallPowerManager.CurrentPower}";
            }
            
            // 타이머 UI 추가 (선택 사항)
            TextMeshPro timerText = GameObject.Find("PowerTimerText")?.GetComponent<TextMeshPro>();
            if (timerText != null)
            {
                timerText.text = $"Time: {BallPowerManager.RemainingTime:F1}s";
            }
        }
    }
    
    // 공의 공격력을 관리하는 글로벌 매니저 클래스
    public static class BallPowerManager
    {
        // 현재 공의 공격력 (기본값 1)
        private static int currentPower = 1;
        
        // 강화 타이머
        private static float powerTimer = 0f;
        
        // 현재 공격력 (읽기 전용)
        public static int CurrentPower => currentPower;
        
        // 남은 시간 (읽기 전용)
        public static float RemainingTime => powerTimer;
        
        // 공격력 증가 메서드
        public static void IncreasePower(int amount, float duration)
        {
            currentPower += amount;
            powerTimer = Mathf.Max(powerTimer, duration); // 더 긴 지속시간 적용
 
        }
        
        // 타이머 업데이트
        public static void UpdateTimer(float deltaTime)
        {
            if (powerTimer > 0)
            {
                powerTimer -= deltaTime;
                
                // 타이머 종료 시 공격력 리셋
                if (powerTimer <= 0)
                {
                    currentPower = 1; // 기본 공격력으로 리셋
                    powerTimer = 0;
                }
            }
        }
        

    }
}