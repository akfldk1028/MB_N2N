# BrickGame UI Polish (화면 전환/애니메이션)

## Overview
상용 모바일 게임 수준의 UI 폴리시. 씬 전환 페이드, 버튼 애니메이션, 로딩 화면.
기존 UIManager + UI_Base 패턴 유지.

## Architecture Rules (반드시 준수)
- UI_Base의 기존 바인딩 패턴 유지
- DOTween 또는 Unity Animation 사용 (LeanTween도 가능)
- CanvasGroup alpha를 이용한 페이드 전환
- UIManager의 팝업 스택 패턴 유지

## Requirements

### 씬 전환 효과 (SceneTransitionManager.cs)
- 페이드 아웃 (0.3초) → 로딩 → 페이드 인 (0.3초)
- 로딩 프로그레스 바 (Addressables 로딩 연동)
- Managers.Scene.LoadScene() 수정

### 버튼 애니메이션
- 터치 시 Scale 축소 (0.95) → Release 시 복귀 (1.0)
- UI_Base 또는 공통 ButtonAnimator 컴포넌트
- StartUpScene의 START/RECIPE/EXIT 버튼 적용

### 게임 결과 화면 애니메이션
- GameResultUIController의 Victory/Defeat 표시에 등장 애니메이션
- 점수 카운팅 애니메이션 (0 → 최종 점수)
- 별/등급 표시 애니메이션

### 매칭 대기 화면
- UI_StartUpScene의 매칭 중 상태에 로딩 스피너
- "매칭 중..." 텍스트 깜빡임

## Key Files to Modify
- `Assets/@Scripts/Managers/Core/SceneManagerEx.cs` - 페이드 전환 추가
- `Assets/@Scripts/UI/UI_Base.cs` - 버튼 애니메이션 유틸
- `Assets/@Scripts/Managers/Contents/BrickGame/UI/GameResultUIController.cs` - 등장 애니메이션
- `Assets/@Scripts/UI/Scene/UI_ StartUpScene.cs` - 매칭 대기 UI 개선

## Acceptance Criteria
- [ ] 씬 전환 시 페이드 효과
- [ ] 버튼 터치 피드백 애니메이션
- [ ] 게임 결과 등장 애니메이션
- [ ] 매칭 대기 시 로딩 스피너
- [ ] 컴파일 0 errors
