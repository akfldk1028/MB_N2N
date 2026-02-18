using System;
using UnityEngine;

/// <summary>
/// 패들(Plank) 관리 매니저
/// - 패들 이동 제어
/// - 전역 InputManager로부터 입력 받기
/// </summary>
public class PlankManager
{
    #region References
    private PhysicsPlank _plank;
    private Camera _mainCamera;
    private IDisposable _arrowKeySubscription;
    #endregion

    #region Input State
    private float _currentHorizontalInput = 0f;
    #endregion

    #region Settings
    private float _keyboardMoveSpeed = 15f;
    private bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (_plank != null) _plank.enabled = _enabled;
        }
    }

    public float KeyboardMoveSpeed
    {
        get => _keyboardMoveSpeed;
        set => _keyboardMoveSpeed = Mathf.Max(1f, value);
    }
    #endregion

    #region Events
    public event Action<Vector3> OnPlankMoved;
    #endregion

    #region Initialization
    public PlankManager()
    {
        GameLogger.SystemStart("PlankManager", "패들 매니저 생성됨");
    }

    public void Initialize(PhysicsPlank plank, Camera mainCamera)
    {
        _plank = plank;
        _mainCamera = mainCamera;

        if (_plank == null)
        {
            GameLogger.Error("PlankManager", "PhysicsPlank가 null!");
            return;
        }

        if (_mainCamera == null)
        {
            GameLogger.Error("PlankManager", "Camera가 null!");
            return;
        }

        // 전역 InputManager의 Input_ArrowKey 구독
        _arrowKeySubscription = Managers.ActionBus.Subscribe(
            MB.Infrastructure.Messages.ActionId.Input_ArrowKey,
            OnArrowKeyInput
        );
        GameLogger.Success("PlankManager", "Input_ArrowKey 이벤트 구독 완료!");
    }

    ~PlankManager()
    {
        _arrowKeySubscription?.Dispose();
    }
    #endregion

    #region Input Handler
    private void OnArrowKeyInput(MB.Infrastructure.Messages.ActionMessage message)
    {
        if (message.TryGetPayload<MB.Infrastructure.Messages.ArrowKeyPayload>(out var payload))
        {
            _currentHorizontalInput = payload.Horizontal;
            GameLogger.DevLog("PlankManager", $"방향키 입력: {_currentHorizontalInput}");
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// 매 프레임 패들 이동 처리
    /// </summary>
    public void UpdateMovement(float deltaTime)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Time.frameCount % 120 == 0)
        {
            GameLogger.DevLog("PlankManager", $"UpdateMovement (enabled: {_enabled}, horizontal: {_currentHorizontalInput})");
        }
        #endif

        if (!_enabled || _plank == null) return;

        // 멀티플레이어: Owner만 입력 처리
        if (_plank.IsNetworkMode() && !_plank.IsOwner) return;

        // 터치/마우스 입력 우선 처리 (모바일, WebGL 지원)
        bool touchInputHandled = ProcessTouchOrMouseInput();

        // 방향키 입력 처리 (터치/마우스 입력이 없을 때만)
        if (!touchInputHandled && Mathf.Abs(_currentHorizontalInput) > 0.01f)
        {
            ProcessKeyboardMovement(deltaTime);
        }
    }

    private void ProcessKeyboardMovement(float deltaTime)
    {
        if (Mathf.Abs(_currentHorizontalInput) < 0.01f) return;

        Vector3 beforePosition = _plank.transform.position;
        _plank.MoveByKeyboard(_currentHorizontalInput, deltaTime);
        Vector3 afterPosition = _plank.transform.position;

        GameLogger.Info("PlankManager", $"🎮 패들 이동: {beforePosition.x:F2} → {afterPosition.x:F2}");
        OnPlankMoved?.Invoke(afterPosition);
    }

    /// <summary>
    /// 터치/마우스 입력 처리 (모바일, WebGL 지원)
    /// </summary>
    /// <returns>터치/마우스 입력이 처리되었으면 true</returns>
    private bool ProcessTouchOrMouseInput()
    {
        if (_mainCamera == null) return false;

        Vector3 pointerPosition = Vector3.zero;
        bool hasInput = false;

        // 터치 입력 우선 (모바일)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerPosition = new Vector3(touch.position.x, touch.position.y, 0);
            hasInput = true;
        }
        // 마우스 입력 (WebGL, 데스크톱)
        else if (Input.GetMouseButton(0))
        {
            pointerPosition = Input.mousePosition;
            hasInput = true;
        }

        if (hasInput)
        {
            Vector3 beforePosition = _plank.transform.position;
            _plank.MoveByPointer(pointerPosition, _mainCamera);
            Vector3 afterPosition = _plank.transform.position;

            // 위치가 실제로 변경되었을 때만 이벤트 발생
            if (Vector3.Distance(beforePosition, afterPosition) > 0.001f)
            {
                GameLogger.DevLog("PlankManager", $"👆 터치/마우스 이동: {beforePosition.x:F2} → {afterPosition.x:F2}");
                OnPlankMoved?.Invoke(afterPosition);
            }

            return true;
        }

        return false;
    }
    #endregion

    #region Control Methods
    public void ResetPosition()
    {
        if (_plank == null) return;

        if (_plank.leftEnd != null && _plank.rightEnd != null)
        {
            float centerX = (_plank.leftEnd.position.x + _plank.rightEnd.position.x) / 2f;
            Vector3 centerPosition = new Vector3(centerX, _plank.transform.position.y, _plank.transform.position.z);

            Rigidbody2D rb = _plank.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.position = new Vector2(centerPosition.x, centerPosition.y);
            else
                _plank.transform.position = centerPosition;

            GameLogger.Info("PlankManager", $"패들 위치 리셋: {centerPosition}");
        }
    }

    public void SetMouseSpeed(float speed)
    {
        if (_plank != null) _plank.smoothSpeed = speed;
    }

    public Vector3 GetPosition()
    {
        return _plank != null ? _plank.transform.position : Vector3.zero;
    }
    #endregion
}
