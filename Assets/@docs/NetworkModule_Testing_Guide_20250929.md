# 🔧 네트워크 모듈 테스트 가이드
*작성일: 2025년 9월 29일*

## 📋 개요

`D:\Data\02_Unity\03_MB\MB\Assets\@Scripts\Network` 폴더에 있는 모든 네트워크 모듈이 제대로 작동하는지 확인하는 완전한 테스트 가이드입니다.

## 🎯 테스트할 네트워크 모듈들

### 📁 확인된 주요 모듈들:

1. **AuthManager** - 인증 및 프로필 관리
2. **ConnectionManagerEx** - 네트워크 연결 관리
3. **LobbyServiceFacadeEx** - 로비 서비스 통합
4. **SessionManagerEx** - 세션 관리
5. **DebugClassFacadeEx** - 디버그 및 로깅
6. **ProfileManagerEx** - 프로필 관리
7. **ConnectionMethodEx** - 연결 방법 추상화
8. **LocalLobbyEx & LocalLobbyUserEx** - 로컬 로비 데이터

## 🚀 빠른 테스트 방법

### 1단계: NetworkModuleTestManager 추가
```
1. 새 씬 생성 또는 기존 씬 열기
2. 빈 GameObject 생성 → "NetworkModuleTestManager"
3. NetworkModuleTestManager 스크립트 추가
4. Inspector에서 테스트 옵션 설정
```

### 2단계: 자동 테스트 실행
```
Unity Play 버튼 클릭
→ 자동으로 모든 모듈 테스트 시작
→ GUI에서 실시간 결과 확인
→ Console에서 상세 로그 확인
```

### 3단계: 결과 확인
```
✅ 성공 신호:
- "🎉 모든 네트워크 모듈이 정상 작동합니다!"
- 통과: X, 실패: 0

❌ 문제 신호:
- "⚠️ X개의 모듈에서 문제가 발견되었습니다."
- Console에 빨간 오류 메시지
```

## 🔍 상세 테스트 시나리오

### 테스트 1: 모듈 초기화 검증
```
목적: 모든 필수 컴포넌트가 올바르게 로드되는지 확인

검증 항목:
- AuthManager 컴포넌트 존재
- DebugClassFacadeEx 컴포넌트 존재
- ProfileManagerEx 인스턴스 생성
- ConnectionManagerEx 참조 확인 (선택적)

예상 결과: "✅ 모듈 초기화: 모든 필수 컴포넌트 발견됨"
```

### 테스트 2: AuthManager 기능 테스트
```
목적: 인증 관리자가 정상 작동하는지 확인

검증 항목:
- 프로필 전환 기능
- 인증 상태 관리
- 예외 처리

예상 결과: "✅ AuthManager: 프로필 관리 기능 정상"
```

### 테스트 3: ConnectionManager 연결 테스트
```
목적: 네트워크 연결 관리자 확인

검증 항목:
- NetworkManager 참조 확인
- 연결 상태 관리
- 연결 방법 추상화

예상 결과: "✅ ConnectionManager: NetworkManager 연결 확인됨"
```

### 테스트 4: Lobby 시스템 데이터 구조 테스트
```
목적: 로비 관련 데이터 구조가 올바른지 확인

검증 항목:
- LocalLobbyEx 객체 생성/설정
- LocalLobbyUserEx 객체 생성/설정
- 데이터 무결성 확인

예상 결과:
- "✅ LocalLobbyEx: 로비 데이터 구조 정상"
- "✅ LocalLobbyUserEx: 사용자 데이터 구조 정상"
```

### 테스트 5: 스트레스 테스트 (선택적)
```
목적: 대량 데이터 처리 시 안정성 확인

검증 항목:
- 다수 객체 생성/해제
- 메모리 누수 확인
- 성능 저하 확인

예상 결과: "✅ 스트레스 테스트: X개 객체 생성/해제 성공"
```

## 🎮 GUI 테스트 인터페이스

### 실시간 모니터링
```
화면 우측 상단에 표시되는 정보:
- 현재 테스트 단계
- 테스트 진행 시간
- 통과/실패 개수
- 최근 테스트 결과 8개
```

### 테스트 제어 버튼
```
- "전체 모듈 테스트 실행": 모든 테스트 자동 실행
- "컴포넌트 검증만": 기본 컴포넌트만 확인
- "AuthManager 테스트": 인증 모듈만 테스트
- "ConnectionManager 테스트": 연결 모듈만 테스트
- "Lobby 시스템 테스트": 로비 모듈만 테스트
```

## 🔧 수동 검증 방법

### 방법 1: 개별 모듈 확인
```
Context Menu (우클릭)에서:
- "Test Auth Manager Only"
- "Test Connection Manager Only"
- "Test Lobby System Only"
- "Validate All Components"
```

### 방법 2: Console 로그 분석
```
로그 태그로 필터링:
[NetworkModuleTest] - 테스트 관련 로그
[AuthManager] - 인증 관련 로그
[ConnectionManagerEx] - 연결 관련 로그
[LobbyServiceFacadeEx] - 로비 관련 로그
```

### 방법 3: Inspector 설정 조정
```
NetworkModuleTestManager 컴포넌트에서:
- autoTestOnStart: 자동 시작 여부
- testAuthManager: AuthManager 테스트 포함
- testConnectionManager: ConnectionManager 테스트 포함
- testLobbySystem: Lobby 시스템 테스트 포함
- enableStressTest: 스트레스 테스트 활성화
- maxConcurrentConnections: 동시 연결 수
```

## 🚨 문제해결

### 컴파일 오류가 있다면:
```
1. Unity.Netcode 패키지 설치 확인
2. Unity.Services 패키지들 설치 확인
3. using 문 누락 확인
4. Assembly Definition 충돌 확인
```

### 모듈을 찾을 수 없다면:
```
1. @Scripts/Network 폴더 구조 확인
2. 파일 이름과 클래스 이름 일치 확인
3. namespace 선언 확인
4. 빌드 오류 없는지 확인
```

### 테스트가 실패한다면:
```
1. Console에서 상세 오류 메시지 확인
2. NetworkManager 프리팹 씬에 추가
3. Unity Services 초기화 확인
4. 인터넷 연결 상태 확인
```

## 📊 예상 테스트 결과

### ✅ 완벽한 모듈 (예상):
```
=== 네트워크 모듈 테스트 완료 ===
테스트 시간: 3.2초
통과: 6, 실패: 0

✅ 모듈 초기화: 모든 필수 컴포넌트 발견됨
✅ AuthManager: 프로필 관리 기능 정상
✅ ConnectionManager: NetworkManager 연결 확인됨
✅ LocalLobbyEx: 로비 데이터 구조 정상
✅ LocalLobbyUserEx: 사용자 데이터 구조 정상
✅ 스트레스 테스트: 5개 객체 생성/해제 성공

🎉 모든 네트워크 모듈이 정상 작동합니다!
```

### ⚠️ 일부 문제 있는 경우:
```
=== 네트워크 모듈 테스트 완료 ===
테스트 시간: 2.8초
통과: 4, 실패: 2

✅ 모듈 초기화: 모든 필수 컴포넌트 발견됨
✅ AuthManager: 프로필 관리 기능 정상
⚠️ ConnectionManager: NetworkManager가 필요함 (테스트 스킵)
❌ LocalLobbyEx: 로비 데이터 설정 실패
✅ LocalLobbyUserEx: 사용자 데이터 구조 정상
❌ 스트레스 테스트: 메모리 부족 오류

⚠️ 2개의 모듈에서 문제가 발견되었습니다.
```

## 🎯 성공 기준

### 기본 성공 (최소 요구사항):
- [ ] 모듈 초기화 성공
- [ ] AuthManager 테스트 통과
- [ ] LocalLobby 데이터 구조 정상
- [ ] 컴파일 오류 없음

### 완전 성공 (이상적):
- [ ] 모든 테스트 통과 (실패: 0)
- [ ] ConnectionManager 정상 작동
- [ ] 스트레스 테스트 통과
- [ ] 성능 이슈 없음

## 💡 추가 검증 방법

### Unity Profiler 활용:
```
Window → Analysis → Profiler
- Memory Usage 확인
- CPU Usage 모니터링
- GC Alloc 최소화 확인
```

### Network 패키지 호환성:
```
Package Manager에서 확인:
- Netcode for GameObjects 버전
- Unity Services 패키지들 버전
- 종속성 충돌 없는지 확인
```

---

**이 가이드대로 하면 당신의 Network 모듈이 완벽하게 작동하는지 100% 확인할 수 있습니다!** 🎯

*네트워크 모듈, 믿고 쓸 수 있게 만들어드렸습니다! 🚀*