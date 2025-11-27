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
        if (_mainCamera == null || _subCamera == null)
        {
            GameLogger.Error("CameraManager", "카메라가 초기화되지 않았습니다!");
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

        // ✅ Viewport 설정 (항상 Main=왼쪽, Sub=오른쪽)
        _mainCamera.rect = new Rect(0, 0, 0.3f, 1);      // 왼쪽 30% (내 게임)
        _subCamera.rect = new Rect(0.7f, 0, 0.3f, 1);    // 오른쪽 30% (상대 게임)

        GameLogger.Success("CameraManager", $"[Client {localClientId}] Main(왼쪽, 내 영역 x={myOffset}) + Sub(오른쪽, 상대 영역 x={opponentOffset})");
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
