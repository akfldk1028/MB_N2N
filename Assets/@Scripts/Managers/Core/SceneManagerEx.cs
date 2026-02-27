/*
 * 씬 매니저 확장 (SceneManagerEx)
 *
 * 역할:
 * 1. Unity의 기본 SceneManager를 확장하여 게임 특화 씬 관리 기능 제공
 * 2. 씬 전환 시 필요한 로직 처리 및 로그 기록
 * 3. 현재 씬의 BaseScene 컴포넌트 참조 제공으로 씬별 고유 기능 접근 가능
 * 4. Define.EScene 열거형을 통한 타입 안전한 씬 전환 시스템 구현
 * 5. 씬 전환 시 필요한 리소스 정리 및 초기화 작업 관리
 * 6. Managers 클래스를 통해 전역적으로 접근 가능한 씬 관리 시스템 제공
 * 7. SceneTransitionManager를 통한 페이드 전환 효과 지원
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
	public SceneManagerEx()
	{
		GameLogger.SystemStart("SceneManagerEx", "생성됨");
	}

	public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }

	public void LoadScene(Define.EScene type)
	{
		//Managers.Clear();
		GameLogger.Progress("SceneManagerEx", $"LoadScene {type} 시작");

		// 전환 중 중복 호출 방지
		if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
		{
			GameLogger.Warning("SceneManagerEx", $"이미 씬 전환 중입니다. LoadScene({type}) 무시됨");
			return;
		}

		string sceneName = GetSceneName(type);

		// 🔒 멀티플레이어 씬 전환 체크
		var networkManager = Unity.Netcode.NetworkManager.Singleton;

		if (networkManager != null && networkManager.IsListening)
		{
			// 멀티플레이어 모드: NetworkManager의 SceneManager 사용
			if (networkManager.IsServer)
			{
				GameLogger.Info("SceneManagerEx", $"[Server] 페이드 전환으로 씬 로드: {sceneName}");

				// SceneTransitionManager로 페이드 전환 위임
				// 페이드 아웃 -> NetworkManager 씬 로드 -> 씬 로드 완료 대기 -> 페이드 인
				SceneTransitionManager.Instance.LoadSceneWithTransition(() =>
				{
					var status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

					if (status != Unity.Netcode.SceneEventProgressStatus.Started)
					{
						GameLogger.Error("SceneManagerEx", $"씬 전환 실패: {status}");
					}
				});
			}
			else
			{
				// Client는 Server의 명령을 기다림 (자동으로 씬 전환됨)
				GameLogger.Info("SceneManagerEx", $"[Client] Server의 씬 전환 명령 대기 중...");
			}
		}
		else
		{
			// 싱글플레이어 모드: SceneTransitionManager로 페이드 전환
			// 페이드 아웃 -> Unity SceneManager 씬 로드 -> 씬 로드 완료 대기 -> 페이드 인
			GameLogger.Info("SceneManagerEx", $"[SinglePlayer] 페이드 전환으로 씬 로드: {sceneName}");

			SceneTransitionManager.Instance.LoadSceneWithTransition(() =>
			{
				SceneManager.LoadScene(sceneName);
			});
		}
	}

	private string GetSceneName(Define.EScene type)
	{
		string name = System.Enum.GetName(typeof(Define.EScene), type);
		return name;
	}

	public void Clear()
	{
		//CurrentScene.Clear();
	}
}
