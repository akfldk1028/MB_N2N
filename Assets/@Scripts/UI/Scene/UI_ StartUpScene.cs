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

#if UNITY_EDITOR
        // MPPM 감지 시 Unity Services 우회하여 직접 로컬 연결
        if (IsMPPMActive())
        {
            await StartLocalMPPMSession();
            GameLogger.Info("UI_StartUpScene", "[MPPM] 직접 연결 완료, 매칭 대기 중...");
            return;
        }
#endif

        // 멀티플레이어 세션 시작 및 매칭 대기
        // ConnectionManagerEx가 2명 달성 시 자동으로 GameScene으로 전환
        await StartMultiplayerSession();

        GameLogger.Info("UI_StartUpScene", "매칭 대기 중... (2명 필요)");
    }

#if UNITY_EDITOR
    /// <summary>
    /// MPPM(Multiplayer Play Mode)이 활성화된 상태인지 감지
    /// 클론 인스턴스이거나, 메인 에디터에서 MPPM 태그가 있는 경우 true
    /// 일반 단일 Play(MPPM 없음)에서는 false
    /// </summary>
    private bool IsMPPMActive()
    {
        try
        {
            // CurrentPlayer는 static 클래스 — 인스턴스가 아닌 타입으로 직접 접근
            // 클론 인스턴스인 경우 항상 MPPM
            if (!Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
                return true;

            // 메인 에디터에서 MPPM 태그가 있으면 MPPM 활성 상태
            var tags = Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags();
            if (tags != null && tags.Length > 0)
                return true;

            // Library/VP 폴더가 존재하면 MPPM 활성 (클론이 생성된 상태)
            string vpPath = System.IO.Path.Combine(Application.dataPath, "..", "Library", "VP");
            if (System.IO.Directory.Exists(vpPath) && System.IO.Directory.GetDirectories(vpPath).Length > 0)
            {
                GameLogger.Info("UI_StartUpScene", "[MPPM] Library/VP 폴더 감지 → MPPM 활성");
                return true;
            }

            return false;
        }
        catch
        {
            // MPPM 패키지가 없거나 접근 불가 시
            return false;
        }
    }

    /// <summary>
    /// MPPM 환경에서 Unity Services 없이 직접 Host/Client로 연결
    /// 메인 에디터 → Host (127.0.0.1:7777)
    /// 클론 → 약간의 딜레이 후 Client 연결
    /// </summary>
    private async Task StartLocalMPPMSession()
    {
        GameLogger.Progress("UI_StartUpScene", "[MPPM] 로컬 직접 연결 모드 시작");

        // Managers 초기화 + NetworkManager + Transport + NetworkPrefabs 전부 대기
        float waitTime = 0f;
        Unity.Netcode.Transports.UTP.UnityTransport transport = null;
        while (waitTime < 30f)
        {
            bool ready = Managers.Initialized
                && Managers.Network != null
                && Managers.Connection != null;

            if (ready)
            {
                transport = Managers.Network.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                // NetworkPrefabs가 로드될 때까지 대기 (ConfigureNetworkManager async 완료 대기)
                if (transport != null && Managers.Network.NetworkConfig?.Prefabs?.NetworkPrefabsLists?.Count > 0)
                    break;
            }

            await Task.Delay(200);
            waitTime += 0.2f;

            if (waitTime % 3f < 0.2f)
                GameLogger.Info("UI_StartUpScene", $"[MPPM] 초기화 대기 중... ({waitTime:F1}초)");
        }

        if (transport == null || Managers.Network == null)
        {
            GameLogger.Error("UI_StartUpScene", $"[MPPM] 초기화 타임아웃 (30초) - Network={Managers.Network != null}, Transport={transport != null}");
            return;
        }
        GameLogger.Success("UI_StartUpScene", $"[MPPM] 네트워크 초기화 완료 ({waitTime:F1}초 대기, Prefabs={Managers.Network.NetworkConfig?.Prefabs?.NetworkPrefabsLists?.Count ?? 0})");

        // 멀티플레이어 모드 설정 (2인)
        Managers.GameMode.SetMultiplayerMode();
        GameLogger.Success("UI_StartUpScene", "[MPPM] 게임 모드: 멀티플레이어 (2인)");

        bool isMainEditor = Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor;

        if (isMainEditor)
        {
            // 메인 에디터 → Host
            GameLogger.Network("UI_StartUpScene", "[MPPM] 메인 에디터 감지 → Host로 시작 (127.0.0.1:7777)");
            string playerName = "Player_Host";
            Managers.LocalUser.IsHost = true;
            Managers.LocalUser.DisplayName = playerName;
            Managers.Connection.StartHostDirect(playerName);
        }
        else
        {
            // 클론 → Client (Host가 먼저 시작될 시간 확보)
            GameLogger.Network("UI_StartUpScene", "[MPPM] 클론 에디터 감지 → Client로 시작");
            string playerName = "Player_Client";
            Managers.LocalUser.IsHost = false;
            Managers.LocalUser.DisplayName = playerName;

            // Host 초기화 대기 + 재시도 (최대 8회)
            for (int attempt = 1; attempt <= 8; attempt++)
            {
                // 첫 시도는 Host 준비 대기 길게, 이후는 짧게
                int waitMs = attempt == 1 ? 5000 : 3000;
                await Task.Delay(waitMs);

                GameLogger.Info("UI_StartUpScene", $"[MPPM] Client 연결 시도 {attempt}/8 (IsListening={Managers.Network.IsListening})");

                // 이전 연결이 남아있으면 정리 후 대기
                if (Managers.Network.IsListening)
                {
                    Managers.Network.Shutdown();
                    await Task.Delay(3000); // Shutdown 완료 충분히 대기
                    GameLogger.Info("UI_StartUpScene", $"[MPPM] Shutdown 후 대기 완료");
                }

                Managers.Connection.StartClientDirect(playerName);

                // 연결 성공 대기 (최대 8초)
                float checkTime = 0f;
                while (checkTime < 8f)
                {
                    await Task.Delay(500);
                    checkTime += 0.5f;
                    if (Managers.Network.IsConnectedClient)
                    {
                        GameLogger.Success("UI_StartUpScene", $"[MPPM] Client 연결 성공! (시도 {attempt})");
                        return;
                    }
                }

                GameLogger.Warning("UI_StartUpScene", $"[MPPM] Client 연결 시도 {attempt} 실패, 재시도...");
            }

            GameLogger.Error("UI_StartUpScene", "[MPPM] Client 연결 8회 모두 실패");
        }
    }
#endif

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
        GameLogger.Info("UI_StartUpScene", "설정 팝업 열기 버튼 클릭!");
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
