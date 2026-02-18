using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrickGame VFX 매니저. 파티클 이펙트 생성/풀링/자동반환 관리.
/// Managers.VFX로 접근. POCO 클래스 (ParticleSystem은 별도 GO에 생성).
/// PoolManager를 통한 오브젝트 재활용. 코루틴 헬퍼(VFXAutoReturn)로 자동 반환.
/// </summary>
public class VFXManager
{
    // VFX 프리팹 이름 상수
    public const string VFX_BRICK_BREAK = "vfx_brick_break";
    public const string VFX_BALL_BOUNCE = "vfx_ball_bounce";
    public const string VFX_STAGE_CLEAR = "vfx_stage_clear";
    public const string VFX_VICTORY = "vfx_victory";
    public const string VFX_SCORE_POPUP = "vfx_score_popup";

    // 프리팹 템플릿 캐시 (PoolManager에 등록할 원본)
    private readonly Dictionary<string, GameObject> _prefabTemplates = new();
    private GameObject _vfxRoot;

    public void Init()
    {
        _vfxRoot = new GameObject("@VFXRoot");
        Object.DontDestroyOnLoad(_vfxRoot);

        // 프리팹 템플릿 생성 (비활성 상태로 보관)
        CreateBrickBreakPrefab();
        CreateBallBouncePrefab();
        CreateStageClearPrefab();
        CreateVictoryPrefab();
        CreateScorePopupPrefab();

        GameLogger.Success("VFXManager", $"초기화 완료 (VFX 프리팹 {_prefabTemplates.Count}개 등록)");
    }

    /// <summary>
    /// VFX를 지정 위치에 생성. PoolManager로 재활용. 재생 완료 시 자동 반환.
    /// </summary>
    /// <param name="name">VFX 프리팹 이름 (VFX_BRICK_BREAK 등)</param>
    /// <param name="position">월드 좌표 위치</param>
    /// <param name="color">파티클 색상 오버라이드 (null이면 기본 색상)</param>
    public void SpawnVFX(string name, Vector3 position, Color? color = null)
    {
        if (!_prefabTemplates.TryGetValue(name, out var prefab))
        {
            GameLogger.Warning("VFXManager", $"VFX 프리팹을 찾을 수 없음: {name}");
            return;
        }

        // PoolManager에서 꺼내기
        GameObject vfxGo = Managers.Pool.Pop(prefab);
        if (vfxGo == null)
        {
            GameLogger.Error("VFXManager", $"VFX 오브젝트 생성 실패: {name}");
            return;
        }

        vfxGo.transform.position = position;

        // ParticleSystem 재생
        var ps = vfxGo.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            GameLogger.Error("VFXManager", $"ParticleSystem 컴포넌트 없음: {name}");
            Managers.Pool.Push(vfxGo);
            return;
        }

        // 색상 오버라이드 적용
        if (color.HasValue)
        {
            var main = ps.main;
            main.startColor = color.Value;
        }

        ps.Clear();
        ps.Play();

        // 코루틴 헬퍼로 자동 반환 예약
        var autoReturn = vfxGo.GetComponent<VFXAutoReturn>();
        if (autoReturn != null)
        {
            autoReturn.ReturnToPool(ps);
        }
    }

    /// <summary>
    /// VFX 리소스 정리
    /// </summary>
    public void Clear()
    {
        _prefabTemplates.Clear();

        if (_vfxRoot != null)
        {
            Object.Destroy(_vfxRoot);
            _vfxRoot = null;
        }

        GameLogger.Info("VFXManager", "VFX 리소스 정리 완료");
    }

    #region Prefab Creation

    /// <summary>
    /// 프리팹 템플릿을 생성하고 PoolManager에 등록할 수 있도록 캐싱.
    /// 비활성 상태로 _vfxRoot 하위에 보관.
    /// </summary>
    private GameObject CreateVFXTemplate(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_vfxRoot.transform);
        go.AddComponent<VFXAutoReturn>();
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// vfx_brick_break - 벽돌 파괴 시 색상별 파티클 버스트
    /// </summary>
    private void CreateBrickBreakPrefab()
    {
        var go = CreateVFXTemplate(VFX_BRICK_BREAK);
        var ps = go.AddComponent<ParticleSystem>();

        // 기본 렌더러 설정
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.4f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = Color.white;
        main.maxParticles = 20;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 12, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _prefabTemplates[VFX_BRICK_BREAK] = go;
    }

    /// <summary>
    /// vfx_ball_bounce - 공 바운스 시 작은 스파크
    /// </summary>
    private void CreateBallBouncePrefab()
    {
        var go = CreateVFXTemplate(VFX_BALL_BOUNCE);
        var ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = 0.2f;
        main.startSpeed = 2f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.9f, 0.5f); // 노란 스파크
        main.maxParticles = 8;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 4, 8)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _prefabTemplates[VFX_BALL_BOUNCE] = go;
    }

    /// <summary>
    /// vfx_stage_clear - 스테이지 클리어 화면 전체 축하 이펙트
    /// </summary>
    private void CreateStageClearPrefab()
    {
        var go = CreateVFXTemplate(VFX_STAGE_CLEAR);
        var ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var main = ps.main;
        main.duration = 1.5f;
        main.startLifetime = 1.2f;
        main.startSpeed = 5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.cyan);
        main.maxParticles = 60;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 30, 60)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.cyan, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _prefabTemplates[VFX_STAGE_CLEAR] = go;
    }

    /// <summary>
    /// vfx_victory - 승리 시 큰 불꽃놀이
    /// </summary>
    private void CreateVictoryPrefab()
    {
        var go = CreateVFXTemplate(VFX_VICTORY);
        var ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.magenta);
        main.maxParticles = 100;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.8f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 40, 60),
            new ParticleSystem.Burst(0.3f, 20, 40)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _prefabTemplates[VFX_VICTORY] = go;
    }

    /// <summary>
    /// vfx_score_popup - 점수 획득 시 작은 파티클 (+100 텍스트는 UI 레이어에서 처리)
    /// </summary>
    private void CreateScorePopupPrefab()
    {
        var go = CreateVFXTemplate(VFX_SCORE_POPUP);
        var ps = go.AddComponent<ParticleSystem>();

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var main = ps.main;
        main.duration = 0.6f;
        main.startLifetime = 0.5f;
        main.startSpeed = 1.5f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.85f, 0f); // 골드
        main.maxParticles = 10;
        main.loop = false;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f; // 위로 떠오름

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 5, 10)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0f), 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _prefabTemplates[VFX_SCORE_POPUP] = go;
    }

    #endregion
}

/// <summary>
/// VFX 자동 반환 헬퍼 MonoBehaviour.
/// ParticleSystem 재생 완료 시 PoolManager에 자동 반환.
/// VFXManager가 POCO 클래스이므로 코루틴 실행을 위해 필요.
/// </summary>
public class VFXAutoReturn : MonoBehaviour
{
    private Coroutine _returnCoroutine;

    /// <summary>
    /// ParticleSystem 재생 완료 후 풀에 반환 예약
    /// </summary>
    public void ReturnToPool(ParticleSystem ps)
    {
        if (_returnCoroutine != null)
            StopCoroutine(_returnCoroutine);

        _returnCoroutine = StartCoroutine(WaitAndReturn(ps));
    }

    private IEnumerator WaitAndReturn(ParticleSystem ps)
    {
        // ParticleSystem이 재생 완료될 때까지 대기
        while (ps != null && ps.isPlaying)
        {
            yield return null;
        }

        // duration + startLifetime 후 안전하게 반환 대기
        if (ps != null)
        {
            yield return new WaitForSeconds(ps.main.startLifetime.constantMax);
        }

        _returnCoroutine = null;

        // PoolManager에 반환
        if (gameObject.activeSelf)
        {
            Managers.Pool.Push(gameObject);
        }
    }

    private void OnDisable()
    {
        // 풀에 반환될 때 코루틴 정리
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }
}
