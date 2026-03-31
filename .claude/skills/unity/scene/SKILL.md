---
name: unity-scene
description: |
  Unity 씬 구조 조사. 씬 정보, 게임오브젝트 계층, 컴포넌트 상태 확인.
  사용 시점: (1) 씬 구조 파악, (2) 오브젝트 찾기, (3) 컴포넌트 값 확인
  사용 금지: 코드만 수정(Edit), 컴파일 확인(unity/validate)
argument-hint: "[info|find <name>|inspect <path>]"
allowed-tools: Read, Grep, Glob, mcp__unity__unity_scene_hierarchy, mcp__unity__unity_gameobject_info, mcp__unity__unity_component_set_property, mcp__unity__unity_scene_open, mcp__unity__unity_scene_save
---

# Unity Scene

씬 구조를 조사하고 오브젝트를 검사합니다.

## Quick Start
```
/unity-scene info                    # 현재 씬 정보
/unity-scene find IsometricGrid      # 오브젝트 찾기
/unity-scene inspect @Managers       # 오브젝트 상세 검사
```

## GameScene 구조
```
GameScene (Build Index 1)
├── @Managers          (런타임, DontDestroyOnLoad)
├── @NetworkSystems    (런타임, DontDestroyOnLoad)
├── IsometricGrid      (Territory 그리드)
├── UI_BrickGameScene  (BrickGame UI)
├── Ball / Plank       (물리 오브젝트)
├── LeftEnd / RightEnd (경계)
└── Cameras            (메인 + 서브)
```

## Related
- `/unity-validate` - 코드 검증
- `/game-audit` - 전체 감사
