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
