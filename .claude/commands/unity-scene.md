---
name: unity-scene
description: |
  Unity 씬 구조 조사. 씬 정보, 게임오브젝트 계층, 컴포넌트 상태 확인.
  사용 시점: (1) 씬 구조 파악, (2) 오브젝트 찾기, (3) 컴포넌트 값 확인, (4) 프리팹 상태 확인
  사용 금지: 코드만 수정(Edit), 컴파일 확인(unity-validate)
argument-hint: "[info|find <name>|inspect <path>]"
allowed-tools: Read, Grep, Glob, mcp__unity__unity_scene_hierarchy, mcp__unity__unity_gameobject_info, mcp__unity__unity_component_set_property, mcp__unity__unity_scene_open, mcp__unity__unity_scene_save
---

# Unity Scene

Unity 씬 구조를 조사하고 오브젝트를 검사합니다.

## When to Use
- 현재 씬 계층 구조 파악
- 특정 게임오브젝트 찾기
- 컴포넌트 속성값 확인/수정
- 씬 전환 테스트

## When NOT to Use
- C# 코드 수정 → Edit 도구 직접 사용
- 컴파일 확인 → `/unity-validate`

## Quick Start
```
/unity-scene info                    # 현재 씬 정보
/unity-scene find IsometricGrid      # 오브젝트 찾기
/unity-scene inspect @Managers       # 오브젝트 상세 검사
```

## Process

### info 모드 (기본)
`mcp__unity__unity_scene_hierarchy` 호출하여:
- 현재 씬 이름, Build Index
- 루트 오브젝트 목록
- 오브젝트 수

### find 모드
`mcp__unity__unity_gameobject_info` 호출하여:
- 이름으로 오브젝트 검색
- 컴포넌트 목록 확인
- Transform 정보

### inspect 모드
오브젝트 경로로 상세 검사:
- 모든 컴포넌트와 속성값
- 자식 오브젝트 계층
- NetworkObject 상태 (Spawned 여부)

## 주요 씬 구조 (GameScene)
```
GameScene (Build Index 1)
├── @Managers          (런타임 생성, DontDestroyOnLoad)
├── @NetworkSystems    (런타임 생성, DontDestroyOnLoad)
├── IsometricGrid      (Territory 그리드)
├── UI_BrickGameScene  (BrickGame UI)
├── Ball / Plank       (물리 오브젝트)
├── LeftEnd / RightEnd (경계)
└── Cameras            (메인 + 서브)
```

## Related Skills
- `/unity-validate` - 코드 검증
- `/game-audit` - 전체 감사
