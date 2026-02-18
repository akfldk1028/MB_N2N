# BrickGame Particle Effects (VFX 모듈)

## Overview
벽돌 파괴, 공 바운스, 스테이지 클리어 등에 파티클 이펙트 추가.
기존 ObjectPool(PoolManager) 패턴을 활용하여 파티클 풀링.

## Architecture Rules (반드시 준수)
- VFXManager는 Managers.cs에 등록하거나, PoolManager 확장
- 프리팹은 Addressables로 로드 (ResourceManager)
- ActionBus 이벤트 구독으로 자동 트리거
- ParticleSystem 사용 (Unity built-in)

## Requirements

### VFX 프리팹 목록
1. **vfx_brick_break** - 벽돌 파괴 시 (색상별 파티클 버스트)
2. **vfx_ball_bounce** - 공 바운스 시 (작은 스파크)
3. **vfx_stage_clear** - 스테이지 클리어 (화면 전체 축하 이펙트)
4. **vfx_victory** - 승리 시 (큰 불꽃놀이)
5. **vfx_score_popup** - 점수 획득 시 (+100 텍스트 팝업)

### VFXManager.cs (새 파일)
- SpawnVFX(string name, Vector3 position)
- PoolManager를 통한 파티클 재활용
- 자동 반환 (ParticleSystem 종료 시)

### ActionBus 연결
- BrickDestroyed → vfx_brick_break (파괴 위치)
- BallBounce → vfx_ball_bounce (충돌 위치)
- StageClear → vfx_stage_clear (화면 중앙)
- Victory → vfx_victory (화면 중앙)

## Key Files to Modify
- `Assets/@Scripts/Managers/Core/Managers.cs` - VFXManager 등록
- `Assets/@Scripts/Managers/Contents/BrickGame/VFXManager.cs` - 새 파일
- ParticleSystem 프리팹 5개 생성

## Acceptance Criteria
- [ ] 벽돌 파괴 시 파티클 발생
- [ ] 스테이지 클리어/승리 시 축하 이펙트
- [ ] 파티클 풀링으로 GC 최소화
- [ ] 컴파일 0 errors
