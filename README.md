# MB_N2N BrickGame - Multiplayer Dual Game

Unity 멀티플레이어 듀얼 게임: **블록깨기 + 땅따먹기** 동시 진행

## Game Structure

```
+------------+------------------+------------+
| Left 30%   |   Center 40%     | Right 30%  |
| My Game    | Territory Bar    | Opponent   |
| (Bricks)   | (Land Control)   | (Bricks)   |
+------------+------------------+------------+
```

- 1vs1 대전: 각 플레이어가 자신의 벽돌깨기를 플레이
- 점수가 Territory 영역에 반영
- Cannon 파괴 시 승패 판정

## Architecture

**핵심 원칙**: 모듈 유지보수 최우선

### Design Patterns
- **Service Locator**: `Managers` 싱글턴 (런타임 @Managers GameObject, DontDestroyOnLoad)
- **ActionMessageBus**: 이벤트 발행/구독 (ActionId enum + DisposableSubscription)
- **State Pattern**: `ConnectionStateEx` 계층 (네트워크 상태 관리)
- **Server Authority**: 모든 게임 로직은 서버에서만 실행 (`MultiplayerUtil.HasServerAuthority()`)
- **POCO Manager**: BrickGameManager는 비-MonoBehaviour

### Module Structure

```
Managers (Service Locator)
├── Game
│   ├── BrickGame (BrickGameManager - POCO)
│   │   ├── BallManager
│   │   ├── BrickManager
│   │   ├── PlankManager
│   │   ├── PowerUpDropManager
│   │   ├── ComponentChargeManager
│   │   └── WinConditionManager
│   └── GameManager
├── UI
│   └── BrickGame (BrickGameUIManager)
│       ├── Score (ScoreUIController)
│       ├── Territory (TerritoryUIController)
│       ├── GameResult (GameResultUIController)
│       └── ComponentGauge (ComponentGaugeUIController)
├── Network
│   ├── ConnectionManagerEx (State Pattern)
│   ├── BrickGameNetworkSync (NetworkBehaviour)
│   └── LobbyServiceFacadeEx (Sessions API)
├── Animation
│   ├── UIAnimationHelper (Coroutine host)
│   ├── SceneTransitionManager (Fade transitions)
│   └── ButtonAnimator (Press feedback)
└── Core
    ├── DataManager
    ├── ResourceManager
    ├── PoolManager
    ├── UIManager
    └── SceneManagerEx
```

### Key Files

| Category | File | Role |
|----------|------|------|
| **Game Logic** | `Contents/BrickGame/BrickGameManager.cs` | Core game logic (POCO) |
| **Game State** | `Contents/BrickGame/GamePhase.cs` | Idle/Playing/Paused/GameOver/StageClear/Victory |
| **Network** | `Network/BrickGameNetworkSync.cs` | Server→Client sync via NetworkedMessageChannel |
| **Connection** | `Network/ConnectionManagement/ConnectionManagerEx.cs` | State-based connection management |
| **Relay** | `Network/ConnectionManagement/Common/ConnectionMethodRelayEx.cs` | Sessions API Relay wrapper |
| **Lobby** | `Network/Lobbies/LobbyServiceFacadeEx.cs` | Sessions API facade |
| **UI** | `Managers/Contents/BrickGame/UI/BrickGameUIManager.cs` | UI coordinator |
| **Input** | `Contents/BrickGame/PlankManager.cs` | Touch/Mouse/Keyboard input |
| **Win** | `Contents/BrickGame/WinConditionManager.cs` | Cannon destruction win condition |
| **Events** | `Infrastructure/Messages/ActionMessageBus.cs` | ActionBus pub/sub |
| **Managers** | `Managers/Managers.cs` | Service Locator singleton |

### Event Flow (ActionBus)

```
[Server] BrickGameManager.OnScoreChanged
  → BrickGameNetworkSync.HandleScoreChanged
    → NetworkedMessageChannel<BrickGameScoreMessage>.Publish (network)
    → ActionBus.Publish(BrickGame_ScoreChanged) (local)
      → BrickGameUIManager.OnScoreChanged
        → ScoreUIController.UpdateScores
        → TerritoryUIController.UpdateRatio
```

## Tech Stack

- **Unity 6** (6000.x)
- **Netcode for GameObjects** v2.5.1
- **Unity Services Multiplayer** v1.1.8 (Sessions API + auto Relay)
- **Unity Lobby** v1.2.2
- **TMPro** (TextMeshPro)
- **Addressables**

## Network Architecture

Sessions API (com.unity.services.multiplayer v1.1.8)가 Relay를 자동 처리:
- `WithRelayNetwork()` → Relay allocation 자동
- `UnityTransport.SetRelayServerData()` 자동 설정
- `StartHost()`/`StartClient()` Sessions lifecycle에 포함

## Build Targets

- Android (Min API 26+)
- Windows (Standalone)
- WebGL

## Project Setup

1. Unity 6 에서 프로젝트 열기
2. Unity Services 연결 (Project Settings → Services)
3. Relay + Lobby 서비스 활성화
4. Play Mode에서 테스트

## Related Projects

- **AC247**: Auto-Claude 자동화 데몬 (`D:\Data\AC247_MB\AC247`)
  - `daemon_runner.py`: 자동 코드 수정/빌드 데몬
  - `spec_runner.py`: 태스크 자동 분해

## Current Status (2026-02-27)

### Completed
- [x] ConnectionApproval 버그 수정
- [x] BrickGameManager 게임 흐름 (StageClear/Victory/RestartGame)
- [x] GameResultUIController 생성 + BrickGameUIManager 통합
- [x] PlankManager 터치 입력 추가
- [x] BrickGameNetworkSync StageClear/Victory 이벤트 동기화
- [x] GamePhase에 Victory 추가
- [x] 파티클 이펙트 시스템 (002)
- [x] UI 폴리시 - 씬 전환 페이드, 버튼 애니메이션, 결과 애니메이션, 매칭 스피너 (003)
- [x] 설정 팝업 (BGM/SFX 볼륨, 진동 토글) (004)
- [x] 세이브/로드 시스템 (HighScore, 자동 저장) (005)
- [x] 모바일 폴리시 (SafeArea, Android 빌드 설정) (006)
- [x] 일시정지 시스템 (Time.timeScale) (007)
- [x] 튜토리얼 (008)
- [x] 파워업 드롭 시스템 (Star/BonusBall) (011)
- [x] 파워업 종류 추가 (012)
- [x] 특수 벽돌 (SteelBrick) (013)
- [x] 콤보 시스템 (014)
- [x] 컴포넌트 게이지 충전 루프 (Bomb/Harvest) (015)
- [x] 코드 리뷰 및 버그 수정

### Remaining
- [ ] PlayMode 테스트 자동화
- [ ] 멀티플랫폼 빌드 설정
- [ ] Lobby UI 완성 (매칭 대기, JoinCode)
