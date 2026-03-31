---
name: unity-validate
description: |
  Unity 프로젝트 전체 검증. 컴파일 → 콘솔 로그 → EditMode 테스트 → PlayMode 테스트.
  사용 시점: (1) 코드 수정 후 빠른 검증, (2) PR/커밋 전, (3) 버그 수정 후 확인
  사용 금지: 빌드만(unity/build), 씬 구조만(unity/scene)
argument-hint: "[quick|full]"
allowed-tools: Read, Grep, Glob, Bash, mcp__unity__unity_get_compilation_errors, mcp__unity__unity_console_log, mcp__unity__unity_play_mode, mcp__unity__unity_editor_state
---

# Unity Validate

코드 수정 후 전체 검증 사이클을 실행합니다.

## Quick Start
```
/unity-validate        # quick (컴파일+로그)
/unity-validate full   # full (컴파일+로그+테스트)
```

## Process

인수: $ARGUMENTS (기본값: quick)

### Step 1: 스크립트 컴파일
`mcp__unity__unity_get_compilation_errors(severity: "error")` 호출
- 0 errors 필수

### Step 2: 콘솔 로그 확인
`mcp__unity__unity_console_log(type: "error")` 호출
- Error 타입만 보고

### Step 3: EditMode 테스트 (full 모드만)
`mcp__unity__unity_advanced_tool(tool: "unity_testing_run_tests")` (testMode: "EditMode")

### Step 4: PlayMode 테스트 (full 모드만)
`mcp__unity__unity_advanced_tool(tool: "unity_testing_run_tests")` (testMode: "PlayMode")

### Step 5: 결과 리포트
```
## 검증 결과
- 컴파일: ✅ 0 errors, N warnings
- 콘솔: ✅ 새 에러 없음
- EditMode: ✅ N/N passed (full만)
- PlayMode: ✅ N/N passed (full만)
```

## Related
- `/unity-build` - 플랫폼 빌드
- `/unity-test` - 테스트만 실행
- `/game-audit` - 전체 감사
