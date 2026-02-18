# BrickGame Sound System (SoundManager 모듈)

## Overview
모듈형 SoundManager를 Managers 패턴에 추가하여 BGM/SFX를 관리한다.
기존 Service Locator 패턴(Managers.cs)에 `Managers.Sound`로 접근 가능하게 한다.

## Architecture Rules (반드시 준수)

### 기존 코드 참조 (반드시 읽고 패턴 따를 것)
- `Assets/@Scripts/Managers/Managers.cs` (390줄) — Service Locator 패턴 참조
  - 네임스페이스: 전역 (using MB.Infrastructure.Messages 등)
  - Contents 섹션에 `private SoundManager _sound = new SoundManager();` 추가
  - `public static SoundManager Sound { get { return Instance?._sound; } }` 추가
- `Assets/@Scripts/Infrastructure/Messages/IAction.cs` — ActionId enum, IActionPayload 참조
  - BrickGame 이벤트: `BrickGame_BrickDestroyed`, `BrickGame_ScoreChanged`, `BrickGame_GameStateChanged`, `BrickGame_GameOver` 등
  - Payload 패턴: `BrickGameStatePayload(GamePhase phase)`, `BrickGameScorePayload(int score, int level)` 등
- `Assets/@Scripts/Contents/BrickGame/BrickGameManager.cs` — 이벤트 흐름 참조
  - `Managers.PublishAction(ActionId.BrickGame_ScoreChanged, new BrickGameScorePayload(...))` 패턴

### SoundManager 설계 원칙
- SoundManager는 POCO 클래스 (MonoBehaviour 불필요, AudioSource는 별도 GO에 생성)
- `Managers.Sound.PlaySFX("brick_break")` 패턴
- AudioClip은 Addressables(ResourceManager)로 로드: `Managers.Resource.Load<AudioClip>("sfx_brick_break")`
- ActionBus 이벤트 구독으로 자동 재생 (BrickDestroyed → SFX)
- 볼륨 설정은 PlayerPrefs 저장
- **SoundManager 자체는 게임 비종속** (generic audio API)
- **BrickGameSoundBinder가 게임 특화 이벤트 ↔ 사운드 매핑**

## Requirements

### 1. SoundManager.cs (새 파일: `Assets/@Scripts/Managers/Core/SoundManager.cs`)

```csharp
public class SoundManager
{
    // AudioSource 관리 (BGM 1개 + SFX 풀)
    private AudioSource _bgmSource;
    private List<AudioSource> _sfxSources; // 3~5개 풀
    private GameObject _audioRoot; // "@AudioSources" GO, DontDestroyOnLoad

    // 볼륨 (0~1)
    public float MasterVolume { get; set; }
    public float BgmVolume { get; set; }
    public float SfxVolume { get; set; }
    public bool IsMuted { get; set; }

    // 초기화 (Managers.cs Awake에서 호출)
    public void Init()
    {
        // 1. "@AudioSources" GO 생성 + DontDestroyOnLoad
        // 2. BGM AudioSource 추가 (loop=true)
        // 3. SFX AudioSource 풀 생성 (3~5개)
        // 4. PlayerPrefs에서 볼륨 로드
    }

    // BGM
    public void PlayBGM(string addressableKey); // Managers.Resource.Load<AudioClip>(key)
    public void StopBGM();
    public void FadeBGM(float targetVolume, float duration);

    // SFX
    public void PlaySFX(string addressableKey); // 풀에서 빈 AudioSource 찾아 재생
    public void PlaySFX(AudioClip clip);

    // 설정 저장
    public void SaveSettings(); // PlayerPrefs.SetFloat("Sound_Master", ...)
    public void LoadSettings(); // PlayerPrefs.GetFloat("Sound_Master", 1f)

    // Cleanup
    public void Clear();
}
```

### 2. Managers.cs 등록

`#region Contents` 섹션에 추가:
```csharp
private SoundManager _sound = new SoundManager();
public static SoundManager Sound { get { return Instance?._sound; } }
```

Awake()에서 Init 호출 (Infrastructure 초기화 이후):
```csharp
_sound.Init();
```

### 3. BrickGameSoundBinder.cs (새 파일: `Assets/@Scripts/Managers/Contents/BrickGame/BrickGameSoundBinder.cs`)

ActionBus 이벤트를 구독하여 적절한 사운드를 자동 재생하는 바인더:

```csharp
public class BrickGameSoundBinder : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();

    public void Initialize()
    {
        // ActionBus 구독 (Managers.Subscribe 사용)
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_BrickDestroyed, OnBrickDestroyed));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_GameStateChanged, OnGameStateChanged));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_GameOver, OnGameOver));
    }

    private void OnBrickDestroyed(ActionMessage msg) => Managers.Sound?.PlaySFX("sfx_brick_break");
    private void OnScoreChanged(ActionMessage msg) => Managers.Sound?.PlaySFX("sfx_score");
    private void OnGameStateChanged(ActionMessage msg)
    {
        if (msg.TryGetPayload<BrickGameStatePayload>(out var payload))
        {
            switch (payload.Phase)
            {
                case GamePhase.StageClear: Managers.Sound?.PlaySFX("sfx_stage_clear"); break;
                case GamePhase.Victory: Managers.Sound?.PlaySFX("sfx_victory"); Managers.Sound?.StopBGM(); break;
                case GamePhase.Playing: Managers.Sound?.PlayBGM("bgm_main"); break;
            }
        }
    }
    private void OnGameOver(ActionMessage msg) { Managers.Sound?.PlaySFX("sfx_game_over"); Managers.Sound?.StopBGM(); }

    public void Dispose()
    {
        foreach (var sub in _subscriptions) sub?.Dispose();
        _subscriptions.Clear();
    }
}
```

### 4. 필요한 오디오 파일 (Addressables 등록)

파일 위치: `Assets/@Resources/Audio/` (또는 Addressables 그룹)
- `bgm_main` — 메인 BGM 루프 (placeholder: 빈 AudioClip 생성)
- `sfx_brick_break` — 벽돌 파괴 효과음
- `sfx_ball_bounce` — 공 바운스 효과음
- `sfx_score` — 점수 획득 효과음
- `sfx_stage_clear` — 스테이지 클리어 팡파레
- `sfx_victory` — 승리 팡파레
- `sfx_game_over` — 게임 오버 효과음
- `sfx_click` — UI 버튼 클릭

> NOTE: 실제 AudioClip 파일이 없으면 빈 AudioClip placeholder를 만들어 컴파일은 통과시킬 것.
> 실제 오디오 파일은 후속 작업에서 교체.

## Key Files to Modify
- `Assets/@Scripts/Managers/Managers.cs` (line 58~69 Contents region) — Sound 프로퍼티 추가

## Key Files to Create
- `Assets/@Scripts/Managers/Core/SoundManager.cs` — 새 파일
- `Assets/@Scripts/Managers/Contents/BrickGame/BrickGameSoundBinder.cs` — 새 파일

## Acceptance Criteria
- [ ] `Managers.Sound` 접근 가능 (null 아님)
- [ ] `Managers.Sound.PlayBGM("bgm_main")` 호출 시 에러 없음
- [ ] `Managers.Sound.PlaySFX("sfx_brick_break")` 호출 시 에러 없음
- [ ] 볼륨 변경이 PlayerPrefs에 저장/로드됨
- [ ] BrickGameSoundBinder가 ActionBus 이벤트 구독하여 자동 SFX 재생
- [ ] 컴파일 0 errors, 0 warnings
- [ ] Unity MCP `recompile_scripts` 통과
