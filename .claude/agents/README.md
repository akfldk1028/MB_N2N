# 서브에이전트 시스템 - 사용 가이드

## 개요

이 프로젝트는 **6개의 전문 서브에이전트**로 구성되어 있으며, Claude가 자동으로 적절한 에이전트를 선택하여 작업을 위임합니다.

## 🚨 핵심 원칙: Never Speculate, Always Research

**모든 서브에이전트는 작업 전 반드시 파일을 읽어야 합니다.**

```
❌ 잘못된 방식:
"BrickGameManager에 AddScore 메서드가 있을 것 같으니 바로 수정"

✅ 올바른 방식:
1. Read BrickGameManager.cs  (코드 구조 확인)
2. Sequential Thinking으로 분석  (맥락 파악)
3. 계획 수립  (무엇을 어떻게 바꿀지)
4. 코드 수정  (실제 작업)
```

이 원칙은 2025년 Claude Code 베스트 프랙티스로, 추측으로 인한 오류를 방지합니다.

## 자동 위임 시스템

**중요**: 슬래시 커맨드가 필요 없습니다. 일반 대화로 요청하면 Claude가 자동으로 적절한 서브에이전트를 선택합니다.

```
사용자: "콤보 시스템 추가해줘"
→ game-logic 에이전트 실행
→ 1. BrickGameManager.cs 읽기
→ 2. Sequential Thinking으로 분석
→ 3. 계획 수립 후 코드 수정

사용자: "멀티플레이어 재연결 로직 구현"
→ network 에이전트 실행
→ 1. ConnectionManager.cs 읽기
→ 2. Sequential Thinking으로 분석
→ 3. 계획 수립 후 코드 수정

사용자: "점수 UI에 애니메이션 추가"
→ ui 에이전트 실행
→ 1. UI_GameScene.cs 읽기
→ 2. Sequential Thinking으로 분석
→ 3. 계획 수립 후 코드 수정
```

## 서브에이전트 목록

| 에이전트 | 담당 모듈 | 핵심 책임 |
|---------|----------|----------|
| **game-logic** | Contents/BrickGame | 게임 메커니즘, 점수/레벨, 상태 관리 |
| **network** | Network | 멀티플레이어 동기화, 로비, 연결 관리 |
| **physics** | Controllers/Object | 물리 시뮬레이션, 입력 처리, 충돌 감지 |
| **ui** | UI | 사용자 인터페이스, 바인딩, 이벤트 처리 |
| **infrastructure** | Infrastructure | 메시지 버스, 상태머신, 디자인 패턴 |
| **managers** | Managers | Service Locator, 데이터/리소스 관리 |

## 아키텍처 원칙

### 당신의 코드는 이미 완벽합니다

이 서브에이전트 시스템은 **당신이 구축한 깔끔한 아키텍처를 유지**하기 위해 만들어졌습니다:

1. **단일 책임 원칙**: 각 모듈은 하나의 책임만
2. **깨끗한 경계**: 모듈 간 직접 참조 금지
3. **이벤트 기반**: ActionBus를 통한 느슨한 결합
4. **단일 인스턴스**: Managers.cs Service Locator 패턴
5. **서버 권한**: 네트워크에서 서버가 모든 게임 로직 처리

### 모듈 간 통신 규칙

```
game-logic (게임 규칙)
  ↓ 이벤트 발행
ActionMessageBus (infrastructure)
  ├→ network (네트워크 동기화)
  ├→ ui (UI 업데이트)
  └→ physics (물리 반응 - 필요시)
```

**절대 하지 않는 것**:
- ❌ UI에서 게임 로직 직접 호출
- ❌ 게임 로직에서 UI 직접 업데이트
- ❌ 물리에서 점수 계산
- ❌ Managers에 비즈니스 로직 포함

## 각 에이전트 상세

### 1. game-logic (게임 로직)

**책임**:
- 게임 상태 관리 (Idle → Playing → GameOver)
- 점수/레벨 계산
- 난이도 조절
- 게임 규칙 구현

**파일**:
- BrickGameManager.cs
- BrickGameState.cs
- BallManager.cs, PlankManager.cs, BrickManager.cs

**통신**:
- ✅ 이벤트 발행: OnScoreChanged, OnLevelUp
- ✅ ActionBus 사용
- ❌ UI/Network/Physics 직접 참조 금지

### 2. network (네트워크)

**책임**:
- 멀티플레이어 상태 동기화
- 로비 시스템
- 연결 관리
- 플레이어 스포닝

**파일**:
- BrickGameNetworkSync.cs
- BrickGameMultiplayerSpawner.cs
- Connection*/Lobby*/Session* 파일들

**통신**:
- ✅ 게임 로직 이벤트 구독
- ✅ NetworkedMessageChannel로 전파
- ❌ 게임 규칙 계산 금지

### 3. physics (물리)

**책임**:
- 입력 처리 (마우스/키보드)
- 물리 시뮬레이션 (공/패들)
- 충돌 감지
- 반사각 계산

**파일**:
- PhysicsBall.cs
- PhysicsPlank.cs
- Brick.cs, BonusBall.cs

**통신**:
- ✅ BrickManager.UnregisterBrick() 호출
- ✅ NetworkVariable 위치 동기화
- ❌ 점수 계산 금지

### 4. ui (UI/UX)

**책임**:
- UI 바인딩 (Enum 패턴)
- 이벤트 구독 및 표시
- 팝업 관리
- 애니메이션

**파일**:
- UI_Base.cs, UI_Scene.cs, UI_Popup.cs
- UI_GameScene.cs
- UI_HeroInfoPopup.cs

**통신**:
- ✅ ActionBus 이벤트 구독
- ✅ ActionDispatcher로 입력 전달
- ❌ 게임 로직 직접 호출 금지

### 5. infrastructure (인프라)

**책임**:
- ActionMessageBus (Pub-Sub)
- MessageChannel (로컬/네트워크)
- StateMachine (FSM)
- DisposableSubscription

**파일**:
- ActionMessageBus.cs
- MessageChannel.cs, NetworkedMessageChannel.cs
- StateMachine.cs

**통신**:
- ✅ 다른 모듈에 API 제공
- ✅ 제네릭 구현 유지
- ❌ 게임 종속 코드 금지

### 6. managers (매니저)

**책임**:
- Service Locator (Managers.cs)
- 데이터 로딩 (DataManager)
- 리소스 관리 (ResourceManager)
- 오브젝트 풀링 (PoolManager)
- UI 스택 관리 (UIManager)

**파일**:
- Managers.cs (싱글톤)
- DataManager.cs, ResourceManager.cs
- PoolManager.cs, UIManager.cs

**통신**:
- ✅ 전역 서비스 제공
- ✅ 초기화 순서 관리
- ❌ 비즈니스 로직 포함 금지

## 데이터 흐름 예시

### 점수 증가 흐름
```
1. [physics] PhysicsBall이 Brick 충돌 감지
2. [physics] BrickManager.UnregisterBrick(brick) 호출
3. [game-logic] BrickGameManager.OnBrickDestroyed()
4. [game-logic] AddScore(points) - 점수 계산
5. [game-logic] OnScoreChanged?.Invoke(newScore)
6. [game-logic] ActionBus.Publish(ActionId.BrickGame_ScoreChanged)
7. [network] BrickGameNetworkSync.HandleScoreChanged() - 서버만
8. [network] NetworkedMessageChannel.Publish() → 모든 클라이언트
9. [network] OnScoreMessageReceived() - 각 클라이언트
10. [network] ActionBus.Publish() - 로컬 이벤트
11. [ui] UI_GameScene.OnScoreChanged() - UI 업데이트
```

### 버튼 클릭 흐름
```
1. [ui] User clicks Start button
2. [ui] OnStartClicked() handler
3. [ui] ActionBus.Publish(ActionId.Game_Start)
4. [game-logic] BrickGameManager subscribes, receives event
5. [game-logic] StartGame() - 게임 시작
6. [game-logic] OnGamePhaseChanged?.Invoke(GamePhase.Playing)
7. [game-logic] ActionBus.Publish(ActionId.BrickGame_PhaseChanged)
8. [ui] UI updates to show "Playing" state
```

## 사용 예시

### 좋은 요청 예시

```
"콤보 시스템 추가 (3연속 히트시 점수 2배)"
→ game-logic 에이전트가 콤보 카운터 및 배수 계산 구현

"멀티플레이어 4인 지원으로 확장"
→ network 에이전트가 플레이어 스포닝 및 카메라 설정 수정

"공 속도 증가 파워업 구현"
→ physics 에이전트가 파워업 상태 및 속도 조절 구현

"점수 증가 시 +100 애니메이션 추가"
→ ui 에이전트가 DOTween 애니메이션 구현
```

### 복합 요청 (여러 에이전트 협업)

```
"멀티볼 파워업 추가"
1. game-logic: 파워업 활성화 조건 및 지속 시간
2. physics: 공 복제 로직 및 SharedPower 사용
3. ui: 파워업 활성 UI 표시
4. network: 파워업 상태 동기화
```

## 명시적 호출 (선택사항)

자동 선택이 잘못되었다고 느끼면 명시적으로 요청 가능:

```
"Use the game-logic subagent to refactor the scoring system"
"Use the network subagent to add reconnection logic"
```

## 파일 구조

```
.claude/
└── agents/
    ├── game-logic.md      # BrickGame 로직 전문가
    ├── network.md         # 멀티플레이어 전문가
    ├── physics.md         # 물리/입력 전문가
    ├── ui.md              # UI/UX 전문가
    ├── infrastructure.md  # 인프라 패턴 전문가
    ├── managers.md        # 서비스 관리 전문가
    └── README.md          # 이 파일
```

## 핵심 설계 원칙 (유지보수성)

당신의 코드베이스가 가진 **깔끔함**을 유지하기 위한 원칙:

1. **Never Speculate Rule**: 추측 금지, 반드시 파일을 읽고 확인
2. **Research → Plan → Code**: 조사 → 계획 → 코딩 순서 엄수
3. **모듈 경계 엄수**: 각 에이전트는 자신의 파일만 수정
4. **이벤트 기반 통신**: ActionBus/NetworkedMessageChannel 사용
5. **단일 인스턴스**: Managers.cs를 통한 전역 접근만 허용
6. **서버 권한**: 게임 로직은 서버에서만 실행
7. **순수 함수**: 사이드 이펙트 최소화
8. **인터페이스 우선**: 구현보다 인터페이스 의존

## 🧠 서브에이전트의 필수 워크플로우

모든 서브에이전트는 다음 4단계를 **반드시** 따릅니다:

### Step 1: Context Discovery (맥락 파악)
```
✅ Read 관련 파일들 (추측하지 말고 읽기!)
✅ Grep/Glob으로 관련 코드 검색
✅ 의존성 파악
```

### Step 2: Sequential Thinking (순차적 분석)
```
✅ sequential-thinking MCP 도구 사용
✅ 현재 구조 이해 → 요구사항 분석 → 계획 수립
✅ 모듈 경계 검증
```

### Step 3: Boundary Verification (경계 확인)
```
✅ 내 담당 파일만 수정하는가?
✅ 다른 모듈의 책임을 침범하지 않는가?
✅ 이벤트/ActionBus를 올바르게 사용하는가?
```

### Step 4: Implementation (구현)
```
✅ 계획에 따라 코드 수정
✅ 이벤트 발행/구독
✅ 기존 패턴 따르기
```

## 트러블슈팅

### "에이전트가 잘못된 파일을 수정했어요"
→ 에이전트에게 명확히 지시: "Only modify BrickGameManager.cs, do not touch UI or Network code"

### "여러 모듈을 수정해야 하는 기능인데?"
→ Claude가 자동으로 여러 에이전트를 순차적으로 호출합니다. 전체 요구사항을 한 번에 설명하세요.

### "에이전트가 모듈 경계를 넘었어요"
→ 각 에이전트 파일의 "NEVER Touch" 섹션을 확인하고, 위반사항을 지적하세요.

## 🔧 사용 중인 도구

### Sequential Thinking MCP
각 서브에이전트는 `mcp__sequential-thinking__sequentialthinking` 도구를 사용하여:
- 단계별 문제 분해
- 반복적 사고 과정 기록
- 분기 탐색 (여러 접근 방식 시도)
- 과거 단계 수정 (revisesThought)
- 명확한 추론 과정 제공

### Context Isolation
각 서브에이전트는 **독립된 컨텍스트 윈도우**에서 작동:
- 메인 대화를 오염시키지 않음
- 관련 정보만 오케스트레이터에게 반환
- 대용량 코드베이스 탐색에 최적화

## 추가 자료

- Claude Code 공식 문서: https://code.claude.com/docs
- 서브에이전트 가이드: https://code.claude.com/docs/en/sub-agents.md
- Sequential Thinking MCP: https://github.com/langgptai/sequential-thinking-mcp
- Claude Code 베스트 프랙티스 (2025): https://www.anthropic.com/engineering/claude-code-best-practices
- 이 프로젝트의 아키텍처: Assets/@Scripts/docs/ (코드베이스 분석 참고)

## 🎯 2025 업데이트

이 서브에이전트 시스템은 2025년 Claude Code 베스트 프랙티스를 반영:
- ✅ **Never Speculate Rule** - 추측 금지, 항상 파일 읽기
- ✅ **Context Awareness** - 독립된 컨텍스트로 효율성 극대화
- ✅ **Sequential Thinking** - MCP 도구를 통한 체계적 추론
- ✅ **Research First** - 코딩 전 반드시 조사 및 계획
- ✅ **Specialized Agents** - 작업별 최적화된 도구 및 프롬프트

---

**당신의 깔끔한 아키텍처를 존중하고 유지하는 것이 이 시스템의 최우선 목표입니다.**
