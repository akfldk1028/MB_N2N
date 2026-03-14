# USK - Unity Starter Kit for Claude Code

다른 Unity 프로젝트에 복사하여 CLI 자동 플레이테스트 가능하게 하는 스크립트 모음.

## 새 프로젝트 적용 (5단계)

### 1. 파일 복사
```
USK/Runtime/ → Assets/Scripts/Utils/ (또는 원하는 Runtime 경로)
USK/Editor/ → Assets/Editor/
```

### 2. 설정 변경
`ProjectSettings/McpUnitySettings.json`:
```json
{ "AllowRemoteConnections": true }
```

### 3. 프로젝트별 수정
- `PlaytestHelper.cs` → `GetGameState()` 내 게임 매니저 접근 코드 수정
- `MPPMCloneAutoStart.cs` → `ButtonName` 상수를 프로젝트 시작 버튼 이름으로 변경

### 4. 패키지 필수
- `com.unity.multiplayer.playmode` (MPPM)
- `com.unity.netcode.gameobjects` (Netcode)
- `com.gamelovers.mcp-unity` (MCP Unity)

### 5. Unity 재시작

## 검증된 테스트 플로우

```bash
/mcp                                                    # MCP 연결
# MPPM 창 열기 (Window > Multiplayer Play Mode)
execute_menu_item("Edit/Play")                          # Play 진입
execute_menu_item("Tools/Playtest/Click Start Button")  # 메인 Start (클론 자동)
# 15초 대기
execute_menu_item("Tools/Playtest/Get Game State")      # Players=2/2 확인
execute_menu_item("Tools/Playtest/Trigger Capture")     # 메인 캡쳐
execute_menu_item("Tools/Playtest/Collect Clone Screenshots")  # 클론 캡쳐 수집
get_console_logs(includeStackTrace: false)               # 로그 확인
execute_menu_item("Tools/Playtest/Stop Play Mode")      # 종료
```

## 메뉴 목록

| 메뉴 | 기능 |
|------|------|
| `Tools/Playtest/Click Start Button` | StartButton 클릭 |
| `Tools/Playtest/Click Restart Button` | RestartButton 클릭 |
| `Tools/Playtest/Click Lobby Button` | LobbyButton 클릭 |
| `Tools/Playtest/Get Game State` | Phase/Score/Players/Role/Scene |
| `Tools/Playtest/Trigger Capture` | 메인 스크린샷 |
| `Tools/Playtest/Collect Clone Screenshots` | 클론 캡쳐 수집 |
| `Tools/Playtest/Stop Play Mode` | Play 종료 |
| `Tools/Playtest/Run Full Pipeline` | 전체 자동화 |

## 핵심 패치: McpPlayModeRestart.cs

이 파일이 해결하는 문제 3가지:
1. **MPPM 클론 오인**: `McpUtils._isMultiplayerPlayModeClone` 캐시를 false로 리셋
2. **Play 모드 서버 종료**: 패키지의 `OnPlayModeStateChanged` 핸들러 제거
3. **Assembly Reload 서버 종료**: `OnBeforeAssemblyReload` 핸들러 제거

## 스크린샷 경로
- 메인: `Screenshots/mp_player1_*.png`
- 클론: `Library/VP/mppm{hash}/Screenshots/` → 수집 후 `Screenshots/clone_*.png`
