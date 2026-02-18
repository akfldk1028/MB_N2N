# BrickGame Mobile Polish (모바일 최적화)

## Overview
Android/iOS 빌드에 필요한 모바일 최적화. Safe Area, 화면 방향, 백 버튼, 햅틱 피드백.

## Architecture Rules (반드시 준수)
- 기존 CanvasScaler 설정 유지 (ScaleWithScreenSize 800x600)
- Platform-specific 코드는 #if UNITY_ANDROID / #if UNITY_IOS 분기

## Requirements

### Safe Area 대응
- SafeAreaHandler.cs 컴포넌트 (노치/펀치홀 대응)
- Canvas 하위에 SafeArea RectTransform 추가
- 모든 UI가 Safe Area 내에 표시

### 화면 방향
- Portrait 고정 (세로 모드)
- ProjectSettings에서 설정

### Android 백 버튼
- GameScene: 일시정지 메뉴 표시
- StartUpScene: 종료 확인 팝업
- 팝업 열려있을 때: 팝업 닫기

### 햅틱 피드백
- 벽돌 파괴 시 짧은 진동 (Handheld.Vibrate 또는 Android native)
- 설정에서 On/Off 가능 (004-settings-screen과 연동)

### Player Settings
- Package Name: com.ac247.brickgame
- Minimum API Level: 26 (Android 8.0)
- Target API Level: 34
- Scripting Backend: IL2CPP
- Target Architecture: ARM64

## Key Files to Create
- `Assets/@Scripts/UI/SafeAreaHandler.cs`

## Key Files to Modify
- `ProjectSettings/ProjectSettings.asset` - Android 설정
- `Assets/@Scripts/UI/Scene/UI_ StartUpScene.cs` - 백 버튼 처리
- `Assets/@Scripts/Contents/BrickGame/BrickGameManager.cs` - 백 버튼 → 일시정지

## Acceptance Criteria
- [ ] Safe Area가 노치 기기에서 작동
- [ ] 세로 모드 고정
- [ ] 백 버튼이 올바르게 동작
- [ ] 햅틱 피드백 작동 (설정 연동)
- [ ] Android 빌드 성공
- [ ] 컴파일 0 errors
