using UnityEngine;
using Unity.Assets.Scripts.Objects;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 강철 벽돌 - 공으로 파괴 불가, 총알(BrickGameBullet)로만 파괴
    /// Brick을 상속하여 공 충돌 시 데미지를 무시하고 회색 메탈릭 비주얼 제공
    /// TakeDamage()는 기본 Brick 로직 그대로 사용 (총알로 파괴 가능)
    /// </summary>
    public class SteelBrick : Brick
    {
        // 회색 메탈릭 색상
        private static readonly Color SteelBrickColor = new Color(0.6f, 0.6f, 0.65f);

        /// <summary>
        /// 공 충돌 시 처리 - 강철 벽돌은 데미지를 받지 않음
        /// 물리 충돌(반사)은 PhysicsObject에서 자연스럽게 처리됨
        /// </summary>
        protected override void HandleBallCollision(Collision2D collision)
        {
            // 강철 벽돌: wave(HP) 감소 없음, 파괴 없음
            // 공의 물리 반사는 PhysicsObject/Rigidbody2D에서 자연스럽게 처리됨
        }

        /// <summary>
        /// 회색 메탈릭 색상 적용 (wave 기반 색상 무시)
        /// </summary>
        protected override void ColorBrick()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.color = SteelBrickColor;
        }
    }
}
