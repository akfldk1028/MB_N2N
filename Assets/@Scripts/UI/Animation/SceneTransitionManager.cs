using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// 씬 전환 시 페이드 효과를 관리하는 싱글톤 매니저
/// 전체 화면 Canvas + Image + CanvasGroup 오버레이를 프로그래밍 방식으로 생성하여
/// 페이드 인/아웃 전환 효과를 제공합니다.
///
/// 생성: Managers.cs / UIAnimationHelper.cs의 싱글톤 패턴 (DontDestroyOnLoad, 지연 생성)
/// 애니메이션: AchievementUnlocked.cs의 CanvasGroup + Mathf.Lerp 코루틴 방식
/// 씬 로딩: SceneManagerEx.cs의 싱글/멀티플레이어 분기 참고
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private static SceneTransitionManager s_instance;
    public static SceneTransitionManager Instance
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

    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    private bool _isTransitioning = false;

    /// <summary>
    /// 현재 전환 중인지 여부
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>
    /// 싱글톤 초기화 - Managers / UIAnimationHelper 패턴과 동일한 지연 생성 방식
    /// </summary>
    public static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@SceneTransitionManager");
            if (go == null)
            {
                go = new GameObject("@SceneTransitionManager");
                go.AddComponent<SceneTransitionManager>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<SceneTransitionManager>();
        }
    }

    private void Awake()
    {
        // 중복 체크
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        CreateOverlayUI();

        GameLogger.Success("SceneTransitionManager", "초기화 완료");
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    #region UI Setup
    /// <summary>
    /// 전체 화면 오버레이 UI를 프로그래밍 방식으로 생성합니다.
    /// Canvas (ScreenSpaceOverlay, sortingOrder=9999) > Image (검정, 전체 화면) > CanvasGroup
    /// </summary>
    private void CreateOverlayUI()
    {
        // Canvas 생성
        GameObject canvasGo = new GameObject("TransitionCanvas");
        canvasGo.transform.SetParent(transform);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        // CanvasScaler 추가 (해상도 대응)
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // GraphicRaycaster 추가 (전환 중 입력 차단)
        canvasGo.AddComponent<GraphicRaycaster>();

        // 검정 이미지 생성 (전체 화면)
        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        Image fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        // RectTransform 전체 화면 설정
        RectTransform rectTransform = imageGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // CanvasGroup 추가 (알파 제어용)
        _canvasGroup = imageGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }
    #endregion

    #region Fade Methods
    /// <summary>
    /// 페이드 아웃 (화면이 검정으로 덮임) - alpha 0 -> 1
    /// AchievementUnlocked.cs의 FadeCanvasGroup 패턴을 따릅니다.
    /// </summary>
    /// <param name="onComplete">완료 시 콜백</param>
    public IEnumerator FadeOut(Action onComplete = null)
    {
        if (_canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        _canvasGroup.blocksRaycasts = true;

        float elapsedTime = 0f;
        _canvasGroup.alpha = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 페이드 인 (검정 화면이 사라짐) - alpha 1 -> 0
    /// AchievementUnlocked.cs의 FadeCanvasGroup 패턴을 따릅니다.
    /// </summary>
    /// <param name="onComplete">완료 시 콜백</param>
    public IEnumerator FadeIn(Action onComplete = null)
    {
        if (_canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        _canvasGroup.alpha = 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        onComplete?.Invoke();
    }
    #endregion

    #region Scene Transition
    /// <summary>
    /// 페이드 전환과 함께 씬을 로드합니다.
    /// 페이드 아웃 -> loadAction 실행 -> 씬 로드 완료 대기 -> 페이드 인
    /// SceneManagerEx.cs의 로딩 흐름을 참고합니다.
    /// </summary>
    /// <param name="loadAction">씬 로딩 액션 (SceneManager.LoadScene 등)</param>
    public void LoadSceneWithTransition(Action loadAction)
    {
        if (loadAction == null)
        {
            GameLogger.Warning("SceneTransitionManager", "loadAction이 null입니다");
            return;
        }

        if (_isTransitioning)
        {
            GameLogger.Warning("SceneTransitionManager", "이미 전환 중입니다");
            return;
        }

        StartCoroutine(TransitionCoroutine(loadAction));
    }

    /// <summary>
    /// Define.EScene을 사용한 페이드 전환 씬 로드 (편의 메서드)
    /// SceneManagerEx.cs의 LoadScene 패턴을 따릅니다.
    /// </summary>
    /// <param name="sceneType">로드할 씬 타입</param>
    public void LoadSceneWithTransition(Define.EScene sceneType)
    {
        string sceneName = System.Enum.GetName(typeof(Define.EScene), sceneType);

        LoadSceneWithTransition(() =>
        {
            GameLogger.Progress("SceneTransitionManager", $"씬 로드: {sceneName}");
            SceneManager.LoadScene(sceneName);
        });
    }

    /// <summary>
    /// 전환 코루틴: 페이드 아웃 -> 씬 로드 -> 씬 로드 완료 대기 -> 페이드 인
    /// </summary>
    private IEnumerator TransitionCoroutine(Action loadAction)
    {
        _isTransitioning = true;

        GameLogger.Progress("SceneTransitionManager", "페이드 아웃 시작");

        // 1. 페이드 아웃 (화면 어두워짐)
        yield return StartCoroutine(FadeOut());

        // 2. 씬 로드 완료 대기를 위한 콜백 등록
        bool sceneLoaded = false;
        SceneManager.sceneLoaded += OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sceneLoaded = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameLogger.Progress("SceneTransitionManager", $"씬 로드 완료: {scene.name}");
        }

        // 3. 씬 로딩 실행
        loadAction.Invoke();

        // 4. 씬 로드 완료 대기 (최대 10초 타임아웃)
        float timeout = 10f;
        float waitTime = 0f;

        while (!sceneLoaded && waitTime < timeout)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!sceneLoaded)
        {
            // 타임아웃 발생 시에도 콜백 해제 및 계속 진행
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameLogger.Warning("SceneTransitionManager", "씬 로드 대기 타임아웃 (10초)");
        }

        // 5. 한 프레임 대기 (씬 초기화 시간 확보)
        yield return null;

        GameLogger.Progress("SceneTransitionManager", "페이드 인 시작");

        // 6. 페이드 인 (화면 밝아짐)
        yield return StartCoroutine(FadeIn());

        _isTransitioning = false;

        GameLogger.Success("SceneTransitionManager", "씬 전환 완료");
    }
    #endregion
}
