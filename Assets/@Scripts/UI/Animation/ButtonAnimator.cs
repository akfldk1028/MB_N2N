using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼 터치 피드백 애니메이션 컴포넌트
/// 터치 시 Scale 축소 (0.95) → Release 시 복귀 (1.0) 효과를 제공합니다.
/// 아무 Button GameObject에 추가하여 사용할 수 있습니다.
///
/// 패턴: UI_EventHandler.cs의 IPointerDownHandler/IPointerUpHandler 인터페이스 구현
/// 애니메이션: AchievementUnlocked.cs의 Mathf.Lerp 코루틴 방식
/// </summary>
public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float pressDownDuration = 0.05f;
    [SerializeField] private float pressUpDuration = 0.1f;

    private Vector3 _originalScale;
    private Coroutine _currentAnimation;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(pressScale, pressDownDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(1.0f, pressUpDuration);
    }

    private void OnDisable()
    {
        // 비활성화 시 스케일 즉시 복구
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }
        transform.localScale = _originalScale;
    }

    /// <summary>
    /// 대상 스케일로 부드럽게 전환합니다.
    /// 이전 애니메이션이 진행 중이면 중단하고 새 애니메이션을 시작합니다.
    /// </summary>
    /// <param name="targetScaleMultiplier">목표 스케일 배율</param>
    /// <param name="duration">전환 시간(초)</param>
    private void AnimateTo(float targetScaleMultiplier, float duration)
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        Vector3 targetScale = _originalScale * targetScaleMultiplier;
        _currentAnimation = StartCoroutine(ScaleCoroutine(targetScale, duration));
    }

    /// <summary>
    /// Mathf.Lerp를 사용한 부드러운 스케일 전환 코루틴
    /// AchievementUnlocked.cs의 FadeCanvasGroup 패턴을 따릅니다.
    /// </summary>
    private IEnumerator ScaleCoroutine(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        _currentAnimation = null;
    }
}
