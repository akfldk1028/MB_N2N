# Managers 액션 흐름 개요

```
[ Unity 루프 ]
      |
      v
Managers.cs (MonoBehaviour)
  - Init(): 싱글톤 인스턴스 보장
  - Awake(): 코어 시스템 연결
        |
        +-- ActionMessageBus 생성 (싱글톤 필드)
        +-- ActionDispatcher(bus) 생성
        +-- StateMachine(bus) 생성
  - Update(): PublishAction(System_Update)
  - LateUpdate(): PublishAction(System_LateUpdate)
  - FixedUpdate(): PublishAction(System_FixedUpdate)
  - 퍼블릭 API로 버스/상태 등록 헬퍼 제공
        |
        v
ActionMessageBus (Managers/Core/ActionMessageBus.cs)
  - Infrastructure의 MessageChannel<ActionMessage>를 래핑
  - ActionId 기반 필터 구독/발행 헬퍼 제공
  - `IsDisposed` 체크로 안전한 해제 보장
        |
        v
ActionDispatcher (Managers/Core/ActionDispatcher.cs)
  - ActionMessageBus에 단 한 번 구독
  - ActionId -> IAction 리스트 매핑 유지
  - ActionMessage가 오면 해당 아이디의 IAction.Execute 실행
        |
        v
IAction 구현체 (게임플레이/UI 등)
  - Managers.RegisterAction(new SomeAction()) 로 등록
  - Execute() 내부에서 도메인 로직 수행, 필요 시 추가 액션 발행/상태 전환
        |
        v
StateMachine (Infrastructure/State/StateMachine.cs)
  - ActionMessageBus 구독
  - 현재 상태가 CanHandle(ActionId)를 true로 리턴하면 메시지 처리
  - 상태는 필요 시 Managers.PublishAction(...)으로 추가 액션 발행 가능
```

## 협업 포인트
- `Managers.PublishAction(...)`이 모든 액션 메시지를 브로드캐스트하는 단일 진입점입니다.
- 루프 콜백이 필요한 시스템은 MonoBehaviour 업데이트를 오버라이드하지 말고 `ActionId.System_*` 구독을 활용하세요.
- `IAction`은 가급적 상태를 갖지 않는 명령 객체로 유지하고, 상태ful 행동은 `IState` 구현에 맡겨 StateMachine이 관리하도록 합니다.
- 씬 언로드나 앱 종료 시 `Managers.OnDestroy`가 Dispatcher, StateMachine, Bus 순으로 Dispose 하며 모든 구독을 정리합니다.

## 사용 예시 코드
```
// 액션 등록
Managers.RegisterAction(new Gameplay.StartSessionAction());

// 상태 전환
Managers.RegisterState(new States.LobbyState());
Managers.SetState(StateId.Lobby);

// MonoBehaviour 없이 업데이트 루프 수신
IDisposable subscription = Managers.Subscribe(ActionId.System_Update, OnTick);
```

새로운 액션 ID나 상태를 추가할 때는 `IAction.cs`에 Enum을 확장하고, 실제 액션은 도메인 폴더에, 상태는 `Infrastructure/State`나 씬별 폴더에 배치하는 구조를 지켜 주세요.