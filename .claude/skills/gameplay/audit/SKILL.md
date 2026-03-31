---
name: game-audit
description: |
  BrickGame + Territory 듀얼 게임 전체 감사. 모듈 연결, 이벤트 흐름, 버그 탐지.
  사용 시점: (1) 프로덕션 점검, (2) 새 기능 추가 전 상태 파악, (3) 버그 원인 추적
  사용 금지: 단순 컴파일(unity/validate), 특정 파일만 수정
argument-hint: "[all|brickgame|territory|network|ui]"
allowed-tools: Read, Grep, Glob, Bash, mcp__unity__unity_get_compilation_errors, mcp__unity__unity_console_log, mcp__unity__unity_scene_hierarchy, mcp__unity__unity_advanced_tool
---

# Game Audit

듀얼 게임 전체를 체계적으로 감사합니다.

## Quick Start
```
/game-audit all         # 전체 감사
/game-audit territory   # 땅따먹기만
/game-audit ui          # UI만
```

## 감사 영역

### brickgame
- BrickGameManager, BrickGameState, BrickGameNetworkSync
- 이벤트: AddScore → BrickGameScorePayload → ActionBus → UI

### territory
- IsometricGridGenerator, Cannon, CannonBullet, BombComponent, HarvestComponent
- 이벤트: Space→CannonBulletRule→Cannon.Fire, Enter→CentralMap

### network
- BrickGameMultiplayerSpawner, NetworkBulletPool
- NetworkVariable 동기화, ServerRpc/ClientRpc 흐름

### ui
- UI_BrickGameScene, BrickGameUIManager, ScoreUI, TerritoryUI, GameResultUI
- Payload 매칭 (SP vs MP)

## 감사 체크리스트
1. Publisher → Subscriber 매칭
2. Payload 타입 일치
3. SP/MP 양쪽 경로 존재
4. null 체크, 에러 핸들링
5. 리소스 정리 (Dispose, OnDestroy)

## 결과 형식
```
## 감사 결과 (YYYY-MM-DD)
### 컴파일: ✅/❌
### 발견된 버그
- BUG-XXX: [설명] (심각도: CRITICAL/HIGH/MEDIUM/LOW)
### 모듈 상태
| 모듈 | 상태 | 비고 |
```

참조: [module-map.md](../_context/project-architecture/module-map.md)

## Related
- `/unity-validate` - 빠른 검증
- `/unity-scene` - 씬 구조
