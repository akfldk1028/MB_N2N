using UnityEngine;

namespace MB.Visual
{
    [CreateAssetMenu(fileName = "Theme_New", menuName = "BrickGame/Color Theme")]
    public class ColorThemeSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string themeName = "New Theme";

        [Header("벽돌 색상 (HP 단계별)")]
        public Color[] brickColors = new Color[]
        {
            new Color(1f, 0.8f, 0.8f),
            new Color(1f, 0.6f, 0.6f),
            new Color(0.8f, 0.6f, 1f),
            new Color(0.6f, 0.8f, 1f),
            new Color(0.6f, 1f, 0.8f),
        };

        [Header("팀 색상")]
        public Color team0Color = new Color(0.5f, 0.9f, 0.5f);
        public Color team1Color = new Color(0.9f, 0.9f, 0.3f);

        [Header("배경")]
        public Color bgTop = new Color(0.7f, 0.85f, 1f);
        public Color bgBottom = new Color(0.95f, 0.95f, 1f);

        [Header("UI")]
        public Color uiPrimary = new Color(0.3f, 0.6f, 0.9f);
        public Color uiSecondary = new Color(0.9f, 0.5f, 0.3f);

        [Header("파티클")]
        public Color particleMain = Color.white;
        public Color particleSub = new Color(1f, 0.9f, 0.5f);

        [Header("Territory 타일 (Phase 2)")]
        public Sprite[] territoryTiles;

        [Header("배경 일러스트 (Phase 2)")]
        public Sprite backgroundSprite;

        public Color GetBrickColor(int hp)
        {
            if (brickColors == null || brickColors.Length == 0) return Color.white;
            int idx = Mathf.Clamp(hp - 1, 0, brickColors.Length - 1);
            return brickColors[idx];
        }
    }
}
