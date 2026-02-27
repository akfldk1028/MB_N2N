using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Define;

public class UI_StartUpScene : UI_Scene
{
	enum GameObjects
	{
		StartImage
	}

	enum Texts
	{
		DisplayText,
		MatchingStatusText,
		HighScoreText,
	}

	enum Buttons
	{
		StartButton,
		RecipeButton,
		ExitButton,
	}

    // 매칭 스피너 관련 필드
    private GameObject _spinnerObject;
    private Coroutine _spinnerCoroutine;
    private Coroutine _textBlinkCoroutine;
    private bool _isMatching = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindObjects(typeof(GameObjects));
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

		GetObject((int)GameObjects.StartImage).BindEvent((evt) =>
		{
			Debug.Log("ChangeScene");
			// Managers.Scene.LoadScene(EScene.GameScene);
		});

	GetObject((int)GameObjects.StartImage).gameObject.SetActive(false);
	GetButton((int)Buttons.RecipeButton).gameObject.BindEvent(OnClickRecipeButton);
	GetButton((int)Buttons.ExitButton).gameObject.BindEvent(OnClickExitButton);
	GetButton((int)Buttons.StartButton).gameObject.BindEvent(OnClickStartButton);

	// 버튼 터치 애니메이션 적용
	GetButton((int)Buttons.StartButton).gameObject.AddButtonAnimation();
	GetButton((int)Buttons.RecipeButton).gameObject.AddButtonAnimation();
	GetButton((int)Buttons.ExitButton).gameObject.AddButtonAnimation();

	// GetText((int)Texts.DisplayText).text = $"StartUpScene";
	Debug.Log($"<color=cyan>[UI_StartUpScene]</color> Asset Load 합니다.");
		StartLoadAssets();

		return true;
    }
    async void OnClickStartButton(PointerEventData evt)
    {
        GameLogger.SystemStart("UI_StartUpScene", "게임 시작 버튼 클릭!");

        // 매칭 상태 UI 표시 시작
        _isMatching = true;
        CreateSpinner();
        _spinnerCoroutine = StartCoroutine(RotateSpinner());
        _textBlinkCoroutine = StartCoroutine(BlinkMatchingText());
        StartCoroutine(UpdateMatchingStatus());

        // 멀티플레이어 세션 시작 및 매칭 대기
        // ConnectionManagerEx가 2명 달성 시 자동으로 GameScene으로 전환
        await StartMultiplayerSession();

        // ❌ 제거: 즉시 씬 전환하지 않음
        // ✅ StartUpScene에서 대기 → 2명 달성 시 ConnectionManagerEx가 자동으로 NetworkManager.SceneManager.LoadScene() 호출
        // Managers.Scene.LoadScene(EScene.GameScene);

        GameLogger.Info("UI_StartUpScene", "매칭 대기 중... (2명 필요)");
    }

    /// <summary>
    /// 매칭 상태를 실시간으로 UI에 표시
    /// </summary>
    private IEnumerator UpdateMatchingStatus()
    {
        while (true)
        {
            // NetworkManager가 없거나 시작되지 않았으면 대기
            if (Managers.Network == null || !Managers.Network.IsListening)
            {
                UpdateMatchingStatusText("매칭 준비 중...");
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 현재 연결된 플레이어 수 확인
            int currentPlayers = Managers.Network.ConnectedClientsIds.Count;
            int requiredPlayers = Managers.GameMode?.MinPlayersToStart ?? 2;

            // 역할 확인 (Host or Client)
            string role = Managers.Network.IsHost ? "Host" : "Client";

            // UI 업데이트
            UpdateMatchingStatusText($"매칭 중... ({currentPlayers}/{requiredPlayers}명) [{role}]");

            // 2명 달성 시 메시지 변경 및 스피너 정리
            if (currentPlayers >= requiredPlayers)
            {
                CleanupMatchingUI();
                UpdateMatchingStatusText("매칭 완료! 게임 시작 중...");
                GameLogger.Success("UI_StartUpScene", "매칭 완료! 게임 시작");
                yield break; // 코루틴 종료
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 매칭 상태 텍스트 업데이트
    /// </summary>
    private void UpdateMatchingStatusText(string message)
    {
        // DisplayText를 사용 (MatchingStatusText가 UI에 없을 경우 대비)
        try
        {
            var text = GetText((int)Texts.DisplayText);
            if (text != null)
            {
                text.text = message;
            }
        }
        catch
        {
            // DisplayText가 없으면 무시
        }
    }

    #region 매칭 스피너 및 텍스트 깜빡임

    /// <summary>
    /// 매칭 스피너를 프로그래밍 방식으로 생성합니다.
    /// 흰색 원형 이미지를 DisplayText 위쪽에 배치합니다.
    /// AchievementUnlocked.cs의 프로그래밍 방식 UI 생성 패턴을 따릅니다.
    /// </summary>
    private void CreateSpinner()
    {
        // 기존 스피너가 있으면 제거
        if (_spinnerObject != null)
        {
            Destroy(_spinnerObject);
            _spinnerObject = null;
        }

        try
        {
            var displayText = GetText((int)Texts.DisplayText);
            if (displayText == null) return;

            // DisplayText의 부모 아래에 스피너 생성
            Transform parent = displayText.transform.parent;

            _spinnerObject = new GameObject("MatchingSpinner");
            _spinnerObject.transform.SetParent(parent, false);

            // RectTransform 설정
            RectTransform rt = _spinnerObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 40f);

            // DisplayText 위쪽에 배치
            RectTransform textRT = displayText.GetComponent<RectTransform>();
            rt.anchoredPosition = textRT.anchoredPosition + new Vector2(0f, 60f);
            rt.anchorMin = textRT.anchorMin;
            rt.anchorMax = textRT.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Image 컴포넌트 추가 (원형 스피너)
            Image spinnerImage = _spinnerObject.AddComponent<Image>();
            spinnerImage.color = Color.white;
            spinnerImage.sprite = CreateCircleSprite();
            spinnerImage.type = Image.Type.Filled;
            spinnerImage.fillMethod = Image.FillMethod.Radial360;
            spinnerImage.fillAmount = 0.75f; // 3/4 원형 (스피너 형태)

            GameLogger.Info("UI_StartUpScene", "매칭 스피너 생성 완료");
        }
        catch (Exception e)
        {
            GameLogger.Warning("UI_StartUpScene", $"스피너 생성 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 원형 스프라이트를 프로그래밍 방식으로 생성합니다.
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 스피너를 지속적으로 회전시키는 코루틴
    /// transform.Rotate를 사용하여 연속 회전합니다.
    /// </summary>
    private IEnumerator RotateSpinner()
    {
        while (_spinnerObject != null)
        {
            _spinnerObject.transform.Rotate(0f, 0f, -360f * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// 매칭 텍스트의 알파를 사인파로 깜빡이게 하는 코루틴
    /// AchievementUnlocked.cs의 FadeCanvasGroup 패턴을 참고한 알파 애니메이션
    /// </summary>
    private IEnumerator BlinkMatchingText()
    {
        TMPro.TMP_Text text = null;
        try
        {
            text = GetText((int)Texts.DisplayText);
        }
        catch
        {
            yield break;
        }

        if (text == null) yield break;

        Color originalColor = text.color;
        float time = 0f;

        while (_isMatching)
        {
            time += Time.deltaTime;
            // 사인파로 알파값 0.3 ~ 1.0 사이에서 부드럽게 변화
            float alpha = Mathf.Lerp(0.3f, 1.0f, (Mathf.Sin(time * 3f) + 1f) / 2f);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // 매칭 종료 시 원래 알파로 복구
        if (text != null)
        {
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        }
    }

    /// <summary>
    /// 매칭 UI (스피너 및 텍스트 깜빡임)를 정리합니다.
    /// 매칭 완료 또는 취소 시 호출합니다.
    /// </summary>
    private void CleanupMatchingUI()
    {
        _isMatching = false;

        if (_spinnerCoroutine != null)
        {
            StopCoroutine(_spinnerCoroutine);
            _spinnerCoroutine = null;
        }

        if (_textBlinkCoroutine != null)
        {
            StopCoroutine(_textBlinkCoroutine);
            _textBlinkCoroutine = null;
        }

        if (_spinnerObject != null)
        {
            Destroy(_spinnerObject);
            _spinnerObject = null;
        }

        // 텍스트 알파 복구
        try
        {
            var text = GetText((int)Texts.DisplayText);
            if (text != null)
            {
                Color c = text.color;
                text.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
        catch
        {
            // DisplayText가 없으면 무시
        }
    }

    private void OnDestroy()
    {
        CleanupMatchingUI();
    }

    #endregion

    /// <summary>
    /// 멀티플레이어 세션 시작 및 랜덤 매치 시도
    /// </summary>
    private async Task StartMultiplayerSession()
    {
        GameLogger.Progress("UI_StartUpScene", "멀티플레이어 세션 시작 준비");

        try
        {
            // 0. 멀티플레이어 모드로 전환 (2인 매칭)
            Managers.GameMode.SetMultiplayerMode();
            GameLogger.Success("UI_StartUpScene", "게임 모드: 멀티플레이어 (2인)");

            // 1. 인증 확인
            bool isAuthenticated = await Managers.Auth.EnsurePlayerIsAuthorized();
            if (!isAuthenticated)
            {
                GameLogger.Error("UI_StartUpScene", "플레이어 인증 실패");
                return;
            }
            GameLogger.Success("UI_StartUpScene", "플레이어 인증 완료");

            // 2. 랜덤 매치 시도 (QuickJoin)
            GameLogger.Info("UI_StartUpScene", "랜덤 매치 시도 중...");
            bool quickJoinSuccess = await Managers.Lobby.QuickJoinAsync();
            
            if (quickJoinSuccess)
            {
                GameLogger.Network("UI_StartUpScene", "랜덤 매치 성공! 기존 세션에 참가");
                return;
            }

            // 3. 기존 세션이 없으면 새 세션 생성 (Host 역할)
            GameLogger.Progress("UI_StartUpScene", "기존 세션 없음, 새 세션 생성 중...");
            string sessionName = $"AutoSession_{UnityEngine.Random.Range(1000, 9999)}";
            bool createSuccess = await Managers.Lobby.CreateLobbyAsync(sessionName, 4);
            
            if (createSuccess)
            {
                GameLogger.Network("UI_StartUpScene", "새 세션 생성 성공! 다른 플레이어 대기 중");
                
                // LocalUser를 Host로 설정
                Managers.LocalUser.IsHost = true;
                Managers.LocalUser.DisplayName = $"Player_{UnityEngine.Random.Range(100, 999)}";
                
                // ConnectionManager를 통해 Host 시작
                Managers.Connection.StartHostLobby(Managers.LocalUser.DisplayName);
                GameLogger.Success("UI_StartUpScene", $"Host로 세션 시작: {Managers.LocalUser.DisplayName}");
            }
            else
            {
                GameLogger.Error("UI_StartUpScene", "세션 생성 실패");
            }
        }
        catch (Exception e)
        {
            GameLogger.Error("UI_StartUpScene", $"멀티플레이어 세션 시작 중 오류: {e.Message}");
        }
    }

    void OnClickRecipeButton(PointerEventData evt)
    {
        Managers.UI.ShowPopupUI<UI_SettingsPopup>();
    }
    void OnClickExitButton(PointerEventData evt)
    {
	    Debug.Log("ExitButton");
	    Application.Quit();
    }
	/// <summary>
	/// 저장된 최고 점수를 UI에 표시
	/// </summary>
	private void DisplayHighScore()
	{
		try
		{
			int highScore = Managers.Game?.BrickGame?.SaveData?.HighScore ?? 0;
			var highScoreText = GetText((int)Texts.HighScoreText);
			if (highScoreText != null)
			{
				highScoreText.text = $"Best: {highScore}";
			}
		}
		catch (Exception e)
		{
			GameLogger.Warning("UI_StartUpScene", $"최고 점수 표시 실패: {e.Message}");
		}
	}

	void StartLoadAssets()
	{
		Managers.Resource.LoadAllAsync<UnityEngine.Object>("PreLoad", (key, count, totalCount) =>
		{
			Debug.Log($"<color=cyan>[UI_StartUpScene]</color> {key} {count}/{totalCount}");

			if (count == totalCount)
			{
				Managers.Data.Init();

				// // 데이터 있는지 확인
				// if (Managers.Game.LoadGame() == false)
				// {
				// 	Managers.Game.InitGame();
				// 	Managers.Game.SaveGame();
				// }

				GetObject((int)GameObjects.StartImage).gameObject.SetActive(true);
				// GetText((int)Texts.DisplayText).text = "Touch To Start";

				// 최고 점수 표시
				DisplayHighScore();
			}
		});
	}
}
