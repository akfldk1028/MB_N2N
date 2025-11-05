# Unity Netcode 네트워크 테스트 가이드
*작성일: 2024년 9월 29일*

## 📋 개요

이 프로젝트는 Unity Netcode for GameObjects를 사용하여 멀티플레이어 게임을 개발하기 위한 완전한 테스트 환경을 제공합니다. Unity Services 없이도 로컬에서 Host/Client 테스트가 가능하며, 여러 창을 이용한 멀티플레이어 시뮬레이션을 지원합니다.

## 🎯 테스트 스크립트 구성

### 📁 테스트 파일 위치: `Assets/@Scripts/Test/`

1. **`LocalNetworkTestManager.cs`** - 메인 네트워크 테스트 매니저
2. **`DummyPlayer.cs`** - 네트워크 동기화 플레이어 오브젝트
3. **`DummyGameManager.cs`** - 플레이어 스폰 및 게임 로직
4. **`MultiInstanceTestGuide.cs`** - 테스트 환경 검증 및 가이드
5. **`NetworkTestManager.cs`** - Unity Services 기반 테스트 (고급)

## 🚀 빠른 시작 가이드

### 1단계: 기본 설정

1. **Unity 프로젝트 열기**
   ```
   D:\Data\02_Unity\03_MB\MB 프로젝트 열기
   ```

2. **필수 패키지 확인**
   - `com.unity.netcode.gameobjects@2.5.1` ✅
   - `com.unity.services.multiplayer@1.1.8` ✅
   - `com.unity.services.core@1.13.0` ✅

3. **테스트 씬 준비**
   ```
   1. 새 씬 생성 또는 기존 씬 열기
   2. 빈 GameObject 생성 → "NetworkTestManager" 이름 변경
   3. LocalNetworkTestManager 컴포넌트 추가
   4. NetworkManager 프리팹을 씬에 추가
   ```

### 2단계: NetworkManager 설정

1. **NetworkManager 프리팹 생성**
   ```
   1. 빈 GameObject 생성 → "NetworkManager" 이름
   2. NetworkManager 컴포넌트 추가
   3. UnityTransport 컴포넌트 추가
   4. Prefabs 폴더에 저장
   ```

2. **Transport 설정 확인**
   ```
   UnityTransport 컴포넌트에서:
   - Protocol Type: UnityTransport
   - Connection Data:
     - Address: 127.0.0.1
     - Port: 7777
   ```

## 🎮 테스트 방법

### 방법 1: ParrelSync 사용 (추천) 🌟

#### ParrelSync 설치
```
1. Unity 에디터에서: Window → Package Manager
2. 좌측 상단 "+" 버튼 클릭
3. "Add package from git URL..." 선택
4. 다음 URL 입력:
   https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync
5. "Add" 버튼 클릭하여 설치 (1-2분 소요)
```

#### Clone 생성 및 테스트
```
1. ParrelSync → Clones Manager 메뉴 열기
2. "Add new clone" 버튼 클릭
3. Clone 이름 입력 (예: "Client_Clone")
4. "Create" 버튼 클릭 (몇 분 소요)
5. "Open in New Editor" 클릭하여 Clone Unity 열기

테스트 실행:
- 원본 Unity: Play 버튼 → Host 자동 시작
- Clone Unity: Play 버튼 → Client 자동 연결 (3초 후)
```

### 방법 2: 빌드 파일 테스트

#### Windows
```
1. File → Build Settings → Build
2. 명령 프롬프트에서:
   YourGame.exe -mode host     # Host 시작
   YourGame.exe -mode client   # Client 시작
```

#### Mac
```
1. File → Build Settings → Build
2. 터미널에서:
   open -n YourGame.app  # 첫 번째 실행 (Host)
   open -n YourGame.app  # 두 번째 실행 (Client)
```

### 방법 3: 수동 GUI 테스트

```
1. Unity에서 Play 버튼
2. GUI에서 "Host로 시작" 버튼 클릭
3. 다른 컴퓨터에서 같은 프로젝트 실행
4. "Client로 시작" 버튼 클릭
```

## 🎪 플레이어 스폰 테스트

### DummyPlayer 프리팹 생성

1. **기본 프리팹 생성**
   ```
   1. GameObject → 3D Object → Cube 생성
   2. DummyPlayer 스크립트 추가
   3. NetworkObject 컴포넌트 추가
   4. Prefabs 폴더에 저장
   ```

2. **GameManager 설정**
   ```
   1. 빈 GameObject 생성 → "DummyGameManager"
   2. DummyGameManager 스크립트 추가
   3. Player Prefab 필드에 DummyPlayer 프리팹 할당
   4. NetworkObject 컴포넌트 추가
   ```

### 테스트 결과 확인

✅ **성공적인 연결 시 확인사항:**
- Host GUI: "연결된 클라이언트: 1" 표시
- Client GUI: "서버에 성공적으로 연결됨!" 표시
- 다른 색상의 플레이어들이 자동 스폰
- 플레이어들이 원형으로 자동 움직임
- WASD로 Host 플레이어 조작 가능

## 🔧 고급 테스트 기능

### 1. RPC 통신 테스트
```
DummyPlayer에서 제공하는 RPC 메서드:
- TestRpcToServerRpc(): 서버로 메시지 전송
- TestRpcToAllClientsRpc(): 모든 클라이언트로 브로드캐스트
- TestRpcToOwnerRpc(): 소유자에게만 전송
```

### 2. 성능 테스트
```
DummyGameManager GUI에서:
- "성능 테스트 시작": 다수 오브젝트 생성으로 네트워크 부하 테스트
- "테스트 오브젝트 정리": 생성된 테스트 오브젝트들 제거
```

### 3. NetworkVariable 동기화 테스트
```
DummyPlayer가 자동으로 테스트하는 항목:
- 위치 동기화 (NetworkVariable<Vector3>)
- 회전 동기화 (NetworkVariable<Quaternion>)
- 색상 동기화 (NetworkVariable<Color>)
```

## 🚨 문제해결

### 컴파일 오류
```
문제: NetworkManager를 찾을 수 없음
해결:
1. Netcode for GameObjects 패키지 설치 확인
2. using Unity.Netcode; 문 확인
3. NetworkManager 프리팹이 씬에 있는지 확인
```

### 연결 실패
```
문제: Client가 Host에 연결되지 않음
해결:
1. 방화벽에서 7777 포트 허용
2. IP 주소가 127.0.0.1인지 확인
3. Host가 먼저 시작되었는지 확인
4. UnityTransport 설정 확인
```

### ParrelSync 설치 실패
```
문제: Git URL로 패키지 설치 안됨
해결:
1. Unity 2021.3 이상 버전 사용
2. Git이 시스템에 설치되어 있는지 확인
3. 인터넷 연결 확인
4. Unity Package Manager 재시작
```

### 플레이어 스폰 안됨
```
문제: 연결되었지만 플레이어가 나타나지 않음
해결:
1. DummyPlayer 프리팹에 NetworkObject 컴포넌트 확인
2. NetworkManager의 NetworkPrefabs 목록에 등록 확인
3. DummyGameManager가 씬에 있고 NetworkObject가 스폰되었는지 확인
```

## 📊 테스트 체크리스트

### 기본 연결 테스트
- [ ] Host 시작 성공
- [ ] Client 연결 성공
- [ ] GUI에서 연결 상태 확인
- [ ] 연결 해제 정상 작동

### 플레이어 동기화 테스트
- [ ] 플레이어 자동 스폰
- [ ] 다른 색상으로 구분
- [ ] 위치 동기화 (자동 움직임)
- [ ] 수동 조작 동기화 (WASD)

### RPC 통신 테스트
- [ ] Server RPC 전송
- [ ] Client RPC 수신
- [ ] 브로드캐스트 RPC
- [ ] 소유자 RPC

### 성능 테스트
- [ ] 다수 오브젝트 동기화
- [ ] 네트워크 트래픽 모니터링
- [ ] 프레임율 확인
- [ ] 메모리 사용량 확인

## 🎯 다음 단계

### Unity Services 통합 테스트
```
NetworkTestManager.cs 사용:
- Unity Authentication 통합
- Unity Multiplayer Sessions API
- Relay 서비스를 통한 원격 연결
- 로비 시스템 테스트
```

### 프로덕션 준비
```
1. 네트워크 보안 설정
2. 치트 방지 시스템
3. 서버 권위 검증
4. 연결 복구 시스템
5. 로드 밸런싱
```

---

## 📞 지원 및 문의

문제가 발생하거나 추가 기능이 필요한 경우:
1. Unity Console 로그 확인
2. MultiInstanceTestGuide 컴포넌트로 환경 검증
3. 각 테스트 스크립트의 상세 로그 활용

**Happy Networking! 🎮🌐**