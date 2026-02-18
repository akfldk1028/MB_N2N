---
name: project-architecture
description: |
  MB_N2N BrickGame 프로젝트 아키텍처 참조. 모듈 구조, 이벤트 흐름, 파일 매핑.
  Claude가 코드 수정 전 자동으로 참조하는 배경 지식.
user-invocable: false
---

# Project Architecture Reference

## 프로젝트 개요
듀얼 게임: 블록깨기 + 땅따먹기 (1v1 멀티플레이어)
화면: 왼쪽 30% (내 게임) | 중앙 40% (Territory Bar) | 오른쪽 30% (상대 게임)

## Service Locator 패턴
```
Managers (MonoBehaviour Singleton, DontDestroyOnLoad)
├── Network: NetworkManager, ConnectionManagerEx, AuthManager, LobbyServiceFacadeEx
├── Contents: GameManager, ObjectManager, MapManager, GameModeService, CameraManager
├── Core: ActionMessageBus, StateMachine, DataManager, PoolManager, ResourceManager, UIManager
└── Access: Managers.Game.BrickGame, Managers.UI, Managers.Resource 등
```

## 이벤트 시스템 (ActionMessageBus)
```csharp
// 발행
Managers.PublishAction(ActionId.BrickGame_ScoreChanged, new BrickGameScorePayload(score, level));
// 구독
IDisposable sub = Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged);
// 해제
sub.Dispose();
```

## 핵심 ActionId 목록
- `BrickGame_ScoreChanged` — 점수 변경 (BrickGameScorePayload)
- `BrickGame_GameStateChanged` — 게임 상태 변경 (BrickGameStatePayload)
- `BrickGame_BrickDestroyed` — 벽돌 파괴 (BrickGameBrickDestroyedPayload)
- `BrickGame_GameEnded` — 게임 종료 (GameEndedPayload)
- `BrickGame_TerritoryChanged` — 영역 변경
- `BrickGame_BulletFired` — 총알 발사 (BulletFiredPayload)
- `Input_Fire` — Space키 (대포 발사)
- `Input_CentralMapFire` — Enter키 (전체 발사)
- `Input_UseMapComponent` — B/H키 (Bomb/Harvest)

## 모듈 구조
| 모듈 | 패턴 | 핵심 파일 |
|------|------|----------|
| BrickGameManager | POCO (비-MonoBehaviour) | `Contents/BrickGame/BrickGameManager.cs` |
| BrickGameNetworkSync | NetworkBehaviour | `Network/BrickGameNetworkSync.cs` |
| GameRuleManager | Strategy Pattern | `Managers/Contents/BrickGame/Rules/GameRuleManager.cs` |
| WinConditionManager | Observer Pattern | `Contents/BrickGame/WinConditionManager.cs` |
| BrickGameUIManager | Mediator | `Managers/Contents/BrickGame/UI/BrickGameUIManager.cs` |
| ConnectionManagerEx | State Machine | `Network/ConnectionManagement/ConnectionManagerEx.cs` |

## 씬 흐름
```
StartUpScene → [START: 멀티] → GameScene (네트워크 초기화)
StartUpScene → [RECIPE: 싱글] → GameScene (SetLocalTestMode)
GameScene.Init() → CameraManager → UI_BrickGameScene → BrickGameInitializer
```

## 주의사항
- @Managers, @NetworkSystems는 런타임에만 존재 (DontDestroyOnLoad)
- `UI_ StartUpScene.cs` 파일명에 공백 있음
- `UI_GameScene.cs`는 레거시 RPG UI (BrickGame과 무관)
- Relay 통합은 이미 완료 (ConnectionMethodRelayEx + WithRelayNetwork)

상세 모듈 매핑은 [module-map.md](module-map.md) 참조.
