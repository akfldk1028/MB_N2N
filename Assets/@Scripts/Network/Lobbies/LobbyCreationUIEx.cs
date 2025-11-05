// using System;
// using UnityEngine;
// using UnityEngine.UI;
// using VContainer;


//     public class LobbyCreationUIEx : MonoBehaviour
//     {
//         [SerializeField] InputField m_LobbyNameInputField;
//         [SerializeField] GameObject m_LoadingIndicatorObject;
//         [SerializeField] Toggle m_IsPrivate;
//         [SerializeField] CanvasGroup m_CanvasGroup;
//
//         // VContainer 의존성 제거 - Initialize 패턴으로 변경
//         private LobbyUIMediatorEx m_LobbyUIMediator;
//
//         // VContainer 의존성 제거 - Initialize 패턴 구현
//         public virtual void Initialize(LobbyUIMediatorEx lobbyUIMediator)
//         {
//             m_LobbyUIMediator = lobbyUIMediator;
//         }

//         void Awake()
//         {
//             EnableUnityRelayUI();
//         }

//         void EnableUnityRelayUI()
//         {
//             m_LoadingIndicatorObject.SetActive(false);
//         }

//         public void OnCreateClick()
//         {
//             m_LobbyUIMediator.CreateLobbyRequest(m_LobbyNameInputField.text, m_IsPrivate.isOn);
//         }

//         public void Show()
//         {
//             m_CanvasGroup.alpha = 1f;
//             m_CanvasGroup.blocksRaycasts = true;
//         }

//         public void Hide()
//         {
//             m_CanvasGroup.alpha = 0f;
//             m_CanvasGroup.blocksRaycasts = false;
//         }
//     }