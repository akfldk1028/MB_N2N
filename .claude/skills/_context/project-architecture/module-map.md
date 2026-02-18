# Module Map — 전체 파일 ↔ 모듈 매핑

## Managers/Core (Service Locator)
| 파일 | 역할 | 접근 |
|------|------|------|
| `Managers/Managers.cs` | 싱글턴 Service Locator | `Managers.XXX` |
| `Managers/Core/DataManager.cs` | JSON 데이터 로드 | `Managers.Data` |
| `Managers/Core/ResourceManager.cs` | Addressables 리소스 | `Managers.Resource` |
| `Managers/Core/PoolManager.cs` | 오브젝트 풀링 | `Managers.Pool` |
| `Managers/Core/UIManager.cs` | UI 팝업 스택 관리 | `Managers.UI` |
| `Managers/Core/SceneManagerEx.cs` | 씬 전환 | `Managers.Scene` |

## Managers/Contents (게임 매니저)
| 파일 | 역할 | 접근 |
|------|------|------|
| `Managers/Contents/GameManager.cs` | 게임 매니저 (BrickGame, Rules, WinCondition 보유) | `Managers.Game` |
| `Managers/Contents/ObjectManager.cs` | 오브젝트 관리 | `Managers.Object` |
| `Managers/Contents/MapManager.cs` | 맵 관리 | `Managers.Map` |
| `Managers/Contents/CameraManager.cs` | 카메라 관리 | `Managers.Camera` |
| `Managers/Contents/BrickGame/Rules/GameRuleManager.cs` | 게임 규칙 (Strategy) | `Managers.Game.Rules` |
| `Managers/Contents/BrickGame/Rules/Impl/CannonBulletRule.cs` | 점수=총알 규칙 | `Managers.Game.Rules.ActiveRule` |
| `Managers/Contents/BrickGame/UI/BrickGameUIManager.cs` | BrickGame UI 통합 | `Managers.UI.BrickGame` |
| `Managers/Contents/BrickGame/UI/ScoreUIController.cs` | 점수 UI | - |
| `Managers/Contents/BrickGame/UI/TerritoryUIController.cs` | 영역 바 UI | - |
| `Managers/Contents/BrickGame/UI/GameResultUIController.cs` | 승리/패배 UI | - |

## Contents/BrickGame (게임 로직 POCO)
| 파일 | 역할 |
|------|------|
| `Contents/BrickGame/BrickGameManager.cs` | 코어 게임 루프 (POCO) |
| `Contents/BrickGame/BrickGameState.cs` | 게임 상태 (GamePhase enum) |
| `Contents/BrickGame/WinConditionManager.cs` | 승리 조건 (Cannon 파괴) |
| `Contents/BrickGame/BallManager.cs` | 공 관리 |
| `Contents/BrickGame/BrickManager.cs` | 벽돌 관리 |
| `Contents/BrickGame/PlankManager.cs` | 패들 입력 |

## Contents/Game (Territory 시스템)
| 파일 | 역할 |
|------|------|
| `Contents/Game/ColorfulCubeGrid.cs` | 등각 그리드 생성 + 블록 소유권 |
| `Contents/Game/Cannon.cs` | 대포 (HP, 발사, 회전) |
| `Contents/Game/CannonBullet.cs` | 총알 (블록 점령, 데미지) |
| `Contents/Game/BombComponent.cs` | 폭탄 특수능력 |
| `Contents/Game/HarvestComponent.cs` | 수확 특수능력 |
| `Contents/Game/CubeInPanel.cs` | 패널 내 큐브 |

## Network (멀티플레이어)
| 파일 | 역할 |
|------|------|
| `Network/BrickGameMultiplayerSpawner.cs` | 멀티 오케스트레이터 |
| `Network/BrickGameNetworkSync.cs` | 게임 상태 네트워크 동기화 |
| `Network/CentralMapBulletController.cs` | Enter키 전체발사 (멀티전용) |
| `Network/NetworkBulletPool.cs` | 네트워크 총알 풀링 |
| `Network/MultiplayerScoreSync.cs` | 점수 네트워크 동기화 |
| `Network/ConnectionManagement/ConnectionManagerEx.cs` | 연결 상태 머신 |
| `Network/ConnectionManagement/ConnectionState/*` | 각 연결 상태 |
| `Network/Lobbies/LobbyServiceFacadeEx.cs` | 로비/세션 관리 |

## Infrastructure (기반 시스템)
| 파일 | 역할 |
|------|------|
| `Infrastructure/Messages/IAction.cs` | ActionId enum + Payload 정의 |
| `Infrastructure/Messages/ActionMessageBus.cs` | 이벤트 버스 |
| `Infrastructure/Messages/ActionDispatcher.cs` | 프레임 이벤트 디스패처 |
| `Infrastructure/Messages/MessageChannel.cs` | 제네릭 pub/sub |
| `Infrastructure/State/StateMachine.cs` | 상태 머신 |
| `Infrastructure/DisposableSubscription.cs` | 구독 해제 유틸 |

## UI
| 파일 | 역할 |
|------|------|
| `UI/Scene/UI_BrickGameScene.cs` | 게임씬 메인 UI |
| `UI/Scene/UI_ StartUpScene.cs` | 시작화면 UI (공백 주의) |
| `UI/UI_Base.cs` | UI 기본 클래스 |
| `UI/Popup/UI_Popup.cs` | 팝업 기본 클래스 |
