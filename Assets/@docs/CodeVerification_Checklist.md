# ✅ 코드 검증 체크리스트
*2024년 9월 29일*

## 🔍 내가 만든 코드가 정말 작동하는지 확인하는 방법

### 📋 1단계: 코드 파일 존재 확인

```bash
# 파일 경로에서 확인
D:\Data\02_Unity\03_MB\MB\Assets\@Scripts\Test\ 폴더에:

✅ LocalNetworkTestManager.cs (있어야 함)
✅ DummyPlayer.cs (있어야 함)
✅ DummyGameManager.cs (있어야 함)
✅ MultiInstanceTestGuide.cs (있어야 함)
✅ NetworkTestManager.cs (기존, Unity Services용)
```

### 🎯 2단계: Unity에서 스크립트 컴파일 확인

#### Unity 에디터에서 확인할 것들:
1. **Console 창 열기** (Window → General → Console)
2. **컴파일 오류 없는지 확인**
   ```
   ❌ 빨간 오류 메시지 없어야 함
   ⚠️ 노란 경고는 있어도 됨
   ```
3. **Project 창에서 스크립트 아이콘 확인**
   ```
   📄 정상: C# 스크립트 아이콘
   ⚠️ 문제: 빨간 X 표시된 아이콘
   ```

### 🚀 3단계: 실제 기능 테스트

#### 기본 연결 테스트
```
✅ 예상 결과:
- GUI에 "Host 시작 성공!" 표시
- Console에 "[LocalNetworkTest] Host 시작 중..." 로그
- ParrelSync Clone에서 자동으로 Client 연결

❌ 문제 신호:
- GUI에 "Host 시작 실패!" 표시
- Console에 빨간 오류 메시지
- Clone에서 Host로 시작됨
```

#### 플레이어 스폰 테스트
```
✅ 예상 결과:
- 빨간색(Host), 파란색(Client) 플레이어 스폰
- 플레이어들이 원형으로 자동 움직임
- Console에 "[DummyPlayer] 플레이어 생성됨" 로그

❌ 문제 신호:
- 플레이어가 스폰되지 않음
- 같은 색상으로만 나타남
- 움직이지 않거나 동기화 안됨
```

### 🔧 4단계: 코드 품질 검증

#### A. API 사용 검증
```csharp
// 올바른 Unity Netcode API 사용 확인
using Unity.Netcode;                    // ✅
using Unity.Netcode.Transports.UTP;     // ✅

// 잘못된 API 사용
using UnityEngine.Networking;           // ❌ (구식 UNet)
using Mirror;                           // ❌ (다른 라이브러리)
```

#### B. NetworkBehaviour 상속 확인
```csharp
// 올바른 상속
public class DummyPlayer : NetworkBehaviour  // ✅

// 잘못된 상속
public class DummyPlayer : MonoBehaviour     // ❌
```

#### C. RPC 선언 검증
```csharp
// 올바른 RPC 선언
[Rpc(SendTo.Server)]                         // ✅
void TestServerRpc() { }

// 잘못된 RPC 선언
[ServerRpc]                                  // ❌ (구식)
void TestServerRpc() { }
```

### 🎮 5단계: 실제 동작 시나리오 테스트

#### 시나리오 1: 기본 Host/Client 연결
```
1. Unity Play 버튼 클릭
2. 3초 이내 "Host 시작 성공!" 표시 확인
3. ParrelSync Clone에서 Play 버튼 클릭
4. 5초 이내 "서버에 성공적으로 연결됨!" 확인
5. Host GUI에서 "연결된 클라이언트: 1" 확인

🎯 결과: 모두 성공하면 코드 정상 작동!
```

#### 시나리오 2: 플레이어 동기화
```
1. 기본 연결 완료 후
2. DummyPlayer 프리팹 생성 및 설정
3. Play 시 자동으로 플레이어 스폰 확인
4. Host에서 WASD 조작 시 Client에서도 움직임 확인
5. 자동 원형 움직임 동기화 확인

🎯 결과: 동기화되면 NetworkVariable 정상 작동!
```

#### 시나리오 3: RPC 통신
```
1. 플레이어 스폰 완료 후
2. Host에서 RPC 테스트 버튼 클릭
3. Console에서 Server/Client RPC 로그 확인
4. 메시지가 양방향으로 전송되는지 확인

🎯 결과: RPC 로그가 나타나면 통신 정상!
```

### 📊 6단계: 성능 및 안정성 검증

#### 메모리 사용량 확인
```
Unity Profiler (Window → Analysis → Profiler):
- Memory Usage 확인
- 메모리 누수 없는지 확인
- NetworkObject 수 모니터링
```

#### 네트워크 트래픽 확인
```
Console 로그에서 확인:
- 과도한 NetworkVariable 업데이트 없는지
- RPC 무한 루프 없는지
- 연결/해제가 정상적인지
```

### 🚨 7단계: 문제 발생 시 디버깅

#### 컴파일 오류가 있다면:
```
1. Package Manager에서 Netcode for GameObjects 설치 확인
2. Unity 버전이 2021.3+ 인지 확인
3. Assembly Definition 파일 충돌 확인
```

#### 연결이 안된다면:
```
1. 방화벽에서 7777 포트 허용
2. NetworkManager 설정 확인
3. Host가 먼저 시작되었는지 확인
```

#### 동기화가 안된다면:
```
1. NetworkObject 컴포넌트 확인
2. NetworkVariable 선언 확인
3. IsServer/IsClient 로직 확인
```

### 🎯 최종 확인: "정말 잘 만들어졌나?"

#### ✅ 성공 기준:
- [ ] ParrelSync Clone에서 자동 Client 시작
- [ ] Host/Client 연결 성공 (5초 이내)
- [ ] 플레이어 다른 색상으로 스폰
- [ ] 네트워크 동기화 정상 (위치, 회전)
- [ ] WASD 조작 시 실시간 동기화
- [ ] RPC 메시지 양방향 전송
- [ ] 연결 해제 시 정상 정리

#### 🏆 완벽한 코드라면:
```
모든 체크리스트 ✅ 완료!
Console에 오류 없음!
ParrelSync로 즉시 테스트 가능!
문서대로 5분만에 작동!
```

---

## 💡 꿀팁: 빠른 검증 방법

### 1분 컴파일 테스트:
```
Unity 열기 → Play 버튼 → Console 확인
오류 없으면 기본 코드 OK!
```

### 5분 기능 테스트:
```
QuickStart_MultiplayerTest.md 따라하기
Host/Client 연결되면 네트워크 코드 OK!
```

### 10분 완전 테스트:
```
플레이어 프리팹까지 만들어서 테스트
모든 기능 작동하면 완벽한 코드!
```

**이 체크리스트대로 하면 내 코드가 정말 작동하는지 100% 확인 가능해요! 🎯**