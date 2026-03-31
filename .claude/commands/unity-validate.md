---
name: unity-validate
description: |
  Unity 프로젝트 전체 검증 사이클. 컴파일 → 콘솔 로그 → EditMode 테스트 → PlayMode 테스트.
  사용 시점: (1) 코드 수정 후 빠른 검증, (2) PR/커밋 전, (3) 버그 수정 후 확인
  사용 금지: 빌드만 필요할 때(unity-build), 씬 구조만 볼 때(unity-scene)
argument-hint: "[quick|full]"
allowed-tools: Read, Grep, Glob, Bash, mcp__unity__unity_get_compilation_errors, mcp__unity__unity_console_log, mcp__unity__unity_advanced_tool
---

# Unity Validate

코드 수정 후 전체 검증 사이클을 실행합니다.

## When to Use
- 코드 수정 후 컴파일 + 에러 확인
- 커밋 전 검증
- 버그 수정 후 회귀 테스트

## When NOT to Use
- 단순 파일 읽기/탐색 → 직접 Read 사용
- 빌드만 필요 → `/unity-build` 사용
- 씬 구조 확인 → `/unity-scene` 사용

## Quick Start
```
/unity-validate        # quick 모드 (컴파일+로그만)
/unity-validate full   # full 모드 (컴파일+로그+테스트)
```

## Process

인수: $ARGUMENTS (기본값: quick)

### Step 1: 스크립트 컴파일
`mcp__unity__unity_get_compilation_errors(severity: "error")` 호출하여 컴파일 에러 확인.
- 0 errors 확인 필수
- warnings는 97개(기존)까지 허용

### Step 2: 콘솔 로그 확인
`mcp__unity__unity_console_log(type: "error")` 호출하여 런타임 에러 확인.
- Error 타입 로그가 있으면 보고

### Step 3: EditMode 테스트 (full 모드만)
`mcp__unity__unity_advanced_tool(tool: "unity_testing_run_tests")` (testMode="EditMode") 실행.
- 실패한 테스트 있으면 원인 분석

### Step 4: PlayMode 테스트 (full 모드만)
`mcp__unity__unity_advanced_tool(tool: "unity_testing_run_tests")` (testMode="PlayMode") 실행.
- 실패한 테스트 있으면 원인 분석

### Step 5: 결과 리포트
```
✅ 컴파일: 0 errors, N warnings
✅ 콘솔: 에러 없음
✅ EditMode: X/X passed
✅ PlayMode: X/X passed
```

## Related Skills
- `/unity-build` - 플랫폼 빌드
- `/unity-scene` - 씬 구조 확인
- `/game-audit` - 전체 게임 감사
