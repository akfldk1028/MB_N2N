using System;
using System.Collections;
using UnityEngine;
using Unity.Assets.Scripts.Network;

namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// Connection state corresponding to a client attempting to reconnect to a server - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    /// It will try to reconnect a number of times defined by the ConnectionManager's NbReconnectAttempts property.
    /// If it succeeds, it will transition to the ClientConnected state. If not, it will transition to the Offline state.
    /// </summary>
    class ClientReconnectingStateEx : ClientConnectingStateEx
{
    // VContainer IPublisher 제거
    Coroutine m_ReconnectCoroutine;
    int m_NbAttempts;

    const float k_TimeBeforeFirstAttempt = 1;
    const float k_TimeBetweenAttempts = 5;

    public override void Enter()
    {
        m_NbAttempts = 0;
        m_ReconnectCoroutine = m_ConnectionManager.StartCoroutine(ReconnectCoroutine());
    }

    public override void Exit()
    {
        if (m_ReconnectCoroutine != null)
        {
            m_ConnectionManager.StopCoroutine(m_ReconnectCoroutine);
            m_ReconnectCoroutine = null;
        }
        base.Exit();
    }

    private IEnumerator ReconnectCoroutine()
    {
        Debug.Log("[ClientReconnectingStateEx] 재연결 시도 시작");

        yield return new WaitForSeconds(k_TimeBeforeFirstAttempt);

        while (m_NbAttempts < m_ConnectionManager.NbReconnectAttempts)
        {
            m_NbAttempts++;

            Debug.Log($"[ClientReconnectingStateEx] 재연결 시도 {m_NbAttempts}/{m_ConnectionManager.NbReconnectAttempts}");

            // VContainer IPublisher 대신 직접 호출
            var reconnectMessage = new ReconnectMessage(m_NbAttempts, m_ConnectionManager.NbReconnectAttempts);
            Debug.Log($"[ClientReconnectingStateEx] ReconnectMessage: {m_NbAttempts}/{m_ConnectionManager.NbReconnectAttempts}");

            bool reconnectSuccess = false;
            bool shouldContinue = true;

            if (m_ConnectionMethod != null)
            {
                // async 메서드를 동기적으로 실행
                var setupTask = m_ConnectionMethod.SetupClientReconnectionAsync();
                yield return new WaitUntil(() => setupTask.IsCompleted);

                if (setupTask.IsCompletedSuccessfully)
                {
                    var (success, shouldTryAgain) = setupTask.Result;
                    if (!success)
                    {
                        if (!shouldTryAgain)
                        {
                            Debug.Log("[ClientReconnectingStateEx] 재연결 중단 - shouldTryAgain이 false");
                            shouldContinue = false;
                        }
                        else
                        {
                            Debug.Log("[ClientReconnectingStateEx] 재연결 설정 실패, 다시 시도");
                            yield return new WaitForSeconds(k_TimeBetweenAttempts);
                            continue;
                        }
                    }
                    else
                    {
                        reconnectSuccess = true;
                    }
                }
            }

            if (!shouldContinue) break;
            if (!reconnectSuccess) continue;

            try
            {
                bool startResult = m_ConnectionManager.NetworkManager.StartClient();
                if (startResult)
                {
                    Debug.Log("[ClientReconnectingStateEx] 재연결 성공");
                    yield break; // 성공하면 코루틴 종료
                }
                else
                {
                    Debug.LogWarning("[ClientReconnectingStateEx] StartClient 실패");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ClientReconnectingStateEx] 재연결 중 오류: {e.Message}");
            }

            if (m_NbAttempts < m_ConnectionManager.NbReconnectAttempts)
            {
                yield return new WaitForSeconds(k_TimeBetweenAttempts);
            }
        }

        PublishConnectStatus(ConnectStatus.GenericDisconnect);
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
    }
    }
}