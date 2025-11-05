# 🔧 ParrelSync 없이 멀티플레이어 테스트하는 방법
*작성일: 2025년 9월 29일*

## ⚠️ ParrelSync Unity 6 호환성 문제

ParrelSync가 Unity 6에서 작동하지 않을 수 있습니다. 하지만 **걱정 마세요!** 다른 방법들이 있어요!

---

## 🎯 방법 1: 빌드 파일로 테스트 (가장 확실!)

### Windows에서:
```
1. File → Build Settings
2. Platform: Windows → Switch Platform
3. Build 클릭하여 .exe 파일 생성

실행:
cmd에서:
YourGame.exe -mode host      # Host 시작
YourGame.exe -mode client    # Client 시작

또는 단축 실행:
YourGame.exe -mode host & YourGame.exe -mode client
```

### 빌드 설정 최적화:
```
Build Settings에서:
- Development Build ✅ 체크
- Script Debugging ✅ 체크
- Deep Profiling Support ✅ 체크
```

---

## 🎯 방법 2: Unity 에디터 + 빌드 조합

### 가장 실용적인 방법:
```
1. Unity 에디터: Host로 실행
2. 빌드 파일: Client로 실행

장점:
- 에디터에서 디버깅 가능
- 빌드는 실제 환경과 동일
- 코드 수정 즉시 반영
```

### 설정 방법:
```
1. LocalNetworkTestManager에서:
   - autoStartOnPlay = false (수동 시작)

2. Unity 에디터에서:
   - Play → "Host로 시작" 버튼 클릭

3. 빌드 파일 실행:
   - YourGame.exe → "Client로 시작" 버튼 클릭
```

---

## 🎯 방법 3: 수동 IP 연결 (다른 컴퓨터)

### 네트워크 설정:
```csharp
// LocalNetworkTestManager.cs에서 수정
[Header("네트워크 설정")]
public string serverIP = "192.168.1.100";  // Host PC의 실제 IP
public ushort serverPort = 7777;
```

### Host PC 설정:
```
1. cmd에서 IP 확인: ipconfig
2. 방화벽에서 7777 포트 허용
3. LocalNetworkTestManager의 serverIP를 "0.0.0.0"으로 설정
```

### Client PC 설정:
```
1. 같은 Unity 프로젝트 복사
2. LocalNetworkTestManager의 serverIP를 Host PC IP로 설정
3. Play → "Client로 시작"
```

---

## 🎯 방법 4: 명령줄 자동화 (편리함!)

### 배치 파일 생성:

#### **start_host.bat:**
```batch
@echo off
echo Host 시작 중...
cd /d "빌드 폴더 경로"
YourGame.exe -mode host
pause
```

#### **start_client.bat:**
```batch
@echo off
echo Client 시작 중...
cd /d "빌드 폴더 경로"
YourGame.exe -mode client
pause
```

#### **start_both.bat:**
```batch
@echo off
echo 멀티플레이어 테스트 시작...
cd /d "빌드 폴더 경로"
start YourGame.exe -mode host
timeout /t 3
start YourGame.exe -mode client
echo 완료! Host와 Client가 시작되었습니다.
pause
```

---

## 🎯 방법 5: Unity 6용 대안 도구

### 1. **Unity Multiplayer Play Mode** (공식!)
```
Package Manager에서:
com.unity.multiplayer.playmode

장점:
- Unity 공식 도구
- Unity 6 완전 지원
- Virtual Players 생성 가능
```

### 2. **NetcodeTestHelper** (커스텀)
우리가 만든 코드를 활용:
```csharp
// 이미 구현된 기능들:
- 명령줄 자동 감지
- GUI 수동 제어
- 자동 역할 배정
- 상세한 로깅
```

---

## 🎯 방법 6: 개발자 모드 활용

### Unity 에디터에서 다중 인스턴스:
```
1. Unity Hub에서 같은 프로젝트를 다른 버전으로 열기
2. 또는 프로젝트 폴더 복사해서 별도로 열기
3. 각각 다른 역할(Host/Client)로 실행
```

### 주의사항:
```
- 같은 폴더 동시 수정 시 충돌 가능
- Library 폴더 공유 문제
- 프로젝트 설정 동기화 이슈
```

---

## 📋 추천 워크플로우 (Unity 6용)

### 🥇 **최고 추천: 에디터 + 빌드**
```
1. Unity 에디터: Host (디버깅 용이)
2. 빌드 파일: Client (실제 환경)
3. GUI 버튼으로 수동 시작
4. 실시간 로그 모니터링
```

### 🥈 **편의성 중시: 빌드 파일 + 배치**
```
1. 개발용 빌드 생성 (Development Build)
2. 배치 파일로 Host/Client 자동 시작
3. 명령줄 인수로 역할 자동 배정
4. 빠른 반복 테스트
```

### 🥉 **안정성 중시: 다중 PC**
```
1. 실제 네트워크 환경 테스트
2. 각 PC에서 빌드 실행
3. 실제 지연시간 경험
4. 프로덕션 환경과 동일
```

---

## 🔧 코드 수정 (ParrelSync 없이 최적화)

### LocalNetworkTestManager.cs 개선:
```csharp
[Header("테스트 모드")]
public bool forceHostMode = false;      // 강제 Host 모드
public bool forceClientMode = false;    // 강제 Client 모드
public bool useCommandLineArgs = true;  // 명령줄 인수 사용

private void Start()
{
    ProcessTestMode();

    if (autoStartOnPlay)
    {
        StartCoroutine(AutoStartTest());
    }
}

private void ProcessTestMode()
{
    if (forceHostMode)
    {
        Log("강제 Host 모드로 설정됨");
        // Host로 강제 시작
    }
    else if (forceClientMode)
    {
        Log("강제 Client 모드로 설정됨");
        // Client로 강제 시작
    }
    else if (useCommandLineArgs)
    {
        // 기존 명령줄 처리 로직
    }
}
```

---

## ✅ 결론: ParrelSync 없어도 괜찮아요!

### 장점들:
- **빌드 테스트**: 실제 환경과 동일
- **명령줄 자동화**: 더 편리한 워크플로우
- **Unity 6 호환**: 버전 문제 없음
- **네트워크 테스트**: 실제 지연시간 경험

### 우리 코드의 강점:
- **명령줄 지원**: ParrelSync 없이도 자동화
- **GUI 제어**: 수동 테스트 가능
- **상세 로깅**: 문제 진단 용이
- **유연한 설정**: 다양한 테스트 시나리오

**ParrelSync가 안되어도 완벽하게 멀티플레이어 테스트 가능합니다!** 🎯

---
*2025년에도 변치 않는 네트워크 테스트의 진리! 🚀*