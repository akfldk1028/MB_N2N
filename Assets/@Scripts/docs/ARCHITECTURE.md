# MB_N2N 프로젝트 아키텍처 문서

> 📅 최종 업데이트: 2025-11-05
> 🎮 프로젝트: MB_N2N (Multiplayer Brick Breaking Game)

---

## 📋 목차

1. [프로젝트 개요](#프로젝트-개요)
2. [전체 모듈 구조](#전체-모듈-구조)
3. [핵심 디자인 패턴](#핵심-디자인-패턴)
4. [모듈별 상세 설명](#모듈별-상세-설명)
5. [의존성 관계](#의존성-관계)
6. [멀티플레이어 아키텍처](#멀티플레이어-아키텍처)
7. [유지보수 가이드라인](#유지보수-가이드라인)

---

## 프로젝트 개요

### 핵심 정보
- **프로젝트명**: MB_N2N (Multiplayer Brick Breaking Game)
- **장르**: 멀티플레이어 벽돌깨기 게임
- **네트워킹**: Unity Netcode for GameObjects (NGO) + Unity Gaming Services
- **핵심 패턴**: Service Locator + Event-Driven Architecture

### 기술 스택
- Unity Engine
- Unity Netcode for GameObjects (NGO)
- Unity Gaming Services (Authentication, Lobby/Sessions)
- Addressables (리소스 관리)
- C# .NET

---

## 전체 모듈 구조

```
Assets/@Scripts/
├── Managers/          ⭐ 핵심 서비스 허브 (Service Locator)
├── Infrastructure/    🔧 이벤트 & 상태 관리 인프라
├── Network/          🌐 멀티플레이어 시스템
├── Controllers/      🎮 게임 엔티티 컨트롤러
├── UI/               🖼️ UI 프레임워크
├── Data/             📊 게임 컨텐츠 데이터
├── Contents/         🎯 게임별 로직
├── Scenes/           🎬 씬 컨트롤러
├── Utils/            🛠️ 유틸리티 & 정의
├── Lobby/            🏛️ 로비 시스템 (레거시/검토 필요)
├── Test/             🧪 테스트 인프라
├── Editor/           ✏️ 에디터 도구
└── docs/             📚 문서
```

---

## 핵심 디자인 패턴

### 1. Service Locator Pattern ⭐ (Primary)
**위치**: `Managers.cs`

**특징**:
- 모든 게임 시스템에 대한 중앙화된 접근점
- Singleton 기반
- DontDestroyOnLoad로 씬 전환 간 유지

**사용 예시**:
```csharp
// 리소스 로드
Managers.Resource.Instantiate("Prefabs/Player");

// UI 표시
Managers.UI.ShowPopupUI<UI_HeroInfoPopup>();

// 데이터 접근
var heroData = Managers.Data.HeroDic[heroId];

// 이벤트 구독
Managers.Subscribe(ActionId.System_Update, OnUpdate);
```

### 2. Observer Pattern (Event-Driven)
**위치**: `Infrastructure/MessageSystem/`

**구성요소**:
- `ActionMessageBus` - Pub/Sub 이벤트 버스
- `MessageChannel` - 제네릭 메시지 채널
- `NetworkedMessageChannel` - 네트워크 인식 메시징

**사용 예시**:
```csharp
// 이벤트 구독
Managers.Subscribe(ActionId.Gameplay_StartSession, OnGameStart);

// 이벤트 발행
Managers.PublishAction(ActionId.Score_Updated, scoreData);

// 구독 해제
Managers.Unsubscribe(ActionId.Gameplay_StartSession, OnGameStart);
```

### 3. State Pattern
**위치**: `Infrastructure/StateMachine/` 및 `Network/ConnectionManagement/`

**사용처**:
- 게임 상태 관리 (InGame, Lobby, etc.)
- 네트워크 연결 상태 (Offline, Connecting, Connected, Hosting)
- Creature AI 상태

**사용 예시**:
```csharp
// 상태 등록
Managers.RegisterState(new GameplayState());

// 상태 전환
Managers.SetState(StateId.InGame);
```

### 4. Command Pattern
**위치**: `Infrastructure/MessageSystem/IAction.cs`

**특징**:
- 실행 가능한 액션을 객체로 캡슐화
- ActionDispatcher를 통한 커맨드 처리

### 5. Object Pool Pattern
**위치**: `Managers/Core/PoolManager.cs`

**사용 예시**:
```csharp
// 풀에서 가져오기
var obj = Managers.Pool.Pop(original);

// 풀로 반환
Managers.Pool.Push(poolable);
```

### 6. Facade Pattern
**위치**: `Network/Lobbies/LobbyServiceFacadeEx.cs`

**목적**: Unity Gaming Services의 복잡한 API를 단순화

### 7. Template Method Pattern
**위치**: `Scenes/BaseScene.cs`, `UI/UI_Base.cs`

**특징**: 공통 로직은 기본 클래스에서, 세부 구현은 서브클래스에서

---

## 모듈별 상세 설명

### 1. Managers (핵심 서비스 허브) ⭐

#### 📁 구조
```
Managers/
├── Managers.cs              # 메인 Service Locator
├── Core/                    # 핵심 시스템
│   ├── DataManager.cs       # 게임 데이터 관리
│   ├── ResourceManager.cs   # Addressables 기반 에셋 로딩
│   ├── UIManager.cs         # UI 라이프사이클
│   ├── PoolManager.cs       # 오브젝트 풀링
│   └── SceneManagerEx.cs    # 씬 관리
└── Contents/                # 게임 로직
    ├── GameManager.cs       # 게임 상태
    ├── ObjectManager.cs     # 런타임 오브젝트 생성
    ├── MapManager.cs        # 맵/레벨 관리
    └── BrickGameInitializer.cs
```

#### 🔑 핵심 역할
- 모든 게임 시스템의 진입점
- 초기화 순서 관리
- Unity Services 연동
- NetworkManager 설정

#### 📝 유지보수 규칙
1. **새 매니저 추가 시**: `Managers.cs`에 프로퍼티 추가 및 `Init()` 순서 고려
2. **초기화 순서 중요**: 의존성 있는 매니저는 나중에 초기화
3. **DontDestroyOnLoad**: 씬 전환 시 유지되어야 함
4. **Singleton 패턴 유지**: 오직 하나의 인스턴스만 존재

---

### 2. Infrastructure (인프라 계층) 🔧

#### 📁 구조
```
Infrastructure/
├── MessageSystem/           # 메시징 시스템
│   ├── ActionMessageBus.cs
│   ├── ActionDispatcher.cs
│   ├── MessageChannel.cs
│   └── NetworkedMessageChannel.cs
├── StateMachine/           # 상태 머신
│   ├── StateMachine.cs
│   ├── IState.cs
│   └── StateId.cs
└── Utils/
    ├── NetworkGuid.cs
    └── BufferedMessageChannel.cs
```

#### 🔑 핵심 역할
- 게임 전체의 이벤트 기반 통신 제공
- 상태 관리 프레임워크 제공
- 모듈 간 느슨한 결합 유지

#### 📝 유지보수 규칙
1. **새 이벤트 추가 시**: `ActionId` enum에 추가
2. **메시지 구독**: 사용 후 반드시 구독 해제 (`Unsubscribe`)
3. **상태 추가**: `StateId` enum 및 `IState` 구현 클래스 생성
4. **네트워크 메시지**: `NetworkedMessageChannel` 사용 시 동기화 고려

---

### 3. Network (멀티플레이어 시스템) 🌐

#### 📁 구조
```
Network/
├── ConnectionManagement/     # 연결 관리
│   ├── ConnectionManagerEx.cs
│   └── States/              # 연결 상태들
│       ├── OfflineStateEx.cs
│       ├── ClientConnectingStateEx.cs
│       ├── ClientConnectedStateEx.cs
│       ├── HostingStateEx.cs
│       └── ClientReconnectingStateEx.cs
├── Lobbies/                 # 로비 시스템
│   ├── LobbyServiceFacadeEx.cs
│   ├── LocalLobbyEx.cs
│   └── LocalLobbyUserEx.cs
├── Session/                 # 세션 관리
│   ├── SessionManagerEx.cs
│   └── SessionPlayerDataEx.cs
├── Auth/                    # 인증
│   └── AuthManager.cs
└── Common/                  # 공통 유틸
    ├── GameModeService.cs
    ├── ClientPrefs.cs
    ├── UpdateRunnerEx.cs
    └── RateLimitCooldown.cs
```

#### 🔑 핵심 역할
- Unity Netcode for GameObjects 통합
- Unity Gaming Services (Lobby/Sessions) 연동
- 연결 상태 관리 및 재연결 처리
- 세션 데이터 지속성

#### 📝 유지보수 규칙
1. **연결 흐름**: State Pattern을 따름 - 상태 전환 시 적절한 State 클래스 사용
2. **재연결 처리**: `SessionManager`가 플레이어 데이터 보존 담당
3. **로비 API**: `LobbyServiceFacadeEx`를 통해서만 접근 (직접 호출 금지)
4. **Rate Limiting**: Unity Services API 호출 시 `RateLimitCooldown` 사용 필수
5. **Network 동기화**: `ServerAnimationHandler`, `ServerCharacterMovement` 사용
6. **Connection Approval**: `ConnectionManagerEx`에서 플레이어 검증

---

### 4. Controllers (게임 엔티티) 🎮

#### 📁 구조
```
Controllers/
├── BaseObject.cs            # 모든 게임 오브젝트 기본
├── Creature/               # AI 기반 엔티티
│   ├── Creature.cs         # 추상 크리처
│   ├── Hero.cs             # 플레이어 캐릭터
│   └── Monster.cs          # 적
├── Object/                 # 물리/게임 오브젝트
│   ├── BrickGameManager.cs         # 벽돌게임 로직 ⭐
│   ├── PhysicsObject.cs
│   ├── PhysicsBall.cs
│   ├── PhysicsPlank.cs
│   ├── BricksWave.cs
│   ├── ServerAnimationHandler.cs   # 네트워크 애니메이션
│   └── ServerCharacterMovement.cs  # 네트워크 이동
└── CameraController.cs
```

#### 🔑 핵심 역할
- 게임 내 모든 엔티티의 동작 정의
- 물리 시뮬레이션
- 네트워크 동기화

#### 📝 유지보수 규칙
1. **계층 구조 유지**: `BaseObject` → `Creature` or `Object` 상속
2. **네트워크 오브젝트**: NetworkObject 컴포넌트 필요 시 Server/Client 동기화 고려
3. **물리 계산**: Server Authority 원칙 준수
4. **애니메이션 동기화**: `ServerAnimationHandler` 사용
5. **이동 동기화**: `ServerCharacterMovement` 사용

---

### 5. UI (사용자 인터페이스) 🖼️

#### 📁 구조
```
UI/
├── UI_Base.cs              # UI 기본 클래스
├── Scene/                  # 씬 UI (항상 표시)
│   ├── UI_Scene.cs
│   ├── UI_GameScene.cs
│   └── UI_StartUpScene.cs
├── Popup/                  # 팝업 UI (스택 관리)
│   ├── UI_Popup.cs
│   └── UI_HeroInfoPopup.cs
└── SubItem/               # UI 서브 아이템
    └── UI_EventHandler.cs  # 이벤트 핸들링
```

#### 🔑 핵심 역할
- Enum 기반 컴포넌트 바인딩 시스템
- 팝업 스택 관리
- 자동 Sorting Order 관리

#### 사용 예시
```csharp
public class UI_GameScene : UI_Scene
{
    enum Buttons { StartButton, QuitButton }
    enum Texts { ScoreText, LevelText }

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.StartButton).onClick.AddListener(OnStart);
        GetText((int)Texts.ScoreText).text = "Score: 0";
    }
}

// 사용
Managers.UI.ShowSceneUI<UI_GameScene>();
Managers.UI.ShowPopupUI<UI_HeroInfoPopup>();
Managers.UI.ClosePopupUI();
```

#### 📝 유지보수 규칙
1. **Enum 바인딩 필수**: UI 컴포넌트는 Enum으로 관리
2. **UI 계층**: Scene UI (하나만) vs Popup UI (스택)
3. **이벤트 등록**: `Init()`에서 이벤트 핸들러 등록
4. **리소스 해제**: `ClosePopupUI()` 호출 시 자동으로 Destroy
5. **Sorting Order**: UIManager가 자동 관리 (수동 변경 금지)

---

### 6. Data (게임 데이터) 📊

#### 📁 구조
```
Data/
└── Data.Contents.cs        # 모든 게임 데이터 정의
    ├── CreatureData
    │   ├── MonsterData
    │   └── HeroData
    ├── SkillData
    ├── ProjectileData
    ├── ItemData
    ├── DropTableData
    └── TextData
```

#### 🔑 핵심 역할
- 게임 컨텐츠 데이터 구조 정의
- JSON 파싱 및 Dictionary 변환
- 데이터 기반 설계(Data-Driven Design)

#### 사용 예시
```csharp
// 데이터 로드 (초기화 시 한 번)
Managers.Data.Init();

// 데이터 접근
HeroData heroData = Managers.Data.HeroDic[heroId];
MonsterData monsterData = Managers.Data.MonsterDic[monsterId];
SkillData skillData = Managers.Data.SkillDic[skillId];
```

#### 📝 유지보수 규칙
1. **JSON 파일 위치**: `Resources/Data/*.json`
2. **새 데이터 타입 추가 시**:
   - `Data.Contents.cs`에 클래스 정의
   - `ILoader<Key, Value>` 구현
   - `DataManager.cs`에 Dictionary 프로퍼티 추가
3. **데이터 수정**: JSON 파일 수정 후 Unity 재시작 또는 런타임 Reload
4. **ID 관리**: 모든 데이터는 고유 ID를 가져야 함

---

### 7. Contents (게임 로직) 🎯

#### 📁 구조
```
Contents/
├── Game/                   # 게임 메카닉
│   ├── Cannon.cs
│   ├── CannonBullet.cs
│   ├── ColorfulCubeGrid.cs
│   ├── ReleaseGameManager.cs
│   └── UILayoutManager.cs
└── Stat/                   # 스탯 시스템
    ├── CreatureStat.cs
    └── StatModifier.cs
```

#### 🔑 핵심 역할
- 게임 특화 로직 구현
- 스탯 시스템 및 수정자 패턴

#### 📝 유지보수 규칙
1. **게임별 로직 분리**: 범용 로직은 Controllers, 특화 로직은 Contents
2. **스탯 수정**: StatModifier를 통해 임시 스탯 변경

---

### 8. Scenes (씬 컨트롤러) 🎬

#### 📁 구조
```
Scenes/
├── BaseScene.cs           # 추상 씬 기본
├── StartUpScene.cs        # 초기 로딩 씬
└── GameScene.cs           # 메인 게임 씬
```

#### 🔑 핵심 역할
- 씬 레벨 초기화 및 정리
- Managers 초기화 트리거
- 씬별 UI 표시

#### 사용 예시
```csharp
public class GameScene : BaseScene
{
    public override void Clear()
    {
        // 씬 종료 시 정리 로직
    }

    protected override void Init()
    {
        base.Init();
        SceneType = EScene.GameScene;

        // 게임 씬 초기화
        Managers.UI.ShowSceneUI<UI_GameScene>();
    }
}
```

#### 📝 유지보수 규칙
1. **템플릿 메서드**: `Init()` override 시 `base.Init()` 호출 필수
2. **SceneType 설정**: 각 씬은 고유한 EScene enum 값 설정
3. **정리 로직**: `Clear()` 메서드에 씬 종료 시 정리 로직 구현
4. **씬 로드**: `Managers.Scene.LoadScene()` 사용

---

### 9. Utils (유틸리티) 🛠️

#### 📁 구조
```
Utils/
├── Define.cs              # Enum 및 상수 정의
├── Util.cs                # 헬퍼 메서드
├── Extension.cs           # 확장 메서드
├── InitBase.cs            # 초기화 인터페이스
└── GameLogger.cs          # 커스텀 로깅
```

#### 🔑 핵심 역할
- 프로젝트 전역 상수 및 Enum 정의
- 공통 유틸리티 함수
- C# 확장 메서드

#### 주요 Enum
```csharp
EScene          // 씬 타입
EUIEvent        // UI 이벤트 타입
EObjectType     // 오브젝트 타입
ECreatureState  // 크리처 상태
EItemType       // 아이템 타입
EEffectType     // 이펙트 타입
ELayer          // Unity Layer
```

#### 📝 유지보수 규칙
1. **새 Enum 추가**: `Define.cs`에 추가
2. **공통 함수**: 재사용 가능한 함수는 `Util.cs`에 static 메서드로 추가
3. **확장 메서드**: 특정 타입 확장은 `Extension.cs`에 추가
4. **로깅**: Debug.Log 대신 `GameLogger` 사용 권장

---

### 10. Test (테스트) 🧪

#### 📁 구조
```
Test/
├── NetworkTestManager.cs
├── NetworkIntegrationTestManager.cs
├── LocalNetworkTestManager.cs
├── MultiInstanceTestGuide.cs
├── DummyPlayer.cs
└── DummyGameManager.cs
```

#### 🔑 핵심 역할
- 네트워크 기능 테스트
- 멀티플레이어 시뮬레이션
- 로컬 멀티 인스턴스 테스트

---

## 의존성 관계

### 의존성 다이어그램
```
                    [Managers]
                   (전역 Service Locator)
                        ↓
        +---------------+---------------+
        ↓               ↓               ↓
   [Infrastructure] [Network]    [Core Systems]
   (메시징/상태)      (NGO)      (Data/Resource/UI)
        ↓               ↓               ↓
        +---------------+---------------+
                        ↓
                [Controllers]
             (게임 엔티티 로직)
                        ↓
                   [Contents]
              (게임 특화 로직)
                        ↓
                    [Scenes]
                 (씬 초기화)
```

### 의존성 규칙
1. **상위 → 하위 의존만 허용**
2. **Managers는 모든 모듈에 접근 가능**
3. **Controllers는 Managers를 통해서만 다른 시스템 접근**
4. **UI는 Controllers를 직접 참조하지 않음** (이벤트로 통신)
5. **Data는 의존성 없음** (순수 데이터 구조)

---

## 멀티플레이어 아키텍처

### 네트워크 흐름

```
1. [초기화]
   Managers.cs
   └─> Unity Services 초기화 (Authentication, Lobby)
   └─> NetworkManager 설정 (TransportType, ConnectionData)
   └─> ConnectionManager 초기화

2. [로비 생성/참가]
   LobbyServiceFacadeEx
   └─> TryCreateSessionAsync() 또는 TryQuickJoinSessionAsync()
   └─> 세션 데이터 동기화 (GameMode, MaxPlayers, etc.)
   └─> LocalLobby 상태 업데이트

3. [연결 시작]
   ConnectionManagerEx
   └─> State 전환: Offline → ClientConnecting
   └─> NetworkManager.StartClient() 또는 StartHost()
   └─> Connection Approval 처리

4. [세션 관리]
   SessionManagerEx
   └─> 플레이어 데이터 저장 (재연결용)
   └─> IsPersistentSession 플래그 관리
   └─> 중복 연결 방지

5. [게임플레이]
   BrickGameManager + Network Sync
   └─> ServerAnimationHandler (애니메이션 동기화)
   └─> ServerCharacterMovement (이동 동기화)
   └─> NetworkVariable/RPC로 점수/상태 동기화

6. [재연결]
   ClientReconnectingStateEx
   └─> SessionManager에서 이전 세션 데이터 복구
   └─> 자동 재연결 시도
   └─> 상태 복원
```

### 네트워크 동기화 패턴

#### 1. Server Authority (권장)
```csharp
public class MyNetworkObject : NetworkBehaviour
{
    private NetworkVariable<int> score = new NetworkVariable<int>();

    [ServerRpc]
    public void UpdateScoreServerRpc(int newScore)
    {
        // 서버에서만 실행
        score.Value = newScore;
    }
}
```

#### 2. ClientRpc (서버 → 모든 클라이언트)
```csharp
[ClientRpc]
public void ShowEffectClientRpc(Vector3 position)
{
    // 모든 클라이언트에서 실행
    SpawnEffect(position);
}
```

#### 3. NetworkVariable (자동 동기화)
```csharp
private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();

private void Update()
{
    if (IsServer)
    {
        position.Value = transform.position;
    }
}
```

---

## 유지보수 가이드라인

### 🚨 절대 규칙 (DO NOT)

1. **❌ Managers를 직접 수정하지 말 것**
   - 새 매니저 추가 시에만 수정
   - 기존 매니저 로직 변경 금지

2. **❌ Service Locator 패턴 우회 금지**
   - 항상 `Managers.*`를 통해 접근
   - Singleton 직접 접근 금지

3. **❌ 네트워크 동기화 없이 게임 상태 변경 금지**
   - Server Authority 원칙 준수
   - 클라이언트에서 직접 게임 상태 변경 금지

4. **❌ 이벤트 구독 후 해제 안 하면 메모리 누수**
   - OnDestroy()에서 반드시 Unsubscribe
   - MessageChannel 사용 시 Dispose 호출

5. **❌ Unity API 직접 사용 최소화**
   - SceneManager → Managers.Scene
   - Resources.Load → Managers.Resource
   - Instantiate/Destroy → Managers.Pool

### ✅ 권장 사항 (DO)

1. **✅ 새 기능 추가 시 이벤트 사용**
   ```csharp
   // ❌ 나쁜 예: 직접 호출
   FindObjectOfType<UIManager>().UpdateScore(score);

   // ✅ 좋은 예: 이벤트 발행
   Managers.PublishAction(ActionId.Score_Updated, score);
   ```

2. **✅ 오브젝트 생성/삭제는 풀링 사용**
   ```csharp
   // ❌ 나쁜 예
   var obj = Instantiate(prefab);
   Destroy(obj);

   // ✅ 좋은 예
   var obj = Managers.Pool.Pop(original);
   Managers.Pool.Push(poolable);
   ```

3. **✅ 데이터는 Data 모듈에서만 로드**
   ```csharp
   // ❌ 나쁜 예
   var json = Resources.Load<TextAsset>("Data/Heroes");

   // ✅ 좋은 예
   var heroData = Managers.Data.HeroDic[heroId];
   ```

4. **✅ 네트워크 오브젝트는 Server Authority**
   ```csharp
   // ✅ 서버에서만 상태 변경
   if (IsServer)
   {
       health.Value -= damage;
   }

   // 클라이언트는 ServerRpc로 요청
   [ServerRpc]
   void TakeDamageServerRpc(int damage) { ... }
   ```

5. **✅ 코루틴 대신 UpdateRunner 사용**
   ```csharp
   // ❌ 나쁜 예
   StartCoroutine(WaitAndExecute());

   // ✅ 좋은 예
   Managers.UpdateRunner.StartCoroutine(WaitAndExecute());
   ```

### 🔧 문제 해결 가이드

#### 네트워크 연결 실패
1. `ConnectionManagerEx` 로그 확인
2. Unity Gaming Services 인증 상태 확인
3. `LobbyServiceFacadeEx` 세션 상태 확인
4. Rate Limit 초과 여부 확인

#### 재연결 실패
1. `SessionManager`에 세션 데이터 저장 여부 확인
2. `IsPersistentSession` 플래그 확인
3. `ClientPrefs`에 GUID 저장 여부 확인

#### UI가 표시되지 않음
1. `Managers.UI` 초기화 여부 확인
2. Canvas Sorting Order 확인 (UIManager가 자동 관리)
3. Enum 바인딩이 올바른지 확인

#### 오브젝트 생성/삭제 시 성능 문제
1. `PoolManager` 사용 여부 확인
2. Addressables로 에셋 로드 확인
3. 불필요한 Instantiate/Destroy 제거

### 📝 코드 리뷰 체크리스트

- [ ] Managers를 통해 다른 시스템에 접근하는가?
- [ ] 이벤트 구독 시 해제 코드가 있는가?
- [ ] 네트워크 동기화가 필요한 경우 Server Authority를 따르는가?
- [ ] 오브젝트 풀링을 사용하는가?
- [ ] 새 데이터 타입 추가 시 DataManager에 등록했는가?
- [ ] UI 바인딩 시 Enum을 사용하는가?
- [ ] 씬 전환 시 정리 로직이 있는가?

---

## 확장 가이드

### 새 매니저 추가하기

1. **매니저 클래스 생성**
   ```csharp
   public class MyNewManager
   {
       public void Init()
       {
           // 초기화 로직
       }

       public void Clear()
       {
           // 정리 로직
       }
   }
   ```

2. **Managers.cs에 등록**
   ```csharp
   public class Managers : MonoBehaviour
   {
       private static MyNewManager s_myNew = new MyNewManager();
       public static MyNewManager MyNew { get { return s_myNew; } }

       void Start()
       {
           // 초기화 순서에 추가
           s_myNew.Init();
       }
   }
   ```

### 새 UI 추가하기

1. **UI 클래스 생성**
   ```csharp
   public class UI_MyPopup : UI_Popup
   {
       enum Buttons { ConfirmButton, CancelButton }
       enum Texts { TitleText, MessageText }

       public override void Init()
       {
           base.Init();

           Bind<Button>(typeof(Buttons));
           Bind<TMP_Text>(typeof(Texts));

           GetButton((int)Buttons.ConfirmButton).onClick.AddListener(OnConfirm);
           GetButton((int)Buttons.CancelButton).onClick.AddListener(OnCancel);
       }

       void OnConfirm() { ... }
       void OnCancel() { ClosePopupUI(); }
   }
   ```

2. **UI 표시**
   ```csharp
   Managers.UI.ShowPopupUI<UI_MyPopup>();
   ```

### 새 게임 데이터 추가하기

1. **Data.Contents.cs에 클래스 정의**
   ```csharp
   [Serializable]
   public class WeaponData
   {
       public int DataId;
       public string Name;
       public int Damage;
   }

   [Serializable]
   public class WeaponDataLoader : ILoader<int, WeaponData>
   {
       public List<WeaponData> weapons = new List<WeaponData>();

       public Dictionary<int, WeaponData> MakeDict()
       {
           Dictionary<int, WeaponData> dict = new Dictionary<int, WeaponData>();
           foreach (WeaponData weapon in weapons)
               dict.Add(weapon.DataId, weapon);
           return dict;
       }
   }
   ```

2. **DataManager.cs에 등록**
   ```csharp
   public Dictionary<int, WeaponData> WeaponDic { get; private set; } = new Dictionary<int, WeaponData>();

   public void Init()
   {
       WeaponDic = LoadJson<WeaponDataLoader, int, WeaponData>("WeaponData").MakeDict();
   }
   ```

3. **JSON 파일 생성**: `Resources/Data/WeaponData.json`
   ```json
   {
       "weapons": [
           {
               "DataId": 1,
               "Name": "Sword",
               "Damage": 10
           }
       ]
   }
   ```

---

## 참고 자료

- [Unity Netcode for GameObjects 공식 문서](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity Gaming Services 공식 문서](https://docs.unity.com/ugs/en-us/manual/overview/manual/unity-gaming-services-home)
- [Addressables 시스템 가이드](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- 프로젝트 내 다른 문서: `GAME_MODE_SWITCH.md`

---

## 변경 이력

| 날짜 | 버전 | 변경 내용 |
|------|------|----------|
| 2025-11-05 | 1.0 | 초기 문서 작성 |

---

**📌 이 문서는 프로젝트의 단일 진실 공급원(Single Source of Truth)입니다. 아키텍처 변경 시 반드시 이 문서를 업데이트하세요.**
