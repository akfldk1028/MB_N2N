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
    private Vector3 _originalCameraPosition;  // 원래 카메라 위치 (멀티플레이어 기준점)
    #endregion

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
            // ✅ 원래 카메라 위치 저장 (멀티플레이어 시 기준점)
            _originalCameraPosition = _mainCamera.transform.position;
            GameLogger.Success("CameraManager", $"Main_Camera, Sub_Camera 발견 (원래 위치: {_originalCameraPosition})");
        }
    }

    /// <summary>
    /// Client-side Viewport 설정 (Host vs Client)
    /// 카메라 위치도 각 플레이어의 블록 영역으로 이동
    ///
    /// 규칙:
    /// - 왼쪽 화면 = 내 게임 영역 (직접 플레이)
    /// - 오른쪽 화면 = 상대방 게임 영역 (관전)
    /// </summary>
    /// <param name="localClientId">내 Client ID</param>
    public void SetupViewportsForLocalPlayer(ulong localClientId)
    {
        // ✅ 카메라가 없으면 자동으로 Initialize 시도 (Client 타이밍 이슈 해결)
        if (_mainCamera == null || _subCamera == null)
        {
            GameLogger.Warning("CameraManager", "카메라가 없음 - Initialize 재시도...");
            Initialize();
        }

        if (_mainCamera == null || _subCamera == null)
        {
            GameLogger.Error("CameraManager", "카메라 초기화 실패! Main/Sub 카메라를 찾을 수 없습니다.");
            return;
        }

        // ✅ 플레이어별 xOffset (_plankSpacing = 15: 카메라 영역 침범 방지)
        float player0Offset = -15f;  // Player 0 영역 (Host)
        float player1Offset = +15f;  // Player 1 영역 (Client)

        // localClientId가 0이면 Host, 1이면 Client
        bool isHost = (localClientId == 0);

        // ✅ 내 영역과 상대 영역 결정
        float myOffset = isHost ? player0Offset : player1Offset;
        float opponentOffset = isHost ? player1Offset : player0Offset;

        // ✅ 왼쪽 카메라 = 내 영역, 오른쪽 카메라 = 상대 영역
        // Main_Camera를 왼쪽(내 영역)으로, Sub_Camera를 오른쪽(상대 영역)으로 사용
        Vector3 mainPos = _originalCameraPosition;
        mainPos.x = _originalCameraPosition.x + myOffset;  // Main → 내 영역
        _mainCamera.transform.position = mainPos;

        Vector3 subPos = _originalCameraPosition;
        subPos.x = _originalCameraPosition.x + opponentOffset;  // Sub → 상대 영역
        _subCamera.transform.position = subPos;
        _subCamera.transform.rotation = _mainCamera.transform.rotation;

        // ✅ Aspect Ratio 유지 Viewport 설정
        ApplyAspectRatioViewports();

        GameLogger.Success("CameraManager", $"[Client {localClientId}] Main(왼쪽, 내 영역 x={myOffset}) + Sub(오른쪽, 상대 영역 x={opponentOffset})");
        GameLogger.Info("CameraManager", $"[DEBUG] Screen: {Screen.width}x{Screen.height}, Main rect: {_mainCamera.rect}, Sub rect: {_subCamera.rect}");
    }

    /// <summary>
    /// Aspect Ratio를 유지하는 Viewport 설정
    /// 게임 영역의 비율을 유지하면서 letterbox/pillarbox 적용
    /// </summary>
    private void ApplyAspectRatioViewports()
    {
        // 타겟 게임 비율 (세로형 블록깨기: 9:16 또는 유사)
        float targetAspect = 9f / 16f;  // 0.5625

        // 각 뷰포트 영역의 너비 비율 (30%)
        float viewportWidth = 0.3f;

        // 현재 화면 비율
        float screenAspect = (float)Screen.width / Screen.height;

        // 뷰포트 내부의 실제 비율 계산
        // 뷰포트가 화면의 30%를 차지하므로, 뷰포트 내부 비율 = screenAspect * viewportWidth
        float viewportAspect = screenAspect * viewportWidth;

        float viewportHeight = 1f;
        float yOffset = 0f;

        // ✅ Letterbox 계산 (비율 유지)
        if (viewportAspect > targetAspect)
        {
            // 뷰포트가 타겟보다 넓음 → 높이를 줄여서 letterbox
            viewportHeight = targetAspect / viewportAspect;
            yOffset = (1f - viewportHeight) / 2f;
        }
        // 뷰포트가 타겟보다 좁으면 → 그대로 사용 (pillarbox는 이미 뷰포트 너비로 처리됨)

        // ✅ Viewport 적용 (왼쪽 30%, 오른쪽 30%, 중앙 40% 빈 공간)
        _mainCamera.rect = new Rect(0, yOffset, viewportWidth, viewportHeight);
        _subCamera.rect = new Rect(1f - viewportWidth, yOffset, viewportWidth, viewportHeight);

        GameLogger.Info("CameraManager",
            $"Viewport 설정: viewportAspect={viewportAspect:F2}, targetAspect={targetAspect:F2}, height={viewportHeight:F2}, yOffset={yOffset:F2}");
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

    /// <summary>
    /// Main Camera 반환
    /// </summary>
    public Camera GetMainCamera() => _mainCamera;

    /// <summary>
    /// Sub Camera 반환
    /// </summary>
    public Camera GetSubCamera() => _subCamera;
}
