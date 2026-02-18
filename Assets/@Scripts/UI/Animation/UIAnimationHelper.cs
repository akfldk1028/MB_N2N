using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// UI 애니메이션 유틸리티 싱글톤
/// MonoBehaviour가 아닌 클래스(GameResultUIController 등)에서 코루틴을 실행할 수 있도록
/// 코루틴 호스팅을 제공하고, 재사용 가능한 애니메이션 메서드를 제공합니다.
///
/// 패턴: AchievementUnlocked.cs의 FadeCanvasGroup 방식 (CanvasGroup + Mathf.Lerp)
/// 생성: Managers.cs의 싱글톤 패턴 (DontDestroyOnLoad, 지연 생성)
/// </summary>
public class UIAnimationHelper : MonoBehaviour
{
    private static UIAnimationHelper s_instance;
    public static UIAnimationHelper Instance
    {
        get
        {
            if (s_instance == null)
            {
                Init();
            }
            return s_instance;
        }
    }

    /// <summary>
    /// 싱글톤 초기화 - Managers 패턴과 동일한 지연 생성 방식
    /// </summary>
    public static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@UIAnimationHelper");
            if (go == null)
            {
                go = new GameObject("@UIAnimationHelper");
                go.AddComponent<UIAnimationHelper>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<UIAnimationHelper>();
            GameLogger.Success("UIAnimationHelper", "초기화 완료");
        }
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    #region Coroutine Hosting
    /// <summary>
    /// 외부 클래스에서 코루틴을 실행할 수 있도록 호스팅합니다.
    /// MonoBehaviour가 아닌 클래스(GameResultUIController 등)에서 사용합니다.
    /// </summary>
    /// <param name="coroutine">실행할 코루틴</param>
    /// <returns>실행 중인 Coroutine 참조</returns>
    public Coroutine StartAnimCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            GameLogger.Warning("UIAnimationHelper", "null 코루틴 실행 시도");
            return null;
        }

        return StartCoroutine(coroutine);
    }

    /// <summary>
    /// 실행 중인 코루틴을 중지합니다.
    /// </summary>
    /// <param name="coroutine">중지할 Coroutine 참조</param>
    public void StopAnimCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }
    #endregion

    #region Fade Animation
    /// <summary>
    /// CanvasGroup의 알파를 부드럽게 전환합니다.
    /// AchievementUnlocked.cs의 FadeCanvasGroup 패턴을 따릅니다.
    /// </summary>
    /// <param name="cg">대상 CanvasGroup</param>
    /// <param name="startAlpha">시작 알파값</param>
    /// <param name="endAlpha">종료 알파값</param>
    /// <param name="duration">전환 시간(초)</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration, Action onComplete = null)
    {
        if (cg == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        cg.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        cg.alpha = endAlpha;
        onComplete?.Invoke();
    }
    #endregion

    #region Scale Animation
    /// <summary>
    /// Transform의 스케일을 부드럽게 전환합니다.
    /// Mathf.Lerp를 사용한 선형 보간 방식입니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="startScale">시작 스케일</param>
    /// <param name="endScale">종료 스케일</param>
    /// <param name="duration">전환 시간(초)</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public static IEnumerator ScaleTo(Transform target, Vector3 startScale, Vector3 endScale, float duration, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        target.localScale = startScale;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 스케일 펀치 효과 (확대 후 원래 크기로 복귀)
    /// 게임 결과 타이틀 등에 사용합니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="punchScale">최대 스케일 (기본 1.5)</param>
    /// <param name="duration">전환 시간(초)</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public static IEnumerator ScalePunch(Transform target, float punchScale, float duration, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startScale = Vector3.one * punchScale;
        Vector3 endScale = Vector3.one;

        yield return ScaleTo(target, startScale, endScale, duration, onComplete);
    }
    #endregion

    #region Count Animation
    /// <summary>
    /// 텍스트에 숫자 카운팅 애니메이션을 적용합니다.
    /// 0에서 목표 값까지 부드럽게 카운팅합니다.
    /// </summary>
    /// <param name="textComponent">대상 TMP_Text</param>
    /// <param name="startValue">시작 값</param>
    /// <param name="endValue">종료 값</param>
    /// <param name="duration">카운팅 시간(초)</param>
    /// <param name="format">텍스트 포맷 (기본 "{0}")</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public static IEnumerator CountTo(TMP_Text textComponent, int startValue, int endValue, float duration, string format = "{0}", Action onComplete = null)
    {
        if (textComponent == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
            textComponent.text = string.Format(format, currentValue);
            yield return null;
        }

        textComponent.text = string.Format(format, endValue);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 텍스트에 숫자 카운팅 애니메이션을 적용합니다. (float 버전)
    /// </summary>
    /// <param name="textComponent">대상 TMP_Text</param>
    /// <param name="startValue">시작 값</param>
    /// <param name="endValue">종료 값</param>
    /// <param name="duration">카운팅 시간(초)</param>
    /// <param name="format">텍스트 포맷 (기본 "{0:F1}")</param>
    /// <param name="onComplete">완료 시 콜백</param>
    public static IEnumerator CountTo(TMP_Text textComponent, float startValue, float endValue, float duration, string format = "{0:F1}", Action onComplete = null)
    {
        if (textComponent == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float currentValue = Mathf.Lerp(startValue, endValue, t);
            textComponent.text = string.Format(format, currentValue);
            yield return null;
        }

        textComponent.text = string.Format(format, endValue);
        onComplete?.Invoke();
    }
    #endregion

    #region Utility
    /// <summary>
    /// CanvasGroup 컴포넌트를 가져오거나 추가합니다.
    /// Util.GetOrAddComponent 패턴을 따릅니다.
    /// </summary>
    /// <param name="go">대상 GameObject</param>
    /// <returns>CanvasGroup 컴포넌트</returns>
    public static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        return Util.GetOrAddComponent<CanvasGroup>(go);
    }
    #endregion
}
