# BrickGame Pause System (일시정지)

## Overview
게임 중 일시정지 기능. 터치 영역 밖 탭 또는 전용 버튼으로 일시정지.
Time.timeScale = 0 기반.

## Architecture Rules (반드시 준수)
- BrickGameManager에 Pause/Resume 메서드 추가
- GamePhase에 Paused 상태 추가
- ActionBus로 Pause/Resume 이벤트 발행
- UI_PausePopup은 UIManager 팝업 스택 활용

## Requirements

### GamePhase 추가
- GamePhase.Paused 추가

### BrickGameManager 확장
- PauseGame() → Time.timeScale = 0, GamePhase.Paused
- ResumeGame() → Time.timeScale = 1, 이전 GamePhase 복귀
- IsPaused 프로퍼티

### UI_PausePopup.cs
- "계속하기" 버튼 → ResumeGame()
- "재시작" 버튼 → RestartGame()
- "나가기" 버튼 → StartUpScene으로 전환
- "설정" 버튼 → UI_SettingsPopup (004 연동)

### 일시정지 트리거
- 화면 상단 일시정지 버튼 (UI_BrickGameScene)
- Android 백 버튼 (006 연동)
- 앱 백그라운드 진입 시 자동 일시정지

## Key Files to Create
- `Assets/@Scripts/UI/Popup/UI_PausePopup.cs`

## Key Files to Modify
- `Assets/@Scripts/Contents/BrickGame/BrickGameManager.cs` - Pause/Resume
- `Assets/@Scripts/Contents/BrickGame/BrickGameState.cs` - GamePhase.Paused 추가
- `Assets/@Scripts/UI/Scene/UI_BrickGameScene.cs` - 일시정지 버튼

## Acceptance Criteria
- [ ] 일시정지 시 게임 완전 정지
- [ ] 재개 시 정상 동작
- [ ] 팝업에서 재시작/나가기 작동
- [ ] 백그라운드 진입 시 자동 일시정지
- [ ] 컴파일 0 errors
