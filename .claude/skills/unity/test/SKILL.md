---
name: unity-test
description: |
  Unity EditMode/PlayMode 테스트 실행 및 결과 분석.
  사용 시점: (1) 기능 구현 후 테스트, (2) 버그 수정 후 회귀 테스트, (3) 새 테스트 작성 후
  사용 금지: 컴파일만(unity/validate quick), 빌드(unity/build)
argument-hint: "[edit|play|all]"
allowed-tools: Read, Grep, Glob, Bash, mcp__mcp-unity__run_tests, mcp__mcp-unity__recompile_scripts
---

# Unity Test

테스트를 실행하고 결과를 분석합니다.

## Quick Start
```
/unity-test edit   # EditMode 테스트만
/unity-test play   # PlayMode 테스트만
/unity-test all    # 전체 (기본값)
```

## Process

인수: $ARGUMENTS (기본값: all)

### Step 1: 컴파일 확인
`mcp__mcp-unity__recompile_scripts` → 0 errors

### Step 2: 테스트 실행
- `edit` → EditMode만
- `play` → PlayMode만
- `all` → EditMode → PlayMode 순차

### Step 3: 결과 분석
실패 시: 원인 분석 → 관련 코드 확인 → 수정 제안

## 테스트 파일 위치
```
Assets/Tests/
├── EditMode/ → *Test.cs
└── PlayMode/ → *Test.cs
```

## Related
- `/unity-validate` - 컴파일+테스트 통합
