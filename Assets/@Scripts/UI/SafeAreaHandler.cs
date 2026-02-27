using UnityEngine;

/// <summary>
/// Safe Area 핸들러 (SafeAreaHandler)
///
/// 역할:
/// 1. 노치/펀치홀이 있는 디바이스에서 UI가 안전 영역 내에 표시되도록 보장
/// 2. Canvas 하위 RectTransform의 앵커를 Screen.safeArea에 맞게 자동 조정
/// 3. 화면 방향 변경 시 자동으로 재계산
///
/// 사용법:
/// - Canvas 하위에 SafeArea RectTransform을 생성하고 이 컴포넌트를 추가
/// - 모든 UI 요소를 SafeArea RectTransform의 자식으로 배치
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
	private RectTransform _rectTransform;
	private Canvas _canvas;
	private Rect _lastSafeArea = Rect.zero;
	private Vector2Int _lastScreenSize = Vector2Int.zero;
	private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		_canvas = GetComponentInParent<Canvas>();

		if (_canvas == null)
		{
			Debug.LogWarning($"[SafeAreaHandler] Canvas not found in parent hierarchy. ({gameObject.name})");
			enabled = false;
			return;
		}

		ApplySafeArea();
	}

	private void Update()
	{
		if (HasScreenChanged())
		{
			ApplySafeArea();
		}
	}

	/// <summary>
	/// 화면 크기나 방향이 변경되었는지 확인
	/// </summary>
	private bool HasScreenChanged()
	{
		Rect safeArea = Screen.safeArea;
		Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
		ScreenOrientation orientation = Screen.orientation;

		if (safeArea != _lastSafeArea || screenSize != _lastScreenSize || orientation != _lastOrientation)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Screen.safeArea를 읽어 RectTransform 앵커를 조정하여 Safe Area 적용
	/// </summary>
	private void ApplySafeArea()
	{
		Rect safeArea = Screen.safeArea;

		if (Screen.width <= 0 || Screen.height <= 0)
			return;

		// 픽셀 좌표를 정규화된 앵커 값으로 변환
		Vector2 anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
		Vector2 anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

		// 앵커 값 범위 제한 (0~1)
		anchorMin.x = Mathf.Clamp01(anchorMin.x);
		anchorMin.y = Mathf.Clamp01(anchorMin.y);
		anchorMax.x = Mathf.Clamp01(anchorMax.x);
		anchorMax.y = Mathf.Clamp01(anchorMax.y);

		_rectTransform.anchorMin = anchorMin;
		_rectTransform.anchorMax = anchorMax;

		// 현재 상태 캐시
		_lastSafeArea = safeArea;
		_lastScreenSize = new Vector2Int(Screen.width, Screen.height);
		_lastOrientation = Screen.orientation;
	}
}
