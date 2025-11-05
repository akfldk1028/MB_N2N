using UnityEngine;

/// <summary>
/// 클라이언트 설정을 저장/로드하는 헬퍼 클래스
/// Unity PlayerPrefs를 사용한 간단한 설정 관리
/// </summary>
public static class ClientPrefs
{
    private const string PROFILES_KEY = "AvailableProfiles";
    private const string LAST_PROFILE_KEY = "LastProfile";

    /// <summary>
    /// 사용 가능한 프로필 목록 저장
    /// </summary>
    public static void SetAvailableProfiles(string profiles)
    {
        PlayerPrefs.SetString(PROFILES_KEY, profiles);
        PlayerPrefs.Save();
        Debug.Log($"[ClientPrefs] 프로필 목록 저장됨: {profiles}");
    }

    /// <summary>
    /// 사용 가능한 프로필 목록 로드
    /// </summary>
    public static string GetAvailableProfiles()
    {
        string profiles = PlayerPrefs.GetString(PROFILES_KEY, "");
        Debug.Log($"[ClientPrefs] 프로필 목록 로드됨: {profiles}");
        return profiles;
    }

    /// <summary>
    /// 마지막 사용한 프로필 저장
    /// </summary>
    public static void SetLastProfile(string profile)
    {
        PlayerPrefs.SetString(LAST_PROFILE_KEY, profile);
        PlayerPrefs.Save();
        Debug.Log($"[ClientPrefs] 마지막 프로필 저장됨: {profile}");
    }

    /// <summary>
    /// 마지막 사용한 프로필 로드
    /// </summary>
    public static string GetLastProfile()
    {
        string profile = PlayerPrefs.GetString(LAST_PROFILE_KEY, "");
        Debug.Log($"[ClientPrefs] 마지막 프로필 로드됨: {profile}");
        return profile;
    }

    /// <summary>
    /// 모든 설정 초기화
    /// </summary>
    public static void ClearAll()
    {
        PlayerPrefs.DeleteKey(PROFILES_KEY);
        PlayerPrefs.DeleteKey(LAST_PROFILE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[ClientPrefs] 모든 설정 초기화됨");
    }

    /// <summary>
    /// 고유한 클라이언트 GUID 생성/반환
    /// 디바이스별로 고유한 ID를 생성하여 저장
    /// </summary>
    public static string GetGuid()
    {
        const string GUID_KEY = "ClientGUID";

        string guid = PlayerPrefs.GetString(GUID_KEY, "");
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(GUID_KEY, guid);
            PlayerPrefs.Save();
            Debug.Log($"[ClientPrefs] 새 GUID 생성됨: {guid}");
        }

        return guid;
    }
}