# MB_N2N BrickGame

Unity 6 멀티플레이어 벽돌깨기 + 땅따먹기 듀얼 게임.
화면: 왼쪽 30% (내 게임) | 중앙 40% (Territory Bar) | 오른쪽 30% (상대 게임)

## Architecture Rules

1. **Service Locator**: `Managers.cs` 싱글턴 → `Managers.Game`, `Managers.Sound`, `Managers.UI` 등. 새 Manager는 `Managers.cs` 프로퍼티 + `Init()`에 등록.
2. **Event-Driven**: `ActionMessageBus`로 모듈 간 통신. `DisposableSubscription`으로 구독 해제. 모듈 간 직접 참조 금지.
3. **Server Authority**: 게임 로직은 서버에서만 실행 (`MultiplayerUtil.HasServerAuthority()`). `BrickGameManager`(POCO) → `BrickGameNetworkSync`(NetworkBehaviour) 분리.
4. **POCO Manager**: 게임 로직 Manager는 MonoBehaviour 아닌 순수 C# 클래스.

## Module Boundaries

| Module | Path | Owner Agent |
|--------|------|-------------|
| Game Logic | `Contents/BrickGame/` | game-logic |
| Network | `Network/` | network |
| UI | `UI/` | ui |
| Physics | `Controllers/Object/` | physics |
| Infrastructure | `Infrastructure/` | infrastructure |
| Managers | `Managers/` | managers |

각 에이전트는 자기 모듈 파일만 수정. 모듈 간 통신은 반드시 ActionBus 또는 이벤트 델리게이트 사용.

## Agent Workflow (모든 에이전트 공통)

1. **Read First**: 수정할 파일을 반드시 먼저 읽기. 추측 금지.
2. **Search Dependencies**: `Grep`/`Glob`으로 관련 코드 탐색.
3. **Plan**: 수정 계획 수립 후 모듈 경계 위반 여부 확인.
4. **Implement**: 기존 패턴을 따라 코드 작성.
5. **Verify**: 컴파일 0 errors 확인.

## Tech Stack

- Unity 6000.x, Netcode for GameObjects v2.5.1, Unity Services Multiplayer v1.1.8
- Addressables, URP, IL2CPP
- Build: Android (API 26+), Windows, WebGL

## Conventions

- Asset 로드: `Managers.Resource.Load<T>("key")` (Addressables)
- 오브젝트 풀링: `Managers.Pool` 사용
- 설정값: `BrickGameSettings`에 집중
- UI 클래스: `UI_XxxScene.cs`, `UI_XxxPopup.cs` (UI_Base 상속)
- Sub-Controller: `BrickGameUIManager` → `ScoreUIController`, `TerritoryUIController`
- 새 모듈은 반드시 Managers 패턴 + ActionBus 이벤트 시스템을 따를 것

## MCP Unity 연결

이 프로젝트는 AnkleBreaker MCP (HTTP, port 7890)로 Unity 에디터에 연결되어 있다.
사용 가능한 도구: `mcp__unity__*` (Core ~70개 + Advanced ~130개)

### Unity 에디터 규칙
- Unity 에디터가 **열려있는 상태**에서만 MCP 도구가 동작한다
- `Unity.exe -batchmode`는 에디터가 열린 상태에서 **사용 불가** (UnityLockfile 잠금)
- 빌드/테스트는 `unity_execute_menu_item`으로 에디터 내에서 실행한다

### Play 모드
- 진입/종료: `unity_play_mode(action: "play"/"stop")`
- Play 모드 중 코드 수정 **금지** — 반드시 먼저 종료

### 스크린샷
- Game View: `unity_graphics_game_capture` (Play 모드에서, base64 인라인)
- Scene View: `unity_graphics_scene_capture` (Edit 모드 가능)
- 파일 캡쳐: `execute_menu_item("Tools/Capture Game View")` → `Screenshots/` 폴더

### 코드 수정 → 테스트 루프
1. `unity_scene_save()` → 코드 수정 → `unity_get_compilation_errors(severity: "error")`
2. `unity_execute_menu_item("Edit/Play")` → sleep 4초 → `unity_graphics_game_capture` → Play 종료
3. `unity_console_log(type: "error")` → 에러 확인

### 도구 사용 주의
- `unity_console_log` — 로그 조회 (type: "error"/"warning"/"all")
- Advanced 도구: `unity_advanced_tool(tool: "도구명")` 으로 접근
- 멀티 인스턴스 시 `unity_select_instance` 먼저 호출
