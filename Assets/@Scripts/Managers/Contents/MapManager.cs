/*
 * 맵 매니저 (MapManager)
 * 
 * 역할:
 * 1. 게임 맵 데이터 로드 및 관리
 * 2. 맵 정보 (이름, 크기, 구조 등) 저장 및 접근 기능 제공
 * 3. 맵 그리드 시스템 관리 - 좌표계 변환 및 위치 기반 기능 지원
 * 4. 타일맵 기반 게임에서 타일 정보 접근 및 관리
 * 5. 게임 내 지형 및 장애물 데이터 제공
 * 6. 맵 관련 이벤트 및 상호작용 처리
 * 7. Managers 클래스를 통해 전역적으로 접근 가능한 맵 데이터 제공
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Define;


public class MapManager
{
	public MapManager()
	{
		Debug.Log("<color=magenta>[MapManager]</color> 생성됨");
	}
	
	public GameObject Map { get; private set; }
	public string MapName { get; private set; }
	public Grid CellGrid { get; private set; }



}
