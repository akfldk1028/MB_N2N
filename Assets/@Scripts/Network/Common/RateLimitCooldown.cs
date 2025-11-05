using UnityEngine;

/// <summary>
/// Unity Services API 호출 시 Rate Limit을 관리하는 클래스
/// 각 API 호출에 대한 쿨다운 시간을 관리하여 과도한 요청을 방지
/// </summary>
public class RateLimitCooldown
{
    private float m_CooldownTime;
    private float m_LastCallTime = 0f;
    private bool m_IsOnCooldown = false;

    public RateLimitCooldown(float cooldownTime)
    {
        m_CooldownTime = cooldownTime;
    }

    /// <summary>
    /// API 호출이 가능한지 확인
    /// </summary>
    public bool CanCall
    {
        get
        {
            if (!m_IsOnCooldown)
            {
                return true;
            }

            // 쿨다운 시간이 지났는지 확인
            if (Time.time - m_LastCallTime >= m_CooldownTime)
            {
                m_IsOnCooldown = false;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 쿨다운 상태로 설정 (API 호출 실패 시 사용)
    /// </summary>
    public void PutOnCooldown()
    {
        m_IsOnCooldown = true;
        m_LastCallTime = Time.time;
        Debug.Log($"[RateLimitCooldown] API 호출 제한 활성화 - {m_CooldownTime}초 대기");
    }

    /// <summary>
    /// 쿨다운 해제 (수동으로 재설정할 때 사용)
    /// </summary>
    public void Reset()
    {
        m_IsOnCooldown = false;
        m_LastCallTime = 0f;
        Debug.Log("[RateLimitCooldown] 쿨다운 해제됨");
    }

    /// <summary>
    /// 남은 쿨다운 시간 반환
    /// </summary>
    public float RemainingCooldownTime
    {
        get
        {
            if (!m_IsOnCooldown)
            {
                return 0f;
            }

            float elapsed = Time.time - m_LastCallTime;
            return Mathf.Max(0f, m_CooldownTime - elapsed);
        }
    }
}