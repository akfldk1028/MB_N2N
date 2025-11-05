using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// 메시지 채널 클래스 - 발행-구독(Pub/Sub) 패턴 구현
/// 제네릭 타입 T를 사용하여 다양한 메시지 타입 지원
/// </summary>
public class MessageChannel<T> : IMessageChannel<T>
{
    private const string LOG_PREFIX = "[MessageChannel]";
    readonly List<Action<T>> m_MessageHandlers = new List<Action<T>>();

    /// <summary>
    /// 구독/해제 대기 중인 핸들러를 관리하는 딕셔너리
    /// - Key: 메시지 핸들러
    /// - Value: true=구독 예정, false=구독 해제 예정
    /// 메시지 처리 도중 구독/해제 시 발생할 수 있는 문제를 방지
    /// </summary>
    readonly Dictionary<Action<T>, bool> m_PendingHandlers = new Dictionary<Action<T>, bool>();

    public bool IsDisposed { get; private set; } = false;

    /// <summary>
    /// 채널의 리소스를 정리하고 더 이상의 메시지 처리를 중단
    /// </summary>
    public virtual void Dispose()
    {
        if (!IsDisposed)
        {
            Debug.Log($"{LOG_PREFIX} Disposing channel for {typeof(T).Name}, Handlers: {m_MessageHandlers.Count}, Pending: {m_PendingHandlers.Count}");
            IsDisposed = true;
            m_MessageHandlers.Clear();
            m_PendingHandlers.Clear();
        }
    }

    /// <summary>
    /// 모든 구독자에게 메시지를 발행
    /// </summary>
    /// <param name="message">전달할 메시지</param>
       public virtual void Publish(T message)
       {
           foreach (var handler in m_PendingHandlers.Keys)
           {
               if (m_PendingHandlers[handler])
               {
                   m_MessageHandlers.Add(handler);
               }
               else
               {
                   m_MessageHandlers.Remove(handler);
               }
           }
           m_PendingHandlers.Clear();

           foreach (var messageHandler in m_MessageHandlers)
           {
               if (messageHandler != null)
               {
                   messageHandler.Invoke(message);
               }
           }
       }

    /// <summary>
    /// 새로운 구독자 등록
    /// </summary>
    /// <param name="handler">메시지를 처리할 콜백 함수</param>
    /// <returns>구독 해제에 사용할 수 있는 IDisposable 객체</returns>
       public virtual IDisposable Subscribe(Action<T> handler)
       {
           Assert.IsTrue(!IsSubscribed(handler), "Attempting to subscribe with the same handler more than once");

           if (m_PendingHandlers.ContainsKey(handler))
           {
               if (!m_PendingHandlers[handler])
               {
                   m_PendingHandlers.Remove(handler);
               }
           }
           else
           {
               m_PendingHandlers[handler] = true;
           }

           var subscription = new DisposableSubscription<T>(this, handler);
           return subscription;
       }
    /// <summary>
    /// 구독자 제거
    /// </summary>
    /// <param name="handler">제거할 메시지 핸들러</param>
    public void Unsubscribe(Action<T> handler)
    {
        Debug.Log($"{LOG_PREFIX} Unsubscribing from {typeof(T).Name}");
        if (IsSubscribed(handler))
        {
            if (m_PendingHandlers.ContainsKey(handler))
            {
                if (m_PendingHandlers[handler])
                {
                    Debug.Log($"{LOG_PREFIX} Removing pending subscribe for {typeof(T).Name}");
                    m_PendingHandlers.Remove(handler);
                }
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} Adding pending unsubscribe for {typeof(T).Name}");
                m_PendingHandlers[handler] = false;
            }
        }
    }

    /// <summary>
    /// 특정 핸들러가 현재 구독 중인지 확인
    /// </summary>
    /// <param name="handler">확인할 메시지 핸들러</param>
    /// <returns>구독 중이면 true, 아니면 false</returns>
    bool IsSubscribed(Action<T> handler)
    {
        var isPendingRemoval = m_PendingHandlers.ContainsKey(handler) && !m_PendingHandlers[handler];
        var isPendingAdding = m_PendingHandlers.ContainsKey(handler) && m_PendingHandlers[handler];
        return m_MessageHandlers.Contains(handler) && !isPendingRemoval || isPendingAdding;
    }
}