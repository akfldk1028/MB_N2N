---
name: coding-standards
description: |
  MB_N2N 프로젝트 코딩 규칙. 새 코드 작성 시 Claude가 자동 참조.
  POCO 패턴, ActionBus 이벤트 흐름, Dispose 패턴, 네이밍 규칙.
user-invocable: false
---

# Coding Standards

## 패턴 규칙

### 1. Manager 추가
```csharp
// Managers.cs Contents 섹션에 추가:
private SoundManager _sound = new SoundManager();
public static SoundManager Sound { get { return Instance?._sound; } }
// Awake()에서 Init() 호출
```

### 2. ActionBus 이벤트 추가
```csharp
// 1. IAction.cs에 ActionId 추가
BrickGame_NewEvent,

// 2. Payload 구조체 추가 (같은 파일 또는 별도)
public readonly struct NewEventPayload : IActionPayload
{
    public int Value { get; }
    public NewEventPayload(int value) { Value = value; }
}

// 3. 발행
Managers.PublishAction(ActionId.BrickGame_NewEvent, new NewEventPayload(42));

// 4. 구독 (IDisposable 반드시 보관)
_sub = Managers.Subscribe(ActionId.BrickGame_NewEvent, OnNewEvent);

// 5. 해제 (OnDestroy/Cleanup)
_sub?.Dispose();
```

### 3. UI 팝업 추가
```csharp
// 1. UI_Popup 상속
public class UI_NewPopup : UI_Popup { }

// 2. 프리팹: Assets/@Resources/Prefabs/UI/Popup/UI_NewPopup.prefab

// 3. 표시
Managers.UI.ShowPopupUI<UI_NewPopup>();

// 4. 닫기
Managers.UI.ClosePopupUI();
```

### 4. 네트워크 동기화 규칙
- 게임 로직 = POCO (BrickGameManager)
- 네트워크 동기화 = NetworkBehaviour (BrickGameNetworkSync)
- **절대 섞지 않음**
- Server Authority: `MultiplayerUtil.HasServerAuthority()` 필수 체크
- SP/MP 양쪽 경로 반드시 구현

### 5. 네이밍 규칙
- Manager 클래스: `XXXManager` (POCO)
- NetworkBehaviour: `XXXNetworkSync`, `XXXController`
- UI 클래스: `UI_XXX` (UI_Base 상속)
- Payload: `XXXPayload : IActionPayload` (readonly struct)
- ActionId: `Category_EventName` (예: `BrickGame_ScoreChanged`)

### 6. 리소스 정리 필수
- IDisposable 구독은 반드시 Dispose
- NetworkBehaviour는 OnDestroy에서 정리
- POCO는 Cleanup() 메서드 제공
