---
name: unity-test
description: |
  Unity EditMode/PlayMode 테스트 실행 및 결과 분석. Unity MCP run_tests 사용.
  사용 시점: (1) 기능 구현 후 테스트, (2) 버그 수정 후 회귀 테스트, (3) 새 테스트 작성 후
  사용 금지: 컴파일만 확인(unity-validate quick), 빌드(unity-build)
argument-hint: "[edit|play|all]"
allowed-tools: Read, Grep, Glob, Bash, mcp__unity__unity_advanced_tool, mcp__unity__unity_get_compilation_errors
---

# Unity Test

Unity 테스트를 실행하고 결과를 분석합니다.

## When to Use
- 기능 구현 후 검증
- 버그 수정 후 회귀 테스트
- 새 테스트 작성 후 실행 확인
- PR 전 전체 테스트

## When NOT to Use
- 컴파일만 확인 → `/unity-validate quick`
- 빌드 → `/unity-build`

## Quick Start
```
/unity-test edit   # EditMode 테스트만
/unity-test play   # PlayMode 테스트만
/unity-test all    # 전체 (기본값)
```

## Process

인수: $ARGUMENTS (기본값: all)

### Step 1: 컴파일 확인
테스트 전 `mcp__unity__unity_get_compilation_errors(severity: "error")`로 컴파일 에러 0 확인.

### Step 2: 테스트 실행
- `edit` → EditMode 테스트만
- `play` → PlayMode 테스트만
- `all` → EditMode → PlayMode 순차 실행

### Step 3: 결과 분석
실패한 테스트가 있으면:
1. 실패 원인 분석
2. 관련 코드 파일 확인 (Read)
3. 수정 제안

## 테스트 파일 위치
```
Assets/Tests/
├── EditMode/
│   └── *Test.cs
└── PlayMode/
    └── *Test.cs
```

## 테스트 작성 패턴 (Unity)
```csharp
using NUnit.Framework;

[TestFixture]
public class BrickGameManagerTest
{
    [Test]
    public void AddScore_IncreasesScore()
    {
        var state = new BrickGameState();
        state.AddScore(10);
        Assert.AreEqual(10, state.CurrentScore);
    }
}
```

## Related Skills
- `/unity-validate` - 컴파일+테스트 통합
- `/game-audit` - 전체 시스템 감사
