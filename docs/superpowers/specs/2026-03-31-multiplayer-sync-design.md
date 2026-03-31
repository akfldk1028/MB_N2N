# 멀티플레이어 동기화 정상화 설계

## 핵심 개념

1v1 경쟁 벽돌깨기. 각 플레이어가 **독립된 벽돌깨기 게임**을 하고, 상대방 화면에 자기 게임 상태가 실시간 표시됨.

```
Host 화면:  [내 벽돌(P0)] [Territory] [상대 벽돌(P2) 보기]
Client 화면: [내 벽돌(P2)] [Territory] [상대 벽돌(P0) 보기]
```

## 현재 문제

### 1. Brick 이동 미동기화
- `ObjectPlacement.MoveDown()` → 서버에서만 코루틴 실행
- Brick 프리팹에 NetworkTransform + NetworkRigidbody2D 있지만, `SetupRigidbody`에서 `isKinematic=true` 설정 → NetworkRigidbody2D가 Kinematic body를 제대로 동기화하는지 확인 필요
- **증상**: Client에서 상대 벽돌이 내려오지 않거나 멈춰있음

### 2. Ball 위치 수동 동기화
- `PhysicsBall`이 매 프레임 `_syncedPosition` NetworkVariable을 수동 갱신
- NetworkTransform이 있으면 이중 동기화 → 충돌/대역폭 낭비
- **증상**: Client에서 공 위치가 튀거나 지연

### 3. Plank 입력 문제
- `PhysicsPlank.Update()`에서 `IsOwner && Application.isFocused` 체크
- MPPM 클론에서 Owner 판떼기의 `Application.isFocused`가 false일 수 있음
- **증상**: Client 판떼기 안 움직임

## 해결 방안: NetworkTransform/NetworkRigidbody2D 제대로 활용

### 원칙
- 서버가 물리/이동 처리 → NetworkTransform이 자동으로 Client에 전파
- 수동 NetworkVariable 위치 동기화 제거
- 각 오브젝트가 이미 가진 네트워크 컴포넌트를 신뢰

### 수정 1: Brick 이동 동기화

**파일**: `ObjectPlacement.cs`

현재 `MoveDown` 코루틴에서 `rb.MovePosition()` 사용 중 → 이것 자체는 OK. 
문제는 NetworkRigidbody2D가 Kinematic body의 MovePosition을 동기화하지 못할 수 있음.

**확인사항**:
- Brick 프리팹의 NetworkTransform `SyncPositionX`, `SyncPositionY` 활성화 여부
- NetworkRigidbody2D의 `AutoUpdateKinematicPositionAndRotation` 설정
- `MoveDown`에서 서버만 실행 (이미 수정됨) → Client는 NetworkTransform으로 수신

**수정 방향**:
- NetworkRigidbody2D 설정 확인/수정 (프리팹에서)
- 안 되면 `MoveDown`에서 `transform.position` 직접 변경으로 변경 (NetworkTransform이 이걸 잡음)
- `MoveObjectToTargetY`도 동일하게 확인

### 수정 2: Ball 동기화 정리

**파일**: `PhysicsBall.cs`

현재: 매 프레임 `_syncedPosition.Value = transform.position` + Client에서 Lerp
Ball에도 NetworkTransform + NetworkRigidbody2D 있음 → 이중 동기화.

**수정 방향**:
- `_syncedPosition` 수동 동기화 코드 제거
- NetworkTransform이 서버 Rigidbody2D 물리를 자동 전파하도록 설정 확인
- Client의 `Update()`에서 수동 Lerp 코드 제거
- Ready 상태에서 `SetBallPositionAbovePlank()`은 서버에서 transform 직접 → NetworkTransform 전파

### 수정 3: Plank 입력

**파일**: `PhysicsPlank.cs`

현재: `IsOwner && Application.isFocused` → MoveByKeyboard → ServerRpc
MPPM 클론에서 `Application.isFocused`가 문제.

**수정 방향**:
- `Application.isFocused` 체크를 MPPM 환경에서 완화 (에디터 빌드에서 제거 or MPPM 감지 시 스킵)

## 검증 기준

1. Host에서 벽돌 내려오면 → Client Sub_Camera에서도 동일 위치로 내려옴
2. Host에서 공 발사 → Client Sub_Camera에서도 공 이동 보임
3. Client에서 판떼기 움직임 → Host Sub_Camera에서도 판떼기 이동 보임
4. 양쪽 점수가 실시간 동기화
5. 4뷰 캡쳐 비교: Host Sub ≈ Client Main (동일 게임 상태)

## 수정 안 하는 것

- ObjectPlacement 구조 변경 (서버 전용 유지)
- BrickGameManager POCO 구조 변경
- 대포/Territory 시스템 (이건 별도 이슈)
- 포트 7778→7777 복원 (에디터 재시작 후)
