# BrickGame Tutorial (튜토리얼/온보딩)

## Overview
첫 플레이 시 간단한 튜토리얼. 화면 터치로 패들 이동, 공 발사 설명.
2~3 단계의 간단한 오버레이 방식.

## Architecture Rules (반드시 준수)
- TutorialManager는 Managers 패턴에 추가하거나 독립 컴포넌트
- PlayerPrefs "Tutorial_Completed" 플래그로 1회만 표시
- UI 오버레이 방식 (CanvasGroup + 하이라이트)

## Requirements

### 튜토리얼 단계
1. "화면을 터치하여 패들을 움직여보세요" (패들 영역 하이라이트)
2. "공이 벽돌을 파괴합니다!" (벽돌 영역 하이라이트)
3. "모든 벽돌을 파괴하면 승리!" (점수 영역 하이라이트)
4. 화면 탭으로 다음 단계 진행

### TutorialManager.cs
- ShowTutorial() → 튜토리얼 시작
- NextStep() → 다음 단계
- CompleteTutorial() → PlayerPrefs 저장, 오버레이 제거
- IsTutorialCompleted() 체크

### 트리거
- 첫 게임 시작 시 자동 (PlayerPrefs 확인)
- 설정에서 "튜토리얼 다시 보기" 옵션

## Key Files to Create
- `Assets/@Scripts/Managers/Contents/TutorialManager.cs`
- 튜토리얼 UI 프리팹 (오버레이 + 텍스트 + 하이라이트)

## Acceptance Criteria
- [ ] 첫 플레이 시 튜토리얼 표시
- [ ] 각 단계 터치로 진행
- [ ] 완료 후 재표시되지 않음
- [ ] 컴파일 0 errors
