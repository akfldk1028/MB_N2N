using UnityEngine;

/// <summary>
/// 플레이어 색상 설정 ScriptableObject
/// Unity Editor에서 Assets > Create > Game > Player Colors로 생성
/// </summary>
[CreateAssetMenu(fileName = "PlayerColors", menuName = "Game/Player Colors")]
public class PlayerColorConfig : ScriptableObject
{
    [System.Serializable]
    public class PlayerColor
    {
        public int playerID;
        public Color color = Color.white;
        public string displayName;
    }

    [Header("플레이어 색상")]
    [Tooltip("각 플레이어의 색상 정의")]
    public PlayerColor[] playerColors;

    [Header("중립 색상")]
    [Tooltip("소유자 없는 블록의 색상")]
    public Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    /// <summary>
    /// 플레이어 ID로 색상 가져오기
    /// </summary>
    public Color GetColor(int playerID)
    {
        if (playerColors == null) return Color.white;

        foreach (var pc in playerColors)
        {
            if (pc.playerID == playerID)
                return pc.color;
        }

        return Color.white; // 찾지 못하면 흰색
    }

    /// <summary>
    /// 플레이어 ID가 정의되어 있는지 확인
    /// </summary>
    public bool HasPlayer(int playerID)
    {
        if (playerColors == null) return false;

        foreach (var pc in playerColors)
        {
            if (pc.playerID == playerID)
                return true;
        }

        return false;
    }
}
