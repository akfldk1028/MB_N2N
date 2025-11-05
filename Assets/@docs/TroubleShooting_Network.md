# 🔧 네트워크 테스트 문제해결 가이드
*2024년 9월 29일*

## 🚨 자주 발생하는 문제들

### 1. ParrelSync 설치 실패

#### ❌ 문제: Git URL 추가 시 오류
```
Error: Unable to add package from git URL
```

#### ✅ 해결방법:
1. **Unity 버전 확인**
   - Unity 2021.3 이상 필요
   - Unity Hub에서 버전 업데이트

2. **Git 설치 확인**
   ```bash
   # 명령 프롬프트에서 확인
   git --version
   ```
   - Git이 없으면 https://git-scm.com 에서 설치

3. **Package Manager 재시작**
   - Unity 재시작 후 다시 시도

4. **대안: 수동 다운로드**
   ```
   https://github.com/VeriorPies/ParrelSync/releases
   → Download ZIP → Assets 폴더에 압축 해제
   ```

---

### 2. 네트워크 연결 실패

#### ❌ 문제: Client가 Host에 연결 안됨
```
[LocalNetworkTest] Client 시작 실패!
```

#### ✅ 해결방법:
1. **Host 먼저 시작 확인**
   - Host GUI에서 "Host 시작 성공!" 메시지 확인
   - Host가 완전히 준비된 후 Client 시작

2. **방화벽 설정**
   ```
   Windows 방화벽:
   제어판 → 시스템 및 보안 → Windows Defender 방화벽
   → 고급 설정 → 인바운드 규칙 → 새 규칙
   → 포트 → TCP → 7777 → 허용
   ```

3. **IP 주소 확인**
   ```
   LocalNetworkTestManager에서:
   Server IP: 127.0.0.1 (로컬 테스트)
   Port: 7777 (기본값)
   ```

4. **Unity Transport 설정 확인**
   ```
   NetworkManager → UnityTransport:
   - Protocol Type: UnityTransport
   - Connection Data 설정 확인
   ```

---

### 3. 컴파일 오류

#### ❌ 문제: NetworkManager 타입을 찾을 수 없음
```
error CS0246: The type or namespace name 'NetworkManager' could not be found
```

#### ✅ 해결방법:
1. **패키지 설치 확인**
   ```
   Package Manager에서 확인:
   - Netcode for GameObjects (설치됨)
   - Unity Transport (자동 설치됨)
   ```

2. **using 문 확인**
   ```csharp
   using Unity.Netcode;
   using Unity.Netcode.Transports.UTP;
   ```

3. **Assembly Definition 문제**
   - Scripts 폴더에 .asmdef 파일이 있다면
   - Netcode 어셈블리 참조 추가 필요

---

### 4. 플레이어 스폰 실패

#### ❌ 문제: 연결됐지만 플레이어가 나타나지 않음
```
[DummyGameManager] 플레이어 스폰 실패
```

#### ✅ 해결방법:
1. **NetworkObject 컴포넌트 확인**
   ```
   DummyPlayer 프리팹에서:
   - NetworkObject 컴포넌트 추가됨
   - "Spawn With Scene" 체크 해제
   ```

2. **NetworkPrefabs 등록**
   ```
   NetworkManager → NetworkPrefabs List:
   DummyPlayer 프리팹이 목록에 있는지 확인
   ```

3. **DummyGameManager 설정**
   ```
   - NetworkObject 컴포넌트 추가
   - "Spawn With Scene" 체크
   - Player Prefab 필드에 프리팹 할당
   ```

---

### 5. ParrelSync Clone 인식 실패

#### ❌ 문제: Clone에서 Host로 시작됨
```
Clone이 Client가 아닌 Host로 시작
```

#### ✅ 해결방법:
1. **ParrelSync 설치 확인**
   ```csharp
   // 콘솔에서 확인
   [LocalNetworkTest] ParrelSync 감지 실패 (정상적임)
   ```

2. **수동 Client 시작**
   ```
   Clone Unity에서:
   GUI → "Client로 시작" 버튼 클릭
   ```

3. **Clone 확인**
   ```
   Clone Unity 창 제목에 [CLONE] 표시 확인
   ```

---

### 6. 성능 문제

#### ❌ 문제: 네트워크 동기화 지연
```
플레이어 움직임이 끊기거나 지연됨
```

#### ✅ 해결방법:
1. **Tick Rate 조정**
   ```
   NetworkManager → Network Config:
   - Tick Rate: 30 (기본값)
   - 높일수록 부드럽지만 네트워크 부하 증가
   ```

2. **보간 설정 확인**
   ```csharp
   // DummyPlayer.cs에서 확인
   InterpolateToNetworkState() 메서드 동작
   ```

3. **로컬 테스트 환경**
   ```
   로컬 테스트에서는 지연이 거의 없어야 함
   원격 테스트 시에만 지연 발생 정상
   ```

---

### 7. RPC 통신 실패

#### ❌ 문제: RPC 메시지가 전송되지 않음
```
Server/Client RPC 로그가 나타나지 않음
```

#### ✅ 해결방법:
1. **RPC 권한 확인**
   ```csharp
   [Rpc(SendTo.Server)]    // 서버로만
   [Rpc(SendTo.NotServer)] // 클라이언트들로만
   [Rpc(SendTo.Owner)]     // 소유자에게만
   ```

2. **NetworkBehaviour 상속 확인**
   ```csharp
   public class DummyPlayer : NetworkBehaviour
   ```

3. **NetworkObject 스폰 확인**
   ```
   RPC는 스폰된 NetworkObject에서만 작동
   ```

---

## 🔍 디버깅 도구

### 1. 로그 확인
```csharp
// LocalNetworkTestManager에서
enableVerboseLogging = true

// 콘솔에서 확인할 로그들:
[LocalNetworkTest] Host 시작 중...
[LocalNetworkTest] 클라이언트 연결됨: ClientID 1
[DummyPlayer] 플레이어 생성됨 - ClientID: 1
```

### 2. GUI 정보 활용
```
좌측 상단 GUI에서 확인:
- 현재 상태 (Host/Client/Connected)
- 연결된 클라이언트 수
- 네트워크 상태
```

### 3. MultiInstanceTestGuide 사용
```
Scene에 MultiInstanceTestGuide 추가하여:
- 환경 자동 검증
- 누락된 컴포넌트 확인
- 설정 상태 실시간 모니터링
```

---

## 📞 추가 지원

### Unity Console 활용
```
Window → General → Console
로그 레벨을 "Collapse" 해제하여 모든 메시지 확인
```

### 네트워크 모니터링
```
Window → Multiplayer → Netcode Graph (설치된 경우)
실시간 네트워크 트래픽 모니터링
```

### 공식 문서 참고
```
Unity Netcode for GameObjects:
https://docs-multiplayer.unity3d.com/

ParrelSync GitHub:
https://github.com/VeriorPies/ParrelSync
```

---

**문제가 계속되면 Unity Console 로그를 복사해서 분석해보세요! 🔍**