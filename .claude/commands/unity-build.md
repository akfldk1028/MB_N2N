---
name: unity-build
description: |
  Unity 멀티플랫폼 빌드. Android APK, Windows EXE, WebGL 빌드 실행.
  사용 시점: (1) 테스트 통과 후 빌드, (2) 플랫폼별 빌드 확인, (3) 배포 준비
  사용 금지: 코드만 수정, 테스트만(unity-test), 컴파일만(unity-validate)
argument-hint: "[android|windows|webgl|all]"
disable-model-invocation: true
allowed-tools: Read, Grep, Glob, Bash, mcp__mcp-unity__recompile_scripts, mcp__mcp-unity__execute_menu_item, mcp__mcp-unity__get_console_logs
---

# Unity Build

멀티플랫폼 빌드를 실행합니다.

## When to Use
- 테스트 통과 후 빌드
- 플랫폼별 빌드 확인
- 배포 준비

## When NOT to Use
- 코드만 수정할 때 → Edit 도구 직접 사용
- 테스트만 → `/unity-test`
- 컴파일 확인만 → `/unity-validate`

## Quick Start
```
/unity-build android   # Android APK 빌드
/unity-build windows   # Windows EXE 빌드
/unity-build webgl     # WebGL 빌드
```

## Process

인수: $ARGUMENTS (필수)

### Step 0: 사전 검증
1. `mcp__mcp-unity__recompile_scripts` → 0 errors 확인
2. 콘솔 에러 확인

### Step 1: 빌드 설정 확인
- `ProjectSettings/ProjectSettings.asset` 읽기
- Player Settings (Company Name, Product Name, Bundle ID 등)
- 플랫폼별 설정 확인

### Step 2: 빌드 실행

#### Android
```
빌드 경로: Builds/Android/
필수 설정:
- Min API Level: 26+ (Android 8.0)
- Target API: 34+
- IL2CPP Backend
- ARM64 아키텍처
```

#### Windows
```
빌드 경로: Builds/Windows/
필수 설정:
- x86_64 아키텍처
- IL2CPP Backend
```

#### WebGL
```
빌드 경로: Builds/WebGL/
필수 설정:
- Compression: Brotli
- Memory Size: 256MB+
- Exception Handling: Full
```

### Step 3: 결과 확인
- 빌드 로그 확인
- 에러 있으면 분석 및 수정 제안
- 빌드 크기 보고

## 주의사항
- Unity Relay는 WebGL에서도 지원됨
- Android 터치 입력: PlankManager.cs에 터치 핸들링 필요
- WebGL: `System.Threading.Tasks`는 제한적
- IL2CPP: Reflection 주의 (AOT 제한)

## Related Skills
- `/unity-validate` - 빌드 전 검증
- `/unity-test` - 빌드 전 테스트
- `/game-audit` - 전체 품질 점검
