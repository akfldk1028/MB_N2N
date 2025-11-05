# 메시지 채널 인터페이스 개요

```
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
| 인터페이스         | 주요 멤버              | 책임                                           | 대표 구현                               |
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
| IPublisher<T>      | + Publish(T message)  | 등록된 모든 구독자에게 메시지를 브로드캐스트 | MessageChannel<T>, ActionMessageBus      |
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
| ISubscriber<T>     | + Subscribe(handler)  | 메시지를 수신할 콜백을 등록                   | MessageChannel<T>, BufferedMessageChannel|
|                    | + Unsubscribe(handler)| 등록된 콜백을 해제                             | ActionMessageBus                         |
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
| IMessageChannel<T> | (위 두 인터페이스 상속)| Pub/Sub 전체 기능 제공, 라이프사이클 제어      | MessageChannel<T>, NetworkedMessageChannel|
|                    | + Dispose()           | 안전한 해제를 위한 IsDisposed 노출            | ActionMessageBus                         |
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
| IBufferedMsgCh<T>  | + HasBufferedMessage  | 가장 최근 메시지를 저장                        | BufferedMessageChannel<T>               |
|                    | + BufferedMessage     | 신규 구독자에게 즉시 재생                      |                                         |
+--------------------+-----------------------+-----------------------------------------------+-----------------------------------------+
```

## 인터페이스별 차이점 요약
- **IPublisher<T>**: 발행만 담당합니다. 구독 리스트를 직접 갖지 않고, 실제 구현이 `Publish` 호출 시 모든 대상에게 브로드캐스트합니다.
- **ISubscriber<T>**: 발행은 하지 않고, 콜백 등록/해제를 통제합니다. 콜백을 반환값(`IDisposable`)으로 관리해 구독 해제가 쉽습니다.
- **IMessageChannel<T>**: 발행과 구독을 모두 제공하며, 채널 상태(Dispose 여부)를 노출합니다. 대부분의 메시지 시스템에서 사용하는 기본 계약입니다.
- **IBufferedMessageChannel<T>**: `IMessageChannel<T>`에 "마지막 메시지 저장" 기능이 추가된 특수 채널입니다. 새로운 구독자가 즉시 최신 상태를 받을 수 있습니다.

## 간단한 사용 예시
### 1. MessageChannel<T> - 기본 Pub/Sub
```csharp
var channel = new MessageChannel<string>();
var subscription = channel.Subscribe(msg => Debug.Log($"수신: {msg}"));
channel.Publish("Hello");   // -> "수신: Hello"
subscription.Dispose();      // 구독 해제
```

### 2. BufferedMessageChannel<T> - 최근 메시지 재생
```csharp
var buffered = new BufferedMessageChannel<int>();
buffered.Publish(42);

// 나중에 구독하더라도 직전에 발행된 42를 즉시 받습니다.
buffered.Subscribe(value => Debug.Log($"최근 값: {value}"));
```

### 3. ActionMessageBus - ActionId 필터링
```csharp
var bus = new ActionMessageBus();
var disposable = bus.Subscribe(ActionId.System_Update, () => Debug.Log("업데이트 틱"));
bus.Publish(ActionMessage.From(ActionId.System_Update));
```

### 4. NetworkedMessageChannel<T> - 서버 브로드캐스트 예시
```csharp
var networkChannel = new NetworkedMessageChannel<PlayerState>();
networkChannel.Initialize(NetworkManager.Singleton);

// 서버에서만 Publish 가능, 클라이언트는 Subscribe만 가능
if (NetworkManager.Singleton.IsServer)
{
    networkChannel.Publish(new PlayerState { Hp = 100 });
}
```

## 체크리스트
- 메시지를 발행하기 전에 `IsDisposed`를 확인해 이미 해제된 채널인지 검증합니다.
- 구독 등록 시 중복 핸들러가 들어가지 않도록 검사합니다.
- 네트워크 채널을 사용할 땐 `Initialize(NetworkManager)`를 먼저 호출합니다.
- 버퍼 채널을 사용할 때는 `HasBufferedMessage`를 확인해 초기 UI나 상태를 맞춰 줍니다.