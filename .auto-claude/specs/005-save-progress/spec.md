# BrickGame Save/Progress System (저장 시스템)

## Overview
게임 진행 상태 저장. 최고 점수, 달성 레벨, 총 플레이 횟수.
기존 DataManager 패턴 활용.

## Architecture Rules (반드시 준수)
- DataManager를 통한 JSON 직렬화 또는 PlayerPrefs
- 게임 데이터 모델: BrickGameSaveData
- 저장 시점: 게임 종료, 스테이지 클리어, 앱 백그라운드

## Requirements

### BrickGameSaveData.cs (새 파일)
```csharp
public class BrickGameSaveData
{
    public int HighScore;
    public int MaxLevel;
    public int TotalGamesPlayed;
    public int TotalBricksDestroyed;
    public int TotalVictories;
    public string LastPlayDate;
}
```

### 저장/로드
- `Managers.Game.BrickGame.SaveProgress()`
- `Managers.Game.BrickGame.LoadProgress()`
- PlayerPrefs + JSON 직렬화

### ActionBus 연동
- GameOver → 자동 저장
- StageClear → 자동 저장
- Victory → 자동 저장
- ApplicationPause → 자동 저장

### UI 표시
- StartUpScene에 최고 점수 표시
- GameResultUI에 "New Record!" 표시

## Key Files to Create
- `Assets/@Scripts/Contents/BrickGame/BrickGameSaveData.cs`

## Key Files to Modify
- `Assets/@Scripts/Contents/BrickGame/BrickGameManager.cs` - Save/Load 메서드 추가
- `Assets/@Scripts/UI/Scene/UI_ StartUpScene.cs` - 최고 점수 표시

## Acceptance Criteria
- [ ] 최고 점수가 저장/로드됨
- [ ] 앱 재시작 후 데이터 유지
- [ ] New Record 표시 작동
- [ ] 컴파일 0 errors
