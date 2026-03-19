/*
 * 카메라 매니저 (CameraManager)
 *
 * 역할:
 * 1. 씬에 존재하는 Main_Camera, Sub_Camera Viewport 관리
 * 2. LocalClientId에 따라 왼쪽/오른쪽 Viewport 조정
 * 3. Host: Main(왼쪽) + Sub(오른쪽)
 * 4. Client: Sub(왼쪽) + Main(오른쪽)
 */

using UnityEngine;

public class CameraManager
{
    #region 카메라 참조
    private Camera _mainCamera;   // 씬에 존재 (Main_Camera)
    private Camera _subCamera;    // 씬에 존재 (Sub_Camera)
    #endregion

    // ✅ 게임 영역 설정 (원점 기준)
    private const float GAME_AREA_WIDTH = 7.2f;     // 경계 좌(-3.6) ~ 우(+3.6)
    private const float GAME_AREA_TOP = 1.5f;       // TopEnd y + 여유
    private const float GAME_AREA_BOTTOM = -5.0f;   // Plank y=-4, 공 여유 -1.0
    private const float GAME_CENTER_Y = (GAME_AREA_TOP + GAME_AREA_BOTTOM) / 2f; // ≈ -1.75
    private const float GAME_AREA_HEIGHT = GAME_AREA_TOP - GAME_AREA_BOTTOM;     // ≈ 6.5
    private const float VIEWPORT_WIDTH = 0.3f;      // 양쪽 30%

    public CameraManager()
    {
        GameLogger.Success("CameraManager", "생성됨");
    }

    /// <summary>
    /// 초기화 (씬에서 카메라 찾기)
    /// </summary>
    public void Initialize()
    {
        // Main_Camera 찾기
        _mainCamera = GameObject.Find("Main_Camera")?.GetComponent<Camera>();
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            GameLogger.Warning("CameraManager", "Main_Camera를 이름으로 찾지 못함. Camera.main 사용");
        }

        // Sub_Camera 찾기
        _subCamera = GameObject.Find("Sub_Camera")?.GetComponent<Camera>();
        if (_subCamera == null)
        {
            GameLogger.Error("CameraManager", "Sub_Camera를 찾을 수 없습니다!");
        }

        if (_mainCamera != null && _subCamera != null)
        {
            GameLogger.Success("CameraManager", "Main_Camera, Sub_Camera 발견");
        }
    }

    /// <summary>
    /// Client-side Viewport 설정 (Host vs Client)
    /// Orthographic 카메라로 게임 영역만 정확히 렌더링
    /// </summary>
    public void SetupViewportsForLocalPlayer(ulong localClientId)
    {
        if (_mainCamera == null || _subCamera == null)
        {
            GameLogger.Warning("CameraManager", "카메라가 없음 - Initialize 재시도...");
            Initialize();
        }

        if (_mainCamera == null || _subCamera == null)
        {
            GameLogger.Error("CameraManager", "카메라 초기화 실패!");
            return;
        }

        // ✅ 플레이어별 xOffset
        float player0Offset = -15f;
        float player1Offset = +15f;
        bool isHost = (localClientId == 0);
        float myOffset = isHost ? player0Offset : player1Offset;
        float opponentOffset = isHost ? player1Offset : player0Offset;

        // ✅ 양쪽 카메라를 Orthographic으로 전환 (게임 영역만 정확히 보여줌)
        SetupOrthographicCamera(_mainCamera, myOffset);
        SetupOrthographicCamera(_subCamera, opponentOffset);

        // ✅ Viewport 설정 (양쪽 30%, 전체 높이)
        _mainCamera.rect = new Rect(0, 0, VIEWPORT_WIDTH, 1f);
        _subCamera.rect = new Rect(1f - VIEWPORT_WIDTH, 0, VIEWPORT_WIDTH, 1f);

        GameLogger.Success("CameraManager",
            $"[Client {localClientId}] Ortho 카메라 설정 완료: Main(x={myOffset}), Sub(x={opponentOffset})");
        GameLogger.Info("CameraManager",
            $"[DEBUG] Screen: {Screen.width}x{Screen.height}, OrthoSize={_mainCamera.orthographicSize:F1}");
    }

    /// <summary>
    /// Orthographic 카메라 설정
    /// 게임 영역의 너비/높이에 맞춰 orthoSize 계산
    /// </summary>
    private void SetupOrthographicCamera(Camera cam, float xOffset)
    {
        // Orthographic 전환
        cam.orthographic = true;

        // 뷰포트의 실제 종횡비 계산
        float viewportPixelWidth = VIEWPORT_WIDTH * Screen.width;
        float viewportPixelHeight = Screen.height;
        float viewportAspect = viewportPixelWidth / viewportPixelHeight;

        // ✅ orthoSize 계산: 카메라 x범위가 Territory 맵(x=-10.5~+10.5)과 절대 겹치지 않도록
        // 카메라가 x=±15에 있고, Territory 끝이 x=±10.5
        // 카메라 x범위 = orthoSize * viewportAspect
        // 최대 허용 x반폭 = |xOffset| - 10.5 = 15 - 10.5 = 4.5 (약간 여유 주기)
        float maxHalfWidth = Mathf.Abs(xOffset) - 11.0f; // Territory까지 0.5유닛 여유
        float maxOrthoSizeByWidth = maxHalfWidth / viewportAspect;

        // 너비 기준: 게임 영역을 채우려면
        float sizeByWidth = (GAME_AREA_WIDTH / 2f) / viewportAspect;

        // Territory 겹침 방지를 위해 최대값 제한
        float orthoSize = Mathf.Min(sizeByWidth, maxOrthoSizeByWidth);

        // 최소 크기 보장 (너무 작으면 게임이 안 보임)
        orthoSize = Mathf.Max(orthoSize, 2.5f);

        cam.orthographicSize = orthoSize;

        // 카메라 위치: 게임 영역 중심, z는 뒤로 빼기
        cam.transform.position = new Vector3(xOffset, GAME_CENTER_Y, -10f);
        cam.transform.rotation = Quaternion.identity;

        float actualHalfWidth = orthoSize * viewportAspect;
        GameLogger.Info("CameraManager",
            $"[Ortho] aspect={viewportAspect:F2}, orthoSize={orthoSize:F1}, xRange=[{xOffset - actualHalfWidth:F1}, {xOffset + actualHalfWidth:F1}]");
    }

    /// <summary>
    /// Viewport 초기화 (전체 화면으로 복구)
    /// </summary>
    public void ResetViewports()
    {
        if (_mainCamera != null)
        {
            _mainCamera.rect = new Rect(0, 0, 1, 1);
        }

        if (_subCamera != null)
        {
            _subCamera.rect = new Rect(0, 0, 1, 1);
        }

        GameLogger.Info("CameraManager", "Viewport 초기화 완료");
    }

    public Camera GetMainCamera() => _mainCamera;
    public Camera GetSubCamera() => _subCamera;
}
