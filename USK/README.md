# USK - Unity Starter Kit for Claude Code Automation

CLI(Claude Code)에서 Unity 프로젝트를 자동으로 플레이테스트하기 위한 재사용 가능한 스크립트 모음.
MPPM(Multiplayer Play Mode) 멀티플레이어 테스트 자동화 포함.

## 폴더 구조

```
USK/
├── Runtime/
│   ├── MultiplayerCapture.cs      # 멀티플레이어 스크린샷 캡쳐 (메인+클론)
│   └── MPPMCloneAutoStart.cs      # MPPM 클론 자동 Start 버튼 클릭
├── Editor/
│   ├── CaptureTools.cs            # Game/Scene View 캡쳐 메뉴
│   ├── PlaytestHelper.cs          # 버튼 클릭, 상태 조회, 캡쳐 수집
│   ├── PlaytestPipeline.cs        # 전체 파이프라인 자동화
│   └── McpPlayModeRestart.cs      # MCP Play 모드 재연결
└── README.md
```

## 새 프로젝트에 적용하는 방법

1. `USK/` 폴더를 새 프로젝트의 `Assets/` 하위에 복사
2. `PlaytestHelper.cs`의 `GetGameState()` 메서드를 프로젝트에 맞게 수정
   - `Managers.Game.BrickGame` → 해당 프로젝트의 게임 매니저로 교체
3. `MPPMCloneAutoStart.cs`의 `StartButton` 이름을 해당 프로젝트의 시작 버튼 이름으로 교체
4. MPPM 패키지 설치 확인: `com.unity.multiplayer.playmode`

## CLI 사용법

```bash
# 컴파일 확인
execute_menu_item("Tools/Playtest/Run Full Pipeline")

# 또는 수동:
execute_menu_item("Edit/Play")
execute_menu_item("Tools/Playtest/Click Start Button")
execute_menu_item("Tools/Playtest/Get Game State")
execute_menu_item("Tools/Playtest/Trigger Capture")
execute_menu_item("Tools/Playtest/Collect Clone Screenshots")
execute_menu_item("Tools/Playtest/Stop Play Mode")
```

## 의존성

- Unity 6000.x+
- Multiplayer Play Mode (com.unity.multiplayer.playmode)
- Netcode for GameObjects (NetworkManager 상태 조회용)
