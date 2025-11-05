using System.Threading.Tasks;
using Unity.Assets.Scripts.Network.Interfaces;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Unity Authentication Service를 관리하는 클래스
/// 플레이어 인증 및 로그인 상태를 관리
/// </summary>
public class AuthManager : MonoBehaviour, Unity.Assets.Scripts.Network.Interfaces.IAuthenticationService
{
    private bool m_IsInitialized = false;
    private bool m_IsSigningIn = false;

    /// <summary>
    /// 플레이어가 인증되었는지 확인하고, 필요하면 인증 처리
    /// </summary>
    public async Task<bool> EnsurePlayerIsAuthorized()
    {
        if (m_IsSigningIn)
        {
            Debug.LogWarning("[AuthManager] 이미 로그인 진행 중입니다.");
            return false;
        }

        // Unity Services 초기화 확인
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.LogWarning("[AuthManager] Unity Services가 초기화되지 않았습니다.");
            return false;
        }

        // 이미 인증된 경우
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log($"[AuthManager] 이미 인증됨 - Player ID: {AuthenticationService.Instance.PlayerId}");
            return true;
        }

        // 익명 로그인 시도
        return await TrySignInAnonymously();
    }

    /// <summary>
    /// 익명 로그인 시도
    /// </summary>
    private async Task<bool> TrySignInAnonymously()
    {
        try
        {
            m_IsSigningIn = true;
            Debug.Log("[AuthManager] 익명 로그인 시도 중...");

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"[AuthManager] 익명 로그인 성공 - Player ID: {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[AuthManager] 인증 실패: {ex.Message}");
            return false;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[AuthManager] 요청 실패: {ex.Message}");
            return false;
        }
        finally
        {
            m_IsSigningIn = false;
        }
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[AuthManager] 로그아웃");
            AuthenticationService.Instance.SignOut();
        }
    }

    /// <summary>
    /// 현재 플레이어 ID 반환
    /// </summary>
    public string GetPlayerId()
    {
        return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
    }

    /// <summary>
    /// 인증 상태 확인
    /// </summary>
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

    #region IAuthenticationService Implementation

    public bool IsInitialized => m_IsInitialized;

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        // Unity Services가 초기화되었는지 확인
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            m_IsInitialized = true;
            Debug.Log("[AuthManager] Authentication Service 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[AuthManager] Unity Services가 아직 초기화되지 않음");
        }

        await Task.CompletedTask;
    }

    public void Shutdown()
    {
        SignOut();
        m_IsInitialized = false;
    }

    public string PlayerId => GetPlayerId();

    #endregion
}