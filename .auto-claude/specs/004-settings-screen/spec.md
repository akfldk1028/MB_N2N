# BrickGame Settings Screen (설정 팝업)

## Overview
게임 설정 팝업 UI. 볼륨 조절, 진동 토글, 기본 설정.
기존 UIManager 팝업 스택 패턴 사용.

## Architecture Rules (반드시 준수)
- UI_Popup 상속하여 UI_SettingsPopup 생성
- Managers.UI.ShowPopupUI<UI_SettingsPopup>() 패턴
- 설정값은 PlayerPrefs 저장
- SoundManager(001)와 연동

## Requirements

### UI_SettingsPopup.cs
- BGM 볼륨 슬라이더 (0~100%)
- SFX 볼륨 슬라이더 (0~100%)
- 진동 On/Off 토글
- 닫기 버튼

### 접근 경로
- StartUpScene → RecipeButton (기존 빈 핸들러) → 설정 팝업
- GameScene → 일시정지 메뉴 → 설정 팝업

### PlayerPrefs 키
- "Settings_BGMVolume" (float 0~1)
- "Settings_SFXVolume" (float 0~1)
- "Settings_Vibration" (bool as int)

## Key Files to Create
- `Assets/@Scripts/UI/Popup/UI_SettingsPopup.cs`
- 설정 팝업 프리팹 (Canvas → Slider x2, Toggle x1, Button x1)

## Key Files to Modify
- `Assets/@Scripts/UI/Scene/UI_ StartUpScene.cs` - RecipeButton → Settings

## Acceptance Criteria
- [ ] 설정 팝업 열림/닫힘
- [ ] 볼륨 변경이 즉시 적용됨
- [ ] 설정이 앱 재시작 후에도 유지됨
- [ ] 컴파일 0 errors
