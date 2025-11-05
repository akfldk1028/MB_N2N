// using System;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
// using UnityEngine;
// using UnityEngine.UI;
// using VContainer;


//     /// <summary>
//     /// Handles the list of LobbyListItemUIs and ensures it stays synchronized with the lobby list from the service.
//     /// VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
//     /// </summary>
//     public class LobbyJoiningUIEx : MonoBehaviour
//     {
//         [SerializeField]
//         LobbyListItemUI m_LobbyListItemPrototype;
//         [SerializeField]
//         InputField m_JoinCodeField;
//         [SerializeField]
//         CanvasGroup m_CanvasGroup;
//         [SerializeField]
//         Graphic m_EmptyLobbyListLabel;
//         [SerializeField]
//         Button m_JoinLobbyButton;
//
//         // VContainer 의존성 제거 - Initialize 패턴으로 변경
//         private LobbyUIMediatorEx m_LobbyUIMediator;
//         private UpdateRunnerEx m_UpdateRunner;
//
//         // VContainer ISubscriber 대신 직접 이벤트 구독
//         public event Action<LobbyListFetchedMessageEx> OnLobbyListFetched;
//
//         // List<LobbyListItemUI> m_LobbyListItems = new List<LobbyListItemUI>();
//
//         // VContainer 의존성 제거 - Initialize 패턴 구현
//         public virtual void Initialize(
//             LobbyUIMediatorEx lobbyUIMediator,
//             UpdateRunnerEx updateRunner)
//         {
//             m_LobbyUIMediator = lobbyUIMediator;
//             m_UpdateRunner = updateRunner;
//
//             // VContainer ISubscriber 대신 직접 이벤트 구독
//             OnLobbyListFetched += UpdateUI;
//         }

//         void Awake()
//         {
//             m_LobbyListItemPrototype.gameObject.SetActive(false);
//         }

//         void OnDisable()
//         {
//             if (m_UpdateRunner != null)
//             {
//                 m_UpdateRunner.Unsubscribe(PeriodicRefresh);
//             }
//         }

//         void OnDestroy()
//         {
//             if (OnLobbyListFetched != null)
//             {
//                 OnLobbyListFetched -= UpdateUI;
//             }
//         }

//         /// <summary>
//         /// Added to the InputField component's OnValueChanged callback for the join code text.
//         /// </summary>
//         public void OnJoinCodeInputTextChanged()
//         {
//             m_JoinCodeField.text = SanitizeJoinCode(m_JoinCodeField.text);
//             m_JoinLobbyButton.interactable = m_JoinCodeField.text.Length > 0;
//         }

//         string SanitizeJoinCode(string dirtyString)
//         {
//             return Regex.Replace(dirtyString.ToUpper(), "[^A-Z0-9]", "");
//         }

//         public void OnJoinButtonPressed()
//         {
//             m_LobbyUIMediator.JoinLobbyWithCodeRequest(SanitizeJoinCode(m_JoinCodeField.text));
//         }

//         void PeriodicRefresh(float _)
//         {
//             //this is a soft refresh without needing to lock the UI and such
//             m_LobbyUIMediator.QueryLobbiesRequest(false);
//         }

//         public void OnRefresh()
//         {
//             m_LobbyUIMediator.QueryLobbiesRequest(true);
//         }

//         void UpdateUI(LobbyListFetchedMessageEx message)
//         {
//             EnsureNumberOfActiveUISlots(message.LocalLobbies.Count);

//             for (var i = 0; i < message.LocalLobbies.Count; i++)
//             {
//                 var localLobby = message.LocalLobbies[i];
//                 // m_LobbyListItems[i].SetData(localLobby);
//             }

//             if (message.LocalLobbies.Count == 0)
//             {
//                 m_EmptyLobbyListLabel.enabled = true;
//             }
//             else
//             {
//                 m_EmptyLobbyListLabel.enabled = false;
//             }
//         }

//         void EnsureNumberOfActiveUISlots(int requiredNumber)
//         {
//             // int delta = requiredNumber - m_LobbyListItems.Count;

//             // for (int i = 0; i < delta; i++)
//             // {
//             //     m_LobbyListItems.Add(CreateLobbyListItem());
//             // }

//             // for (int i = 0; i < m_LobbyListItems.Count; i++)
//             // {
//             //     m_LobbyListItems[i].gameObject.SetActive(i < requiredNumber);
//             // }
//         }

//         LobbyListItemUI CreateLobbyListItem()
//         {
//             var listItem = Instantiate(m_LobbyListItemPrototype.gameObject, m_LobbyListItemPrototype.transform.parent)
//                 .GetComponent<LobbyListItemUI>();
//             listItem.gameObject.SetActive(true);

//             // VContainer 의존성 제거 - Initialize 패턴으로 변경
//             // m_Container.Inject(listItem);

//             return listItem;
//         }

//         public void OnQuickJoinClicked()
//         {
//             m_LobbyUIMediator.QuickJoinRequest();
//         }

//         public void Show()
//         {
//             m_CanvasGroup.alpha = 1f;
//             m_CanvasGroup.blocksRaycasts = true;
//             m_JoinCodeField.text = "";
//             m_UpdateRunner.Subscribe(PeriodicRefresh, 10f);
//         }

//         public void Hide()
//         {
//             m_CanvasGroup.alpha = 0f;
//             m_CanvasGroup.blocksRaycasts = false;
//             m_UpdateRunner.Unsubscribe(PeriodicRefresh);
//         }
//     }