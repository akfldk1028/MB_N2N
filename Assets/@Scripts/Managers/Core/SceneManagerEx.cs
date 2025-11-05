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
		SceneManager.LoadScene(GetSceneName(type));
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
