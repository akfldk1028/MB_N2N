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
        
        // BricksWave 로직 통합
        private int wave = 1;
        private int originalWave = 1; // 원래 wave 값 저장 (점수 계산용)
        private TextMeshPro waveText;
        private AudioSource brickHitSound;
        [SerializeField] private Renderer brickRenderer; // Reference to the brick's renderer for color changes
        
        private void Start()
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
            
            // 게임 매니저에 게임 오버 알림
            if (Managers.Game?.BrickGame != null)
            {
                Managers.Game.BrickGame.GameOver();
            }
        }
        
        // 충돌 처리 (BricksWave 로직 통합)
        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);
            
            HandleBallCollision(collision);
        }
        
        // 공과 충돌 시 처리
        private void HandleBallCollision(Collision2D collision)
        {
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
                // 원래 wave 값에 따른 점수 추가
                if (Managers.Game?.BrickGame != null)
                {
                    Managers.Game.BrickGame.AddScore(originalWave);
                }
                
                HandleBrickDestruction();
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 벽돌이 파괴될 때 호출되는 로직
        /// </summary>
        private void HandleBrickDestruction()
        {
            // 업적 및 점수 추적
            int bricksDestroyed = PlayerPrefs.GetInt("numberOfBricksDestroyed", 0) + 1;
            PlayerPrefs.SetInt("numberOfBricksDestroyed", bricksDestroyed);
            
            // 업적 확인 (필요한 경우)
            // CheckAndUnlockAchievement(bricksDestroyed, 100, "destroy100bricks", "destroy 100 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 1000, "destroy1000bricks", "destroy 1000 bricks");
            // CheckAndUnlockAchievement(bricksDestroyed, 10000, "destroy10000bricks", "destroy 10000 bricks");
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
        private void ColorBrick()
        {
            if (brickRenderer == null) return;
            Debug.Log($"[{gameObject.name}] ColorBrick: brickRenderer = {brickRenderer}");
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