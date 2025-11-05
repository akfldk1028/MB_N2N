# 🎮 블록깨기 멀티플레이 설정 가이드

*작성일: 2025-10-26*

## 📋 개요

블록깨기 게임의 멀티플레이 동기화 시스템입니다.
각 플레이어는 자신의 Paddle과 Ball을 조작하며, Brick은 공유됩니다.

---

## ✅ 구현된 기능

### 1. **PhysicsBall** - 공 동기화
```csharp
// NetworkVariable로 위치/속도 동기화 (NetworkTransform 없이)
private NetworkVariable<Vector3> _syncedPosition;
private NetworkVariable<Vector2> _syncedVelocity;
private NetworkVariable<EBallState> _syncedState;
```

**동작 방식:**
- ✅ 서버: 물리 시뮬레이션 + NetworkVariable 업데이트
- ✅ 클라이언트: NetworkVariable 값으로 부드럽게 보간 (Lerp)

### 2. **PhysicsPlank** - 패들 동기화
```csharp
// Owner만 입력 처리
if (baseObject.IsOwner)
{
    ProcessKeyboardMovement(deltaTime);
}

// NetworkVariable로 위치 동기화
private NetworkVariable<Vector3> _syncedPosition;
```

**동작 방식:**
- ✅ Owner: 키보드/마우스 입력 → 위치 업데이트 → NetworkVariable 동기화
- ✅ 다른 플레이어: NetworkVariable 값으로 보간

### 3. **Brick** - 벽돌 동기화 (서버 권한)
```csharp
// 서버에서만 체력 감소
if (IsSpawned && !IsServer) return;

// NetworkVariable로 체력 동기화
private NetworkVariable<int> _syncedWave;
```

**동작 방식:**
- ✅ 서버: 충돌 감지 → 체력 감소 → NetworkVariable 업데이트 → 파괴
- ✅ 클라이언트: NetworkVariable 변경 감지 → UI 업데이트

### 4. **BrickGameMultiplayerSpawner** - 플레이어 스폰
```csharp
// 플레이어 연결 시 자동 스폰
private void OnClientConnected(ulong clientId)
{
    SpawnPlankForPlayer(clientId, playerIndex);
    SpawnBallForPlayer(clientId, playerIndex, plankObject);
}
```

**동작 방식:**
- ✅ 서버: 클라이언트 연결 감지 → Ball & Plank 생성 → Owner 지정
- ✅ 위치: 플레이어 인덱스 기반 자동 배치 (1인: 중앙, 2인: 좌우, 3인+: 균등)

---

## 🚀 설정 방법 (Inspector 없이 코드만)

### 1단계: 씬에 Spawner 추가

```csharp
// GameScene에 빈 GameObject 생성
GameObject spawner = new GameObject("BrickGameMultiplayerSpawner");
spawner.AddComponent<BrickGameMultiplayerSpawner>();
spawner.AddComponent<NetworkObject>(); // NetworkObject 컴포넌트 필수
```

**또는 수동 생성:**
1. Hierarchy에서 빈 GameObject 생성
2. 이름: "BrickGameMultiplayerSpawner"
3. Add Component → `BrickGameMultiplayerSpawner`
4. Add Component → `NetworkObject`

### 2단계: Prefab 준비

**필요한 Prefab 위치:**
```
Assets/
└─ @Resources/
   └─ GameScene/
      └─ Model/
         └─ ball.prefab  ✅ (자동 로드)
```

**Plank는 씬에서 자동 감지:**
- 씬에 `PhysicsPlank` 컴포넌트가 있으면 자동으로 복제 사용
- 없으면 `Resources/GameScene/Plank.prefab` 로드 시도

### 3단계: NetworkObject 확인

**ball.prefab에 필요한 컴포넌트:**
```
ball (GameObject)
├─ NetworkObject ✅ (수동 추가 또는 코드 자동 추가)
├─ PhysicsBall
├─ Rigidbody2D
└─ Collider2D
```

**Plank에 필요한 컴포넌트:**
```
Plank (GameObject)
├─ NetworkObject ✅ (수동 추가 또는 코드 자동 추가)
├─ PhysicsPlank
├─ Rigidbody2D
└─ Collider2D
```

---

## 🎮 테스트 방법

### ParrelSync 사용 (권장)

```bash
1. ParrelSync → Clones Manager
2. "Add new clone" 클릭
3. Clone 이름: "Client_Clone"
4. "Create" 클릭 (몇 분 소요)
5. "Open in New Editor" 클릭

테스트:
- 원본 Unity: Play → Host 시작
- Clone Unity: Play → Client 연결
```

**예상 결과:**
```
✅ Host: 패들 1개, 공 1개 스폰 (중앙)
✅ Client: 패들 1개, 공 1개 추가 스폰 (좌 또는 우)
✅ 각 플레이어는 자신의 패들만 조작 가능
✅ Brick은 공유 (한 플레이어가 부수면 모두에게 반영)
```

---

## 🔧 동기화 세부 사항

### NetworkVariable 업데이트 주기

**PhysicsBall:**
```csharp
// FixedUpdate()에서 서버가 업데이트
private void SyncPositionToClients()
{
    // 위치 차이 0.01f 이상일 때만 업데이트 (최적화)
    if (positionDiff > 0.01f)
    {
        _syncedPosition.Value = transform.position;
    }
}
```

**PhysicsPlank:**
```csharp
// Update()에서 Owner가 업데이트
private void SyncPositionToServer()
{
    // 위치 차이 0.01f 이상일 때만 업데이트
    if (positionDiff > 0.01f)
    {
        _syncedPosition.Value = transform.position;
    }
}
```

**Brick:**
```csharp
// 충돌 시 즉시 업데이트
_syncedWave.Value = wave; // 체력 변경 즉시 반영
```

### 보간 속도 (Interpolation)

```csharp
// Ball: 10f (빠른 보간)
float interpolationSpeed = 10f;
transform.position = Vector3.Lerp(current, _syncedPosition.Value, Time.deltaTime * 10f);

// Plank: 15f (더 빠른 보간 - 즉각 반응)
float interpolationSpeed = 15f;
transform.position = Vector3.Lerp(current, _syncedPosition.Value, Time.deltaTime * 15f);
```

---

## ⚠️ 주의사항

### 1. NetworkObject 필수
```
모든 동기화 오브젝트(Ball, Plank, Brick)는 NetworkObject 컴포넌트가 필요합니다.
BrickGameMultiplayerSpawner가 없으면 코드로 자동 추가합니다.
```

### 2. NetworkManager 설정 확인
```csharp
// ConnectionApproval 활성화 필수
NetworkManager.NetworkConfig.ConnectionApproval = true;

// Scene Management 활성화 권장
NetworkManager.NetworkConfig.EnableSceneManagement = true;
```

### 3. Brick 스폰 권한
```
Brick은 서버에서만 스폰해야 합니다.
BrickManager가 Brick을 생성할 때 IsServer 체크 필요:

if (IsServer || !NetworkManager.Singleton.IsSpawned)
{
    GameObject brick = Instantiate(brickPrefab);
    NetworkObject netObj = brick.GetComponent<NetworkObject>();
    netObj.Spawn(); // 서버에서만 Spawn 호출
}
```

### 4. 물리 시뮬레이션
```
- Ball 물리: 서버에서만 시뮬레이션 (클라이언트는 보간만)
- Plank 물리: Kinematic으로 설정 (충돌 감지만)
- Brick 충돌: 서버에서만 처리
```

---

## 📊 네트워크 트래픽 최적화

### 현재 최적화 방법

```csharp
// 1. 위치 변경 임계값 (0.01f)
if (Vector3.Distance(current, synced) > 0.01f)
{
    _syncedPosition.Value = current; // 필요할 때만 업데이트
}

// 2. NetworkVariable Write Permission
NetworkVariableWritePermission.Server // 또는 .Owner
// → 불필요한 권한 제한으로 트래픽 감소

// 3. NetworkVariable Read Permission
NetworkVariableReadPermission.Everyone
// → 모든 클라이언트가 읽을 수 있지만 쓰기는 제한
```

---

## 🐛 문제해결

### 문제 1: Ball/Plank가 스폰되지 않음
```
해결:
1. BrickGameMultiplayerSpawner GameObject가 씬에 있는지 확인
2. NetworkObject 컴포넌트가 Spawner에 있는지 확인
3. Console에서 "Ball 프리팹 로드 완료" 로그 확인
4. Resources/GameScene/Model/ball.prefab 존재 확인
```

### 문제 2: 다른 플레이어 패들이 조작됨
```
해결:
1. PlankManager.UpdateMovement()에 IsOwner 체크 확인
2. PhysicsPlank.Update()에서 IsOwner 확인
```

### 문제 3: Brick 체력이 동기화 안됨
```
해결:
1. Brick.cs에 NetworkVariable<int> _syncedWave 확인
2. OnNetworkSpawn()에서 _syncedWave 초기화 확인
3. HandleBallCollision()에서 IsServer 체크 확인
```

### 문제 4: Ball 위치가 끊김 (Jittering)
```
해결:
1. 보간 속도 조정: interpolationSpeed = 15f (더 부드럽게)
2. 업데이트 임계값 감소: positionDiff > 0.005f (더 자주 업데이트)
```

---

## 🎯 다음 단계 (선택)

### 1. NetworkTransform 사용 (선택)
```csharp
// 현재: 수동 NetworkVariable 동기화
// 장점: 세밀한 제어, 최적화 가능
// 단점: 코드 복잡도 증가

// 대안: NetworkTransform 컴포넌트
[RequireComponent(typeof(NetworkTransform))]
public class PhysicsBall : PhysicsObject
{
    // 자동으로 위치/회전 동기화 (코드 간소화)
}
```

### 2. ClientRpc로 이펙트 동기화
```csharp
// 벽돌 파괴 이펙트, 사운드 등
[Rpc(SendTo.NotServer)]
void PlayBrickDestroyEffectClientRpc()
{
    // 파티클, 사운드 재생
}
```

### 3. 점수 동기화
```csharp
// BrickGameManager에 NetworkVariable 추가
private NetworkVariable<int> _syncedScore = new NetworkVariable<int>();
```

---

## 📞 지원 및 문의

**로그 레벨 확인:**
```csharp
GameLogger.Info("BrickGameMultiplayerSpawner", "메시지");
GameLogger.Success("BrickGameMultiplayerSpawner", "성공");
GameLogger.Warning("BrickGameMultiplayerSpawner", "경고");
GameLogger.Error("BrickGameMultiplayerSpawner", "오류");
```

**디버깅 팁:**
1. Unity Console에서 "[BrickGameMultiplayerSpawner]" 필터링
2. NetworkManager Statistics 확인 (Window → Multiplayer → Netcode Graph)
3. ParrelSync Clone의 Console도 함께 확인

---

**Happy Multiplayer Gaming! 🎮🌐**
