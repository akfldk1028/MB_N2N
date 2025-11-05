using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 업적 달성 시 표시되는 UI를 관리하는 클래스
/// </summary>
public class AchievementUnlocked : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TextMeshPro achievementNameText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private bool isShowing = false;

    private void Awake()
    {
        // 자동으로 참조 찾기
        if (achievementPanel == null)
        {
            achievementPanel = transform.Find("achievementPanel")?.gameObject;
        }

        if (achievementNameText == null)
        {
            // nameOfAchievement GameObject에서 TextMeshPro 컴포넌트 찾기
            var nameObject = transform.Find("achievementPanel/nameOfAchievement")?.gameObject;
            if (nameObject != null)
            {
                achievementNameText = nameObject.GetComponent<TextMeshPro>();
            }
        }

        // CanvasGroup 컴포넌트 가져오기 (페이드 효과용)
        if (achievementPanel != null)
        {
            canvasGroup = achievementPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = achievementPanel.AddComponent<CanvasGroup>();
            }
        }

        // 초기에는 패널 숨기기
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 컴포넌트가 활성화될 때 자동으로 실행되지 않도록 설정
        enabled = false;
    }

    /// <summary>
    /// 업적 이름을 설정하고 업적 UI를 표시합니다.
    /// </summary>
    /// <param name="achievementName">표시할 업적 이름</param>
    public void NameOfTheAchievement(string achievementName)
    {
        if (isShowing)
        {
            // 이미 표시 중이면 무시
            return;
        }

        StartCoroutine(ShowAchievementCoroutine(achievementName));
    }

    private IEnumerator ShowAchievementCoroutine(string achievementName)
    {
        isShowing = true;

        // 업적 이름 설정
        if (achievementNameText != null)
        {
            achievementNameText.text = achievementName.ToUpper();
        }

        // 패널 활성화
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(true);
        }

        // 페이드 인
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration));
        }

        // 표시 대기
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration));
        }

        // 패널 비활성화
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }

        isShowing = false;
        enabled = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        cg.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        cg.alpha = endAlpha;
    }

    /// <summary>
    /// 즉시 업적 UI를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        StopAllCoroutines();

        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        isShowing = false;
        enabled = false;
    }
}
