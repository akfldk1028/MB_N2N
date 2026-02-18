using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 게임 비종속 오디오 매니저. BGM/SFX 재생, 볼륨 관리, PlayerPrefs 저장.
/// Managers.Sound로 접근. POCO 클래스 (AudioSource는 별도 GO에 생성).
/// </summary>
public class SoundManager
{
    private AudioSource _bgmSource;
    private readonly List<AudioSource> _sfxSources = new();
    private GameObject _audioRoot;

    private const int SFX_POOL_SIZE = 5;
    private const string PREF_MASTER = "Sound_Master";
    private const string PREF_BGM = "Sound_BGM";
    private const string PREF_SFX = "Sound_SFX";
    private const string PREF_MUTED = "Sound_Muted";

    // 로드된 AudioClip 캐시
    private readonly Dictionary<string, AudioClip> _clipCache = new();
    private readonly HashSet<string> _loadingKeys = new();

    public float MasterVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool IsMuted { get; private set; }

    public void Init()
    {
        _audioRoot = new GameObject("@AudioSources");
        UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

        // BGM source
        _bgmSource = _audioRoot.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        // SFX pool
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            var src = _audioRoot.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            _sfxSources.Add(src);
        }

        LoadSettings();
        ApplyVolumes();

        GameLogger.Success("SoundManager", $"초기화 완료 (BGM x1, SFX x{SFX_POOL_SIZE})");
    }

    #region BGM
    public void PlayBGM(string addressableKey)
    {
        var clip = GetOrLoadClip(addressableKey, (loaded) =>
        {
            if (loaded != null && _bgmSource != null)
            {
                if (_bgmSource.clip == loaded && _bgmSource.isPlaying)
                    return;
                _bgmSource.clip = loaded;
                _bgmSource.Play();
            }
        });

        if (clip != null)
        {
            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (_bgmSource != null)
            _bgmSource.Stop();
    }

    public void FadeBGM(float targetVolume, float duration)
    {
        // 간단 구현: 즉시 적용 (Coroutine 없는 POCO이므로)
        if (_bgmSource != null)
            _bgmSource.volume = targetVolume * BgmVolume * MasterVolume * (IsMuted ? 0f : 1f);
    }
    #endregion

    #region SFX
    public void PlaySFX(string addressableKey)
    {
        var clip = GetOrLoadClip(addressableKey, (loaded) =>
        {
            if (loaded != null)
                PlayClipOnAvailableSource(loaded);
        });

        if (clip != null)
            PlayClipOnAvailableSource(clip);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            PlayClipOnAvailableSource(clip);
    }

    private void PlayClipOnAvailableSource(AudioClip clip)
    {
        foreach (var src in _sfxSources)
        {
            if (!src.isPlaying)
            {
                src.clip = clip;
                src.Play();
                return;
            }
        }
        // 모든 소스가 사용 중이면 첫 번째에 강제 재생
        if (_sfxSources.Count > 0)
        {
            _sfxSources[0].clip = clip;
            _sfxSources[0].Play();
        }
    }
    #endregion

    #region Volume
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        float muteMultiplier = IsMuted ? 0f : 1f;

        if (_bgmSource != null)
            _bgmSource.volume = MasterVolume * BgmVolume * muteMultiplier;

        float sfxVol = MasterVolume * SfxVolume * muteMultiplier;
        foreach (var src in _sfxSources)
            src.volume = sfxVol;
    }
    #endregion

    #region Settings Persistence
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(PREF_MASTER, MasterVolume);
        PlayerPrefs.SetFloat(PREF_BGM, BgmVolume);
        PlayerPrefs.SetFloat(PREF_SFX, SfxVolume);
        PlayerPrefs.SetInt(PREF_MUTED, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        BgmVolume = PlayerPrefs.GetFloat(PREF_BGM, 1f);
        SfxVolume = PlayerPrefs.GetFloat(PREF_SFX, 1f);
        IsMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;
    }
    #endregion

    #region Clip Loading
    private AudioClip GetOrLoadClip(string key, Action<AudioClip> onLoaded)
    {
        // 1. 자체 캐시 확인
        if (_clipCache.TryGetValue(key, out var cached))
            return cached;

        // 2. ResourceManager 캐시 확인
        var fromResource = Managers.Resource?.Load<AudioClip>(key);
        if (fromResource != null)
        {
            _clipCache[key] = fromResource;
            return fromResource;
        }

        // 3. 비동기 로드 (중복 방지)
        if (_loadingKeys.Contains(key))
            return null;

        _loadingKeys.Add(key);
        Addressables.LoadAssetAsync<AudioClip>(key).Completed += (op) =>
        {
            _loadingKeys.Remove(key);
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                _clipCache[key] = op.Result;
                onLoaded?.Invoke(op.Result);
            }
            else
            {
                GameLogger.Warning("SoundManager", $"AudioClip 로드 실패: {key}");
            }
        };

        return null;
    }
    #endregion

    public void Clear()
    {
        StopBGM();
        foreach (var src in _sfxSources)
            src.Stop();

        _clipCache.Clear();
        _loadingKeys.Clear();

        if (_audioRoot != null)
            UnityEngine.Object.Destroy(_audioRoot);
    }
}
