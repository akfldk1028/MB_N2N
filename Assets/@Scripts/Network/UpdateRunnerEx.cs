using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

    /// <summary>
    /// 일반적인 MonoBehaviour Update()보다 더 느리거나 정교하지 않은 주기로 업데이트가 필요한 객체들이,
    /// 혹은 Unity의 GameObject와 강하게 결합되지 않고도 Update 루프가 필요할 때 사용되는 클래스.
    /// (서비스 데이터 갱신 등 주기적 업데이트가 필요한 로직을 대신 처리해주는 역할)
    ///
    /// VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    /// </summary>

namespace Unity.Assets.Scripts.Network
{
    // TODO: [개선필요] Managers에서 중앙 관리
    // 현재: 독립적인 MonoBehaviour
    // 개선: Managers.UpdateRunner로 접근
    //
    // Managers.cs:
    // private UpdateRunnerEx _updateRunner;
    // public static UpdateRunnerEx UpdateRunner => Instance?._updateRunner;
    //
    // 초기화:
    // GameObject updateGo = new GameObject("UpdateRunner");
    // _updateRunner = updateGo.AddComponent<UpdateRunnerEx>();
    // DontDestroyOnLoad(updateGo);

    public class UpdateRunnerEx : MonoBehaviour
    {
        // 구독된 함수(Subscriber)에 대한 메타데이터를 담는 내부 클래스
        class SubscriberData
        {
            public float Period;         // 해당 구독자가 호출될 주기(초 단위)
            public float NextCallTime;   // 다음번에 호출될 시간 (Time.time과 비교)
            public float LastCallTime;   // 마지막으로 호출된 시점(Time.time)
        }

        // 메인 스레드에서 처리해야 할 구독/해제 연산을 모아두는 대기열 (FIFO)
        readonly Queue<Action> m_PendingHandlers = new Queue<Action>();

        // 실제로 업데이트 루프에 등록된 구독자들의 집합
        readonly HashSet<Action<float>> m_Subscribers = new HashSet<Action<float>>();

        // 각 구독자(콜백)마다 SubscriberData(주기, 다음 호출 시점 등)를 저장하는 딕셔너리
        readonly Dictionary<Action<float>, SubscriberData> m_SubscriberData = new Dictionary<Action<float>, SubscriberData>();

        /// <summary>
        /// Unity 오브젝트 파괴 시점에 호출된다.
        /// 모든 자료구조를 정리하여 누수나 참조 문제를 방지.
        /// </summary>
        public void OnDestroy()
        {
            Debug.Log("[UpdateRunnerEx] 시작: OnDestroy - 리소스 정리");

            // ResourceManager.Instance.ResetAllScriptableObjects();
            m_PendingHandlers.Clear();
            m_Subscribers.Clear();
            m_SubscriberData.Clear();
            Debug.Log("[UpdateRunnerEx] 종료: OnDestroy");
        }

        /// <summary>
        /// 특정 함수를 일정 주기로 호출되도록 구독한다.
        /// period가 0 이하이면 매 프레임마다 호출된다.
        /// </summary>
        /// <param name="onUpdate">구독할 콜백 함수(델리게이트)</param>
        /// <param name="updatePeriod">호출 주기(초). 0 이하이면 매 프레임 호출.</param>
        public void Subscribe(Action<float> onUpdate, float updatePeriod)
        {
            if (onUpdate == null)
            {
                Debug.Log("[UpdateRunnerEx] Subscribe 실패: null 콜백");
                return;
            }

            if (onUpdate.Target == null)
            {
                Debug.LogError("[UpdateRunnerEx] Subscribe 실패: 범위를 벗어난 로컬 함수");
                return;
            }

            if (onUpdate.Method.ToString().Contains("<"))
            {
                Debug.LogError("[UpdateRunnerEx] Subscribe 실패: 익명 함수");
                return;
            }

            if (!m_Subscribers.Contains(onUpdate))
            {
                Debug.Log($"[UpdateRunnerEx] Subscribe 요청: {onUpdate.Method.Name}, 주기={updatePeriod}초");
                m_PendingHandlers.Enqueue(() =>
                {
                    if (m_Subscribers.Add(onUpdate))
                    {
                        Debug.Log($"[UpdateRunnerEx] Subscribe 성공: {onUpdate.Method.Name}");
                        m_SubscriberData.Add(onUpdate, new SubscriberData()
                        {
                            Period = updatePeriod,
                            NextCallTime = 0,
                            LastCallTime = Time.time
                        });
                    }
                });
            }
            else
            {
                Debug.Log($"[UpdateRunnerEx] Subscribe 무시: {onUpdate.Method.Name} (이미 등록됨)");
            }
        }

        /// <summary>
        /// 특정 함수 구독을 해제한다.
        /// 만약 이전에 Subscribe되지 않았던 함수라도 안전하게 처리 가능.
        /// </summary>
        /// <param name="onUpdate">해제할 콜백 함수(델리게이트)</param>
        public void Unsubscribe(Action<float> onUpdate)
        {
            Debug.Log($"[UpdateRunnerEx] Unsubscribe 요청: {onUpdate?.Method.Name ?? "null"}");
            m_PendingHandlers.Enqueue(() =>
            {
                bool removed = m_Subscribers.Remove(onUpdate);
                m_SubscriberData.Remove(onUpdate);
                Debug.Log($"[UpdateRunnerEx] Unsubscribe {(removed ? "성공" : "실패")}: {onUpdate?.Method.Name ?? "null"}");
            });
        }

        /// <summary>
        /// Unity의 매 프레임 Update()마다 호출된다.
        /// 1) 구독/해제 등 대기열(m_PendingHandlers)에 쌓인 작업을 먼저 처리
        /// 2) 각 구독자별로 '지정된 주기'가 지났으면 콜백 호출
        /// </summary>
        void Update()
        {
            // 대기 중인 작업 처리
            if (m_PendingHandlers.Count > 0)
            {
                Debug.Log($"[UpdateRunnerEx] 대기 작업 처리 시작: {m_PendingHandlers.Count}개");
                while (m_PendingHandlers.Count > 0)
                {
                    m_PendingHandlers.Dequeue()?.Invoke();
                }
                Debug.Log("[UpdateRunnerEx] 대기 작업 처리 완료");
            }

            // 구독자 업데이트
            foreach (var subscriber in m_Subscribers)
            {
                var subscriberData = m_SubscriberData[subscriber];
                if (Time.time >= subscriberData.NextCallTime)
                {
                    float deltaTime = Time.time - subscriberData.LastCallTime;
                    Debug.Log($"[UpdateRunnerEx] 구독자 호출: {subscriber.Method.Name}, deltaTime={deltaTime:F3}초");

                    subscriber.Invoke(deltaTime);

                    subscriberData.LastCallTime = Time.time;
                    subscriberData.NextCallTime = Time.time + subscriberData.Period;
                    Debug.Log($"[UpdateRunnerEx] 다음 호출 예정: {subscriber.Method.Name}, {subscriberData.NextCallTime:F3}초 후");
                }
            }
        }
    }
}