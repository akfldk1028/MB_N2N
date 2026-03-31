# 멀티플레이어 동기화 정상화 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Host/Client 간 벽돌 이동, 공 위치, 판떼기 입력이 상대방 화면에 정확히 표시되도록 동기화 정상화

**Architecture:** Brick은 NetworkTransform+NetworkRigidbody2D로 위치 자동 동기화. Ball/Plank은 NetworkTransform 없으므로 기존 _syncedPosition NV 방식 유지. 서버 권한 원칙 준수.

**Tech Stack:** Unity 6, Netcode for GameObjects v2.5.1, NetworkTransform, NetworkRigidbody2D

**프리팹 현황:**
| 프리팹 | NetworkTransform | NetworkRigidbody2D | 현재 동기화 |
|--------|-----------------|-------------------|------------|
| brick | ✅ | ✅ | MovePosition (서버만) → 자동 전파 가능 |
| operatorBrick | ✅ | ✅ | 동일 |
| ball | ❌ | ❌ | _syncedPosition NV 수동 |
| plank | ❌ | ❌ | _syncedPosition NV + ServerRpc |

---

### Task 1: Brick NetworkTransform 동기화 검증 + 수정

**Files:**
- Modify: `Assets/@Scripts/Controllers/Object/ObjectPlacement.cs:609-616` (SetupRigidbody)
- Modify: `Assets/@Scripts/Controllers/Object/ObjectPlacement.cs:701-720` (MoveDown 코루틴)
- Check: `Assets/@Resources/GameScene/Model/brick.prefab`
- Check: `Assets/@Resources/GameScene/operatorBrick.prefab`

Brick 프리팹에 NetworkTransform + NetworkRigidbody2D가 있고 Rigidbody2D가 Kinematic. 서버에서 `rb.MovePosition()`하면 NetworkRigidbody2D가 자동 동기화해야 하는데, `SetupRigidbody`에서 런타임에 isKinematic을 다시 설정하면서 NetworkRigidbody2D 초기화를 방해할 수 있음.

- [ ] **Step 1: SetupRigidbody에서 불필요한 재설정 제거**

```csharp
// ObjectPlacement.cs SetupRigidbody
private void SetupRigidbody(GameObject obj)
{
    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
    if (rb == null) rb = obj.AddComponent<Rigidbody2D>();
    
    rb.gravityScale = 0;
    // isKinematic 설정 제거 — 프리팹에서 이미 Kinematic 설정됨
    // NetworkRigidbody2D가 bodyType을 관리하므로 런타임에 건드리면 안 됨
}
```

- [ ] **Step 2: MoveDown 코루틴 — transform.position 직접 변경으로 변경**

NetworkRigidbody2D가 Kinematic body의 MovePosition을 전파 못 할 경우 대비.
NetworkTransform은 `transform.position` 변경을 감지하여 동기화함.

```csharp
// ObjectPlacement.cs MoveDown
private IEnumerator MoveDown(GameObject obj, float targetY)
{
    if (obj == null) yield break;
    
    // NetworkTransform이 transform.position 변경을 감지 → 자동 동기화
    while (obj != null && obj.transform.position.y > targetY)
    {
        Vector3 pos = obj.transform.position;
        pos.y -= movingDownStep;
        
        if (pos.y <= targetY)
            pos.y = targetY;
            
        obj.transform.position = pos;
        yield return new WaitForFixedUpdate();
    }
}
```

- [ ] **Step 3: MoveObjectToTargetY도 동일하게 transform.position 사용 확인**

이 코루틴도 `SetupRigidbody` 후에 호출되므로 같은 방식으로 수정.

- [ ] **Step 4: Play 테스트 — Host에서 벽돌 내려올 때 Client에서도 보이는지**

1. 둘 다 Play → Start
2. Host Main_Camera 캡쳐 (내 벽돌)
3. Client Sub_Camera 캡쳐 (Host 벽돌 = 상대)
4. 벽돌 Y 위치가 일치하는지 비교

- [ ] **Step 5: Commit**

```bash
git add Assets/@Scripts/Controllers/Object/ObjectPlacement.cs
git commit -m "fix: Brick 이동 NetworkTransform 동기화 — transform.position 직접 변경"
```

---

### Task 2: PhysicsBall _syncedPosition 정리 + 검증

**Files:**
- Modify: `Assets/@Scripts/Controllers/Object/PhysicsBall.cs`

Ball에는 NetworkTransform이 없으므로 `_syncedPosition` NV 수동 동기화를 유지.
현재 코드가 제대로 동작하는지 검증하고 문제가 있으면 수정.

- [ ] **Step 1: PhysicsBall Update()의 서버/클라이언트 분기 확인**

```
서버: 물리 처리 → _syncedPosition.Value = transform.position
클라이언트: transform.position = Lerp(현재, _syncedPosition.Value, dt*15)
```

이 패턴이 실제 코드에 있는지 확인하고, Ready 상태에서도 동기화되는지 검증.

- [ ] **Step 2: Ready 상태에서 SetBallPositionAbovePlank 동기화 확인**

서버에서 `SetBallPositionAbovePlank()` → `_syncedPosition.Value` 갱신되는지.
Client에서 공이 판떼기 위에 따라다니는지.

- [ ] **Step 3: Play 테스트 — Host 공 발사 후 Client에서 보이는지**

Host에서 방향키로 판떼기 이동 → 공 발사 → Client Sub_Camera에서 공 이동 확인.

- [ ] **Step 4: Commit (수정 있을 경우)**

```bash
git add Assets/@Scripts/Controllers/Object/PhysicsBall.cs
git commit -m "fix: PhysicsBall _syncedPosition 동기화 검증/수정"
```

---

### Task 3: PhysicsPlank MPPM 입력 수정

**Files:**
- Modify: `Assets/@Scripts/Controllers/Object/PhysicsPlank.cs:362-390` (Update)

MPPM 클론에서 `Application.isFocused`가 false여서 판떼기 입력 안 됨.

- [ ] **Step 1: Application.isFocused 체크 완화**

```csharp
// PhysicsPlank.cs Update()
private void Update()
{
    if (IsSpawned)
    {
        if (IsOwner)
        {
            // MPPM 환경에서는 isFocused 체크 완화
            // (에디터에서 Game View가 포커스 안 돼도 입력 받음)
            #if UNITY_EDITOR
            bool canProcessInput = true; // 에디터에서는 항상 입력 처리
            #else
            bool canProcessInput = Application.isFocused;
            #endif

            if (canProcessInput)
            {
                float horizontal = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(horizontal) > 0.01f)
                {
                    MoveByKeyboard(horizontal, Time.deltaTime);
                }
            }

            SyncPositionToServer();
        }
        else
        {
            InterpolateToSyncedPosition();
        }
    }
}
```

- [ ] **Step 2: PhysicsBall의 Input 체크도 동일하게 완화**

PhysicsBall.cs Update()에서도 `Application.isFocused` 체크가 있음 (line ~184).
동일하게 에디터에서는 항상 처리.

```csharp
// PhysicsBall.cs Update() 내부 (Ready 상태 입력 처리)
#if UNITY_EDITOR
bool canProcessInput = true;
#else
bool canProcessInput = Application.isFocused;
#endif

if (CurrentState == EBallState.Ready && (!IsSpawned || IsOwner))
{
    if (canProcessInput)
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // ... 발사 처리
        }
    }
}
```

- [ ] **Step 3: Play 테스트 — Client에서 판떼기 움직이는지**

Client 에디터에서 방향키 입력 → 판떼기 이동 → Host Sub_Camera에서 보이는지.

- [ ] **Step 4: Commit**

```bash
git add Assets/@Scripts/Controllers/Object/PhysicsPlank.cs Assets/@Scripts/Controllers/Object/PhysicsBall.cs
git commit -m "fix: MPPM 에디터 Application.isFocused 완화 — 판떼기/공 입력"
```

---

### Task 4: 통합 테스트 — 4뷰 캡쳐 비교

**Files:** 없음 (테스트만)

- [ ] **Step 1: 둘 다 Play → Start → 50초 대기**
- [ ] **Step 2: Host Main_Camera + Sub_Camera 캡쳐**
- [ ] **Step 3: Client Main_Camera + Sub_Camera 캡쳐**
- [ ] **Step 4: 비교 확인**

| 검증 항목 | 기대 결과 |
|-----------|----------|
| Host Main ↔ Client Sub | 같은 벽돌 배치+이동 |
| Client Main ↔ Host Sub | 같은 벽돌 배치+이동 |
| 공 위치 | 양쪽에서 보임 |
| 판떼기 | 양쪽에서 움직임 |
| 벽돌 wave 텍스트 | 양쪽 일치 |

- [ ] **Step 5: Commit (최종)**

```bash
git commit -m "test: 멀티플레이어 동기화 통합 테스트 통과"
```
