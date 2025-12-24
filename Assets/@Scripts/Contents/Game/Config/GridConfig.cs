using UnityEngine;

/// <summary>
/// 그리드 설정 ScriptableObject
/// Unity Editor에서 Assets > Create > Game > Grid Config로 생성
/// </summary>
[CreateAssetMenu(fileName = "GridConfig", menuName = "Game/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Size")]
    [Tooltip("X축 그리드 크기")]
    public int gridSizeX = 20;

    [Tooltip("Y축 그리드 크기")]
    public int gridSizeY = 20;

    [Header("Cube Settings")]
    [Tooltip("큐브 크기")]
    public float cubeSize = 1.0f;

    [Tooltip("큐브 간격")]
    public float spacing = 0.05f;

    [Tooltip("그리드 높이 (타일 두께)")]
    public float gridHeight = 0.2f;

    [Header("Aspect Ratio")]
    [Tooltip("그리드 종횡비 (정사각형 보정)")]
    public float aspectRatio = 1.5f;

    [Header("Turret")]
    [Tooltip("터렛 Y 위치 오프셋 (0 = 바닥)")]
    public float turretHeightOffset = 0f;

    [Header("Wall")]
    [Tooltip("벽 높이")]
    public float wallHeight = 1.0f;
}
