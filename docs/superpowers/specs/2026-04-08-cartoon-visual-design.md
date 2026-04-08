# Cartoon Visual Overhaul — Design Spec

## Overview
BrickGame 전체 비주얼을 귀엽고 둥근 카툰 스타일로 개선. 하이브리드 접근 (셰이더+스프라이트). 테마 교체 가능.

## Architecture Rules (기존 패턴 준수)
- **Service Locator**: `Managers.Theme` 프로퍼티 추가 → `Managers.cs` Init()에 등록
- **POCO Manager**: `ThemeManager`는 순수 C# 클래스 (MonoBehaviour 아님)
- **모듈 경계**: 비주얼 모듈은 `Contents/Visual/` 하위에 배치
- **ActionBus 이벤트**: 테마 변경 시 `ActionId.ThemeChanged` 발행 → 구독자가 각자 갱신

## 1. Theme System

### ColorThemeSO (ScriptableObject)
```
Assets/@Resources/Themes/
├── Theme_Pastel.asset
├── Theme_Vivid.asset
└── Theme_Candy.asset
```

필드:
- `string themeName`
- `Color[] brickColors` (HP 단계별, 최소 5개)
- `Color team0Color, team1Color`
- `Color bgTop, bgBottom` (배경 그라디언트)
- `Color uiPrimary, uiSecondary`
- `Color particleMain, particleSub`
- `Sprite[] territoryTiles` (중립/팀0/팀1)
- `Sprite backgroundSprite`

### ThemeManager (POCO)
- `Managers.Theme`으로 접근
- `void SetTheme(ColorThemeSO theme)` → ActionBus 발행
- `ColorThemeSO CurrentTheme { get; }`
- 초기 테마: Resources에서 기본 테마 로드

## 2. Brick — Jelly/Bubble Shader

### ShaderGraph: `Shaders/JellyBrick.shadergraph`
- Input: `_BaseColor`, `_GlowIntensity`, `_HitFlash`, `_RoundRadius`
- 반투명 + 내부 하이라이트 (광택)
- UV 기반 둥근 모서리 마스킹
- HitFlash: 0→1→0 (0.1초), 밝아졌다 복원

### BrickVisualController (MonoBehaviour, 벽돌 프리팹에 추가)
- `ThemeChanged` 이벤트 구독 → 색상 갱신
- `OnHit()` → MaterialPropertyBlock으로 `_HitFlash` 애니메이션
- 기존 Brick 로직 수정 없음 — 별도 컴포넌트

### 폰트
- 카툰 둥근 폰트 (무료: Baloo, Fredoka One 등)
- 기존 TextMeshPro 컴포넌트에 폰트만 교체

## 3. Collision Effects

### JuicyHitEffect (프리팹)
```
Assets/@Resources/Effects/
├── JuicyHitEffect.prefab   (벽돌 충돌)
├── BlockCaptureEffect.prefab (Territory 점령)
└── BallTrail.prefab
```

- **스쿼시&스트레치**: DOTween 또는 코루틴 — localScale (1,1)→(1.3,0.7)→(0.9,1.1)→(1,1) 0.2초
- **물방울 파티클**: ParticleSystem, Burst 8~12개, 원형 스프라이트, 테마 색상
- **색번짐**: SpriteRenderer 원형, 0.3초 scale up + fade out
- **공 트레일**: TrailRenderer, 팀 컬러, width 0.1→0, time 0.15초
- **총알 트레일**: TrailRenderer, 팀 컬러, width 0.05→0, time 0.1초

### HitEffectManager (POCO, Managers.HitEffect)
- `void PlayBrickHit(Vector3 pos, Color color)`
- `void PlayBlockCapture(Vector3 pos, Color color)`
- 풀링: `Managers.Pool` 사용

## 4. Territory Grid — Tilemap Texture

### 타일 스프라이트 (256x256, 카툰 스타일)
- 중립: 회색 돌바닥
- 팀0: 잔디
- 팀1: 모래/사막

### TerritoryVisualController (기존 ColorfulCubeGrid에 추가 or 별도 컴포넌트)
- 소유권 변경 시 스프라이트 교체 + 0.3초 페이드
- 먼지 파티클 (BlockCaptureEffect)
- `ThemeChanged` 구독 → 타일 세트 교체

## 5. Background — Illustration Layers

### 레이어 구조 (SpriteRenderer, 각 카메라에 1세트)
- Layer 0: 하늘 그라디언트 (Sorting Order -10)
- Layer 1: 구름 (느린 UV 스크롤, -9)
- Layer 2: 뒷배경 건물/산 (정적, -8)

### BackgroundController (MonoBehaviour, 카메라 하위)
- 구름 스크롤 애니메이션
- `ThemeChanged` 구독 → 배경 스프라이트 세트 교체

## 6. UI — Speech Bubble Style

### UI 에셋
- 9-slice 둥근 패널 스프라이트
- 캡슐형 버튼 스프라이트
- 말풍선 꼬리 스프라이트

### UI 수정 (기존 UI_Base 상속 유지)
- 기존 UI 프리팹의 Image 소스 스프라이트 교체
- 팝업 등장 애니메이션: 바운스 (scale 0→1.1→1, 0.3초)
- 카툰 폰트 적용
- `ThemeChanged` 구독 → UI 색상 갱신

## 7. Camera View Adjustment

- MCP 캡쳐로 확인하며 카메라 위치/크기 조정
- 3분할 비율 (30-40-30) 유지
- 검정 여백 최소화
- 각 카메라의 배경색을 배경 스프라이트로 대체

## Module Boundary

| 새 파일 | 위치 | 역할 |
|---------|------|------|
| ColorThemeSO.cs | Contents/Visual/ | 테마 데이터 SO |
| ThemeManager.cs | Contents/Visual/ | 테마 관리 POCO |
| BrickVisualController.cs | Contents/Visual/ | 벽돌 비주얼 |
| TerritoryVisualController.cs | Contents/Visual/ | Grid 비주얼 |
| BackgroundController.cs | Contents/Visual/ | 배경 레이어 |
| HitEffectManager.cs | Contents/Visual/ | 이펙트 관리 POCO |
| JellyBrick.shadergraph | Shaders/ | 젤리 셰이더 |

기존 파일 수정:
- `Managers.cs` — Theme, HitEffect 프로퍼티 추가
- `ActionId.cs` — ThemeChanged 추가
- 벽돌/Grid 프리팹 — 비주얼 컴포넌트 추가

## MCP 테스트 플로우
1. 셰이더/머티리얼 적용 → `unity_graphics_scene_capture`로 확인
2. Play 모드 → `unity_graphics_game_capture`로 게임뷰 캡쳐
3. 카메라 위치 조정 → 캡쳐 반복
4. 테마 교체 → 런타임 캡쳐 확인
