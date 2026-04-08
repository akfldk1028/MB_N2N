# Cartoon Visual Phase 1 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 테마 시스템 + 젤리 벽돌 셰이더 + 과즙 충돌 이펙트 + 카메라 조정으로 핵심 게임플레이 비주얼을 카툰 스타일로 전환

**Architecture:** Managers 싱글턴 패턴에 ThemeManager(POCO) 등록. ColorThemeSO로 색상/에셋 데이터 관리. ActionBus로 테마 변경 이벤트 전파. 벽돌 비주얼은 별도 컴포넌트(BrickVisualController)로 기존 로직 수정 없이 추가.

**Tech Stack:** Unity 6 URP, ShaderGraph, ParticleSystem, ScriptableObject, TextMeshPro

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Assets/@Scripts/Contents/Visual/ColorThemeSO.cs` | 테마 데이터 SO |
| Create | `Assets/@Scripts/Contents/Visual/ThemeManager.cs` | 테마 관리 POCO |
| Create | `Assets/@Scripts/Contents/Visual/BrickVisualController.cs` | 벽돌 비주얼 제어 |
| Create | `Assets/@Scripts/Contents/Visual/HitEffectManager.cs` | 충돌 이펙트 POCO |
| Create | `Assets/@Shaders/JellyBrick.shadergraph` | 젤리/버블 셰이더 |
| Create | `Assets/@Resources/Themes/Theme_Pastel.asset` | 파스텔 테마 데이터 |
| Create | `Assets/@Resources/Themes/Theme_Vivid.asset` | 비비드 테마 데이터 |
| Create | `Assets/@Resources/Themes/Theme_Candy.asset` | 캔디 테마 데이터 |
| Modify | `Assets/@Scripts/Managers/Managers.cs:59-73` | Theme, HitEffect 프로퍼티 추가 |
| Modify | `Assets/@Scripts/Infrastructure/Messages/IAction.cs:6-30` | Visual_ThemeChanged 추가 |

---

### Task 1: ColorThemeSO — 테마 데이터 ScriptableObject

**Files:**
- Create: `Assets/@Scripts/Contents/Visual/ColorThemeSO.cs`

- [ ] **Step 1: ColorThemeSO 스크립트 생성**

```csharp
using UnityEngine;

namespace MB.Visual
{
    [CreateAssetMenu(fileName = "Theme_New", menuName = "BrickGame/Color Theme")]
    public class ColorThemeSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string themeName = "New Theme";

        [Header("벽돌 색상 (HP 단계별)")]
        public Color[] brickColors = new Color[]
        {
            new Color(1f, 0.8f, 0.8f),  // HP 1
            new Color(1f, 0.6f, 0.6f),  // HP 2
            new Color(0.8f, 0.6f, 1f),  // HP 3
            new Color(0.6f, 0.8f, 1f),  // HP 4
            new Color(0.6f, 1f, 0.8f),  // HP 5
        };

        [Header("팀 색상")]
        public Color team0Color = new Color(0.5f, 0.9f, 0.5f);
        public Color team1Color = new Color(0.9f, 0.9f, 0.3f);

        [Header("배경")]
        public Color bgTop = new Color(0.7f, 0.85f, 1f);
        public Color bgBottom = new Color(0.95f, 0.95f, 1f);

        [Header("UI")]
        public Color uiPrimary = new Color(0.3f, 0.6f, 0.9f);
        public Color uiSecondary = new Color(0.9f, 0.5f, 0.3f);

        [Header("파티클")]
        public Color particleMain = Color.white;
        public Color particleSub = new Color(1f, 0.9f, 0.5f);

        [Header("Territory 타일 (Phase 2)")]
        public Sprite[] territoryTiles;

        [Header("배경 일러스트 (Phase 2)")]
        public Sprite backgroundSprite;

        /// <summary>
        /// HP에 해당하는 벽돌 색상 반환 (범위 초과 시 마지막 색)
        /// </summary>
        public Color GetBrickColor(int hp)
        {
            if (brickColors == null || brickColors.Length == 0) return Color.white;
            int idx = Mathf.Clamp(hp - 1, 0, brickColors.Length - 1);
            return brickColors[idx];
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Run: `unity_get_compilation_errors`
Expected: 0 errors

- [ ] **Step 3: 커밋**

```bash
git add "Assets/@Scripts/Contents/Visual/ColorThemeSO.cs"
git commit -m "feat: ColorThemeSO 테마 데이터 ScriptableObject"
```

---

### Task 2: ThemeManager — POCO 매니저 + Managers 등록

**Files:**
- Create: `Assets/@Scripts/Contents/Visual/ThemeManager.cs`
- Modify: `Assets/@Scripts/Managers/Managers.cs`
- Modify: `Assets/@Scripts/Infrastructure/Messages/IAction.cs`

- [ ] **Step 1: ActionId에 Visual_ThemeChanged 추가**

`Assets/@Scripts/Infrastructure/Messages/IAction.cs`에 enum 항목 추가:

```csharp
// ✅ 비주얼 이벤트
Visual_ThemeChanged,
```

- [ ] **Step 2: ThemeManager 스크립트 생성**

```csharp
using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    /// <summary>
    /// 테마 관리 POCO Manager — Managers.Theme으로 접근
    /// </summary>
    public class ThemeManager
    {
        public ColorThemeSO CurrentTheme { get; private set; }

        public void Init()
        {
            // 기본 테마 로드
            var defaultTheme = Resources.Load<ColorThemeSO>("Themes/Theme_Pastel");
            if (defaultTheme != null)
            {
                CurrentTheme = defaultTheme;
                GameLogger.Success("ThemeManager", $"기본 테마 로드: {defaultTheme.themeName}");
            }
            else
            {
                GameLogger.Warning("ThemeManager", "기본 테마를 찾을 수 없습니다. Resources/Themes/Theme_Pastel 확인");
            }
        }

        public void SetTheme(ColorThemeSO theme)
        {
            if (theme == null) return;
            CurrentTheme = theme;
            Managers.PublishAction(ActionId.Visual_ThemeChanged);
            GameLogger.Info("ThemeManager", $"테마 변경: {theme.themeName}");
        }
    }
}
```

- [ ] **Step 3: Managers.cs에 ThemeManager 등록**

`Managers.cs`의 POCO Manager 섹션에 추가:

```csharp
private ThemeManager _theme = new ThemeManager();
public static ThemeManager Theme { get { return Instance?._theme; } }
```

Init() 메서드에 `_theme.Init();` 추가.

- [ ] **Step 4: 컴파일 확인**

Run: `unity_get_compilation_errors`
Expected: 0 errors

- [ ] **Step 5: 테마 SO 에셋 3개 생성 (MCP)**

```csharp
// unity_execute_code로 ScriptableObject 에셋 생성
var pastel = ScriptableObject.CreateInstance<MB.Visual.ColorThemeSO>();
pastel.themeName = "Pastel";
// ... 색상 설정
UnityEditor.AssetDatabase.CreateAsset(pastel, "Assets/@Resources/Themes/Theme_Pastel.asset");
// Vivid, Candy도 동일하게
```

- [ ] **Step 6: MCP 검증 — 테마 로드 확인**

Play 모드 → `unity_execute_code`로 `Managers.Theme.CurrentTheme.themeName` 확인

- [ ] **Step 7: 커밋**

```bash
git add "Assets/@Scripts/Contents/Visual/ThemeManager.cs" \
        "Assets/@Scripts/Managers/Managers.cs" \
        "Assets/@Scripts/Infrastructure/Messages/IAction.cs" \
        "Assets/@Resources/Themes/"
git commit -m "feat: ThemeManager POCO + Managers 등록 + 테마 3종"
```

---

### Task 3: JellyBrick 셰이더 — ShaderGraph

**Files:**
- Create: `Assets/@Shaders/JellyBrick.shadergraph` (MCP로 생성)
- Create: `Assets/@Resources/Materials/Mat_JellyBrick.mat`

- [ ] **Step 1: ShaderGraph 생성 (MCP unity_execute_code)**

URP Lit 기반 ShaderGraph를 코드로 생성하거나, unity_shader 스킬로 생성.
주요 속성:
- `_BaseColor` (Color) — 테마에서 주입
- `_GlowIntensity` (Float, 0~1) — 내부 광택 강도
- `_HitFlash` (Float, 0~1) — 히트 시 밝기
- `_Alpha` (Float, 0.7~1) — 반투명도

Fallback: ShaderGraph가 MCP로 생성 불가 시, URP/Lit 셰이더 + MaterialPropertyBlock으로 색상/알파 제어.

- [ ] **Step 2: 머티리얼 생성**

```csharp
// unity_execute_code
var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
mat.SetFloat("_Surface", 1); // Transparent
mat.SetFloat("_Blend", 0);   // Alpha
mat.SetColor("_BaseColor", new Color(1, 0.8f, 0.8f, 0.85f));
mat.SetFloat("_Smoothness", 0.9f); // 광택
mat.enableInstancing = true;
UnityEditor.AssetDatabase.CreateAsset(mat, "Assets/@Resources/Materials/Mat_JellyBrick.mat");
```

- [ ] **Step 3: Scene View 캡쳐로 머티리얼 확인**

벽돌 프리팹에 임시 적용 → `unity_graphics_scene_capture`로 확인

- [ ] **Step 4: 커밋**

```bash
git add "Assets/@Shaders/" "Assets/@Resources/Materials/"
git commit -m "feat: JellyBrick 젤리 머티리얼 (URP Lit 반투명)"
```

---

### Task 4: BrickVisualController — 벽돌 비주얼 컴포넌트

**Files:**
- Create: `Assets/@Scripts/Contents/Visual/BrickVisualController.cs`

- [ ] **Step 1: BrickVisualController 생성**

```csharp
using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    /// <summary>
    /// 벽돌 프리팹에 추가하는 비주얼 컴포넌트
    /// 기존 Brick 로직 수정 없이 색상/히트 애니메이션 처리
    /// </summary>
    public class BrickVisualController : MonoBehaviour
    {
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _hp = 1;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void SetHP(int hp)
        {
            _hp = hp;
            UpdateColor();
        }

        public void OnHit()
        {
            StartCoroutine(HitFlashCoroutine());
            StartCoroutine(SquashCoroutine());
        }

        private void UpdateColor()
        {
            var theme = Managers.Theme?.CurrentTheme;
            if (theme == null || _renderer == null) return;

            _renderer.GetPropertyBlock(_mpb);
            Color c = theme.GetBrickColor(_hp);
            c.a = 0.85f; // 젤리 반투명
            _mpb.SetColor(BaseColorId, c);
            _renderer.SetPropertyBlock(_mpb);
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            if (_renderer == null) yield break;
            _renderer.GetPropertyBlock(_mpb);
            Color original = Managers.Theme?.CurrentTheme?.GetBrickColor(_hp) ?? Color.white;
            original.a = 0.85f;

            // Flash white
            _mpb.SetColor(BaseColorId, Color.white);
            _renderer.SetPropertyBlock(_mpb);
            yield return new WaitForSeconds(0.08f);

            // Restore
            _mpb.SetColor(BaseColorId, original);
            _renderer.SetPropertyBlock(_mpb);
        }

        private System.Collections.IEnumerator SquashCoroutine()
        {
            Vector3 orig = Vector3.one;
            // Squash
            transform.localScale = new Vector3(1.3f, 0.7f, 1f);
            yield return new WaitForSeconds(0.05f);
            // Stretch
            transform.localScale = new Vector3(0.85f, 1.15f, 1f);
            yield return new WaitForSeconds(0.05f);
            // Settle
            transform.localScale = new Vector3(1.05f, 0.95f, 1f);
            yield return new WaitForSeconds(0.04f);
            // Restore
            transform.localScale = orig;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 벽돌 프리팹에 컴포넌트 추가 (MCP)**

```csharp
// unity_execute_code — 벽돌 프리팹 찾아서 BrickVisualController 추가
```

- [ ] **Step 4: Play 모드 → Game View 캡쳐 확인**

`unity_graphics_game_capture`로 벽돌 색상/반투명 확인

- [ ] **Step 5: 커밋**

```bash
git add "Assets/@Scripts/Contents/Visual/BrickVisualController.cs"
git commit -m "feat: BrickVisualController 젤리 벽돌 비주얼 + 히트 이펙트"
```

---

### Task 5: HitEffectManager — 충돌 이펙트 POCO

**Files:**
- Create: `Assets/@Scripts/Contents/Visual/HitEffectManager.cs`

- [ ] **Step 1: HitEffectManager 생성**

```csharp
using UnityEngine;

namespace MB.Visual
{
    /// <summary>
    /// 충돌 이펙트 관리 POCO — Managers.HitEffect으로 접근
    /// 파티클 생성 + 풀링(Managers.Pool 사용)
    /// </summary>
    public class HitEffectManager
    {
        private GameObject _hitParticlePrefab;

        public void Init()
        {
            // 파티클 프리팹 로드
            _hitParticlePrefab = Managers.Resource.Load<GameObject>("Effects/JuicyHitEffect");
            if (_hitParticlePrefab == null)
                GameLogger.Warning("HitEffectManager", "JuicyHitEffect 프리팹 없음 — 이펙트 비활성");
        }

        public void PlayBrickHit(Vector3 position, Color color)
        {
            if (_hitParticlePrefab == null) return;

            var go = Managers.Pool.Pop(_hitParticlePrefab);
            go.transform.position = position;

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                ps.Play();
            }

            // 자동 반환 (1초 후)
            Managers.Pool.Push(go, 1f);
        }

        public void PlayBlockCapture(Vector3 position, Color color)
        {
            PlayBrickHit(position, color); // 동일 이펙트 재사용
        }
    }
}
```

- [ ] **Step 2: Managers.cs에 HitEffect 등록**

```csharp
private HitEffectManager _hitEffect = new HitEffectManager();
public static HitEffectManager HitEffect { get { return Instance?._hitEffect; } }
```

Init()에 `_hitEffect.Init();` 추가.

- [ ] **Step 3: 파티클 프리팹 생성 (MCP)**

`unity_execute_code`로 ParticleSystem 오브젝트 생성 → 프리팹 저장
- Burst: 10개
- Shape: Sphere
- Size: 0.1 → 0
- Lifetime: 0.3초
- Speed: 3
- Color: StartColor = 테마에서 주입

- [ ] **Step 4: 공/총알 TrailRenderer 추가 (MCP)**

Ball 프리팹에 TrailRenderer 추가:
- Width: 0.15 → 0
- Time: 0.15초
- Color: 팀 컬러

- [ ] **Step 5: Play 모드 → 충돌 시 이펙트 캡쳐 확인**

- [ ] **Step 6: 커밋**

```bash
git add "Assets/@Scripts/Contents/Visual/HitEffectManager.cs" \
        "Assets/@Scripts/Managers/Managers.cs" \
        "Assets/@Resources/Effects/"
git commit -m "feat: HitEffectManager + 과즙 파티클 + 트레일"
```

---

### Task 6: 카메라 뷰 조정

**Files:**
- Modify: Camera 설정 (MCP로 런타임 조정)

- [ ] **Step 1: 현재 카메라 상태 캡쳐**

3개 카메라 모두 캡쳐:
- `unity_graphics_game_capture(cameraName: "Main_Camera")`
- `unity_graphics_game_capture(cameraName: "Sub_Camera")`
- `unity_graphics_game_capture(cameraName: "Upper_Main_Camera")`

- [ ] **Step 2: 카메라 위치/크기 조정**

`unity_execute_code`로 카메라 orthographicSize, position 조정하며 캡쳐 반복.
목표: 게임 영역이 화면에 꽉 차도록, 검정 여백 최소화.

- [ ] **Step 3: 배경색을 테마 그라디언트로 변경**

각 카메라의 `clearFlags = CameraClearFlags.SolidColor`, `backgroundColor = theme.bgTop`

- [ ] **Step 4: 최종 캡쳐 확인 + 커밋**

```bash
git commit -m "fix: 카메라 뷰 조정 + 배경색 테마 적용"
```

---

### Task 7: 통합 테스트 — MCP Play 모드 검증

- [ ] **Step 1: Play 모드 시작 + MPPM 접속**

- [ ] **Step 2: 전체 화면 캡쳐 (3카메라)**

벽돌 젤리 색상, 히트 이펙트, 트레일, 배경색 확인

- [ ] **Step 3: 테마 교체 테스트**

```csharp
// unity_execute_code
var vivid = Resources.Load<MB.Visual.ColorThemeSO>("Themes/Theme_Vivid");
Managers.Theme.SetTheme(vivid);
```

캡쳐로 색상 변경 확인

- [ ] **Step 4: 문제 발견 시 수정 → 재캡쳐**

- [ ] **Step 5: 최종 커밋 + 푸쉬**

```bash
git push
```
