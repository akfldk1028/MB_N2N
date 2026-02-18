---
name: game-audit
description: |
  BrickGame + Territory 듀얼 게임 전체 감사. 모듈 연결, 이벤트 흐름, 버그 탐지.
  사용 시점: (1) 프로덕션 점검, (2) 새 기능 추가 전 상태 파악, (3) 버그 원인 추적
  사용 금지: 단순 컴파일 확인(unity-validate), 특정 파일만 수정
argument-hint: "[all|brickgame|territory|network|ui]"
allowed-tools: Read, Grep, Glob, Bash, mcp__mcp-unity__recompile_scripts, mcp__mcp-unity__get_console_logs, mcp__mcp-unity__get_scene_info, mcp__mcp-unity__run_tests
---

# Game Audit

BrickGame + Territory 듀얼 게임 전체를 체계적으로 감사합니다.

## When to Use
- 프로덕션 출시 전 품질 점검
- 새 기능 추가 전 현재 상태 파악
- 버그 원인이 불명확할 때 전체 흐름 추적
- 모듈 간 연결 상태 확인

## When NOT to Use
- 단순 컴파일 확인 → `/unity-validate`
- 특정 버그 수정 → 직접 코드 수정
- 씬 구조만 확인 → `/unity-scene`

## Quick Start
```
/game-audit all         # 전체 감사
/game-audit territory   # 땅따먹기 시스템만
```

## 감사 영역

### brickgame - 블록깨기 시스템
검사 대상:
- `BrickGameManager.cs` - 게임 루프, 점수, 레벨
- `BrickGameState.cs` - 상태 관리
- `BrickGameNetworkSync.cs` - 네트워크 동기화
- `BrickGameInitializer.cs` - 초기화 흐름
- 이벤트 흐름: AddScore → BrickGameScorePayload → ActionBus → UI

### territory - 땅따먹기 시스템
검사 대상:
- `IsometricGridGenerator.cs` - Grid 생성, 블록 소유권
- `Cannon.cs` - 대포 (HP, 발사, 회전)
- `CannonBullet.cs` - 총알 (블록 점령, 캐논 데미지)
- `CentralMapBulletController.cs` - 발사 오케스트레이터
- `CannonBulletRule.cs` - 점수=총알 규칙
- `BombComponent.cs` / `HarvestComponent.cs` - 특수 능력
- 이벤트 흐름: Space→CannonBulletRule→Cannon.Fire, Enter→ServerRpc→CentralMap

### network - 네트워크 시스템
검사 대상:
- `BrickGameMultiplayerSpawner.cs` - 멀티 오케스트레이터
- `NetworkBulletPool.cs` - 총알 풀링
- NetworkVariable 동기화 (점수, Territory)
- ServerRpc/ClientRpc 흐름
- WinCondition 네트워크 동기화

### ui - UI 시스템
검사 대상:
- `UI_BrickGameScene.cs` - 게임 UI 바인딩
- `BrickGameUIManager.cs` - UI 통합 매니저
- `ScoreUIController.cs` - 점수 표시
- `GameResultUIController.cs` - 승리/패배 화면
- `TerritoryUIController.cs` - 영역 바
- Payload 일치 여부 (SP vs MP)

## 감사 체크리스트
각 영역에서 확인:
1. 이벤트 발행자(Publisher) → 구독자(Subscriber) 매칭
2. Payload 타입 일치 여부
3. 싱글플레이어/멀티플레이어 양쪽 경로 존재
4. null 체크, 에러 핸들링
5. 리소스 정리 (Dispose, OnDestroy)

## 결과 리포트 형식
```
## 감사 결과 (YYYY-MM-DD)

### 컴파일: ✅/❌
### 테스트: ✅/❌

### 발견된 버그
- BUG-XXX: [설명] (심각도: CRITICAL/HIGH/MEDIUM/LOW)

### 모듈 상태
| 모듈 | 상태 | 비고 |
|------|------|------|
| BrickGame | ✅ | 정상 |
| Territory | ⚠️ | BUG-004 수정됨 |
```

## Related Skills
- `/unity-validate` - 빠른 검증
- `/unity-scene` - 씬 구조
- `/unity-build` - 빌드
