using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using Unity.Services.Authentication;
using UnityEngine;


    /// <summary>
    /// A local wrapper around a lobby's remote data, with additional functionality for providing that data to UI elements and tracking local player objects.
    /// </summary>
    ///
    namespace Unity.Assets.Scripts.UnityServices.Lobbies{
    [Serializable]
    public sealed class LocalLobbyEx
    {
        public event Action<LocalLobbyEx> changed;

        /// <summary>
        /// Create a list of new LocalLobbies from a list of sessions.
        /// </summary>
        public static List<LocalLobbyEx> CreateLocalLobbies(IReadOnlyList<ISession> sessions)
        {
            var retLst = new List<LocalLobbyEx>();
            foreach (var session in sessions)
            {
                retLst.Add(Create(session));
            }
            Debug.Log($"[LocalLobbyEx] CreateLocalLobbies: {retLst.Count}");
            if (retLst.Count > 0)
                Debug.Log($"[LocalLobbyEx] CreateLocalLobbies: {retLst[0].LobbyID}");
            return retLst;
        }

        public static LocalLobbyEx Create(ISession session)
        {
            var data = new LocalLobbyEx();
            data.ApplyRemoteData(session);
            return data;
        }

        Dictionary<string, LocalLobbyUserEx> m_LobbyUsers = new Dictionary<string, LocalLobbyUserEx>();
        public Dictionary<string, LocalLobbyUserEx> LobbyUsers => m_LobbyUsers;

        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayJoinCode = existing.RelayJoinCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayJoinCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
            }
        }

        LobbyData m_Data;
        public LobbyData Data => new LobbyData(m_Data);

        public void AddUser(LocalLobbyUserEx user)
        {
            if (user == null || string.IsNullOrEmpty(user.ID))
            {
                Debug.LogWarning($"[LocalLobbyEx] Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                DoAddUser(user);
                OnChanged();
            }
        }

        void DoAddUser(LocalLobbyUserEx user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.changed += OnChangedUser;
        }

        public void RemoveUser(LocalLobbyUserEx user)
        {
            DoRemoveUser(user);
            OnChanged();
        }

        void DoRemoveUser(LocalLobbyUserEx user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"[LocalLobbyEx] Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.changed -= OnChangedUser;
        }

        void OnChangedUser(LocalLobbyUserEx user)
        {
            OnChanged();
        }

        void OnChanged()
        {
            changed?.Invoke(this);
        }

        public string LobbyID
        {
            get => m_Data.LobbyID;
            set
            {
                m_Data.LobbyID = value;
                OnChanged();
            }
        }

        public string LobbyCode
        {
            get => m_Data.LobbyCode;
            set
            {
                m_Data.LobbyCode = value;
                OnChanged();
            }
        }

        public string RelayJoinCode
        {
            get => m_Data.RelayJoinCode;
            set
            {
                m_Data.RelayJoinCode = value;
                OnChanged();
            }
        }

        public string LobbyName
        {
            get => m_Data.LobbyName;
            set
            {
                m_Data.LobbyName = value;
                OnChanged();
            }
        }

        public bool Private
        {
            get => m_Data.Private;
            set
            {
                m_Data.Private = value;
                OnChanged();
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_Data.MaxPlayerCount;
            set
            {
                m_Data.MaxPlayerCount = value;
                OnChanged();
            }
        }

        public void CopyDataFrom(LobbyData data, Dictionary<string, LocalLobbyUserEx> currUsers)
        {
            m_Data = data;

            if (currUsers == null)
            {
                m_LobbyUsers = new Dictionary<string, LocalLobbyUserEx>();
            }
            else
            {
                List<LocalLobbyUserEx> toRemove = new List<LocalLobbyUserEx>();
                foreach (var oldUser in m_LobbyUsers)
                {
                    if (currUsers.ContainsKey(oldUser.Key))
                    {
                        oldUser.Value.CopyDataFrom(currUsers[oldUser.Key]);
                    }
                    else
                    {
                        toRemove.Add(oldUser.Value);
                    }
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in currUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(currUser.Key))
                    {
                        DoAddUser(currUser.Value);
                    }
                }
            }

            OnChanged();
        }

        public Dictionary<string, string> GetDataForUnityServices() =>
            new Dictionary<string, string>()
            {
                {"RelayJoinCode", RelayJoinCode ?? ""},
                {"LobbyName", LobbyName ?? ""},
                {"MaxPlayerCount", MaxPlayerCount.ToString()},
                {"Private", Private.ToString()}
            };

        public void ApplyRemoteData(ISession session)
        {
            var info = new LobbyData(); // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            info.LobbyID = session.Id;
            info.LobbyCode = session.Code;
            info.Private = false; // Sessions API doesn't have direct private/public concept, handled through session options
            info.LobbyName = session.Name ?? session.Id;
            info.MaxPlayerCount = session.MaxPlayers;

            // Sessions API에서는 코드를 RelayJoinCode로 사용
            info.RelayJoinCode = session.Code;

            var lobbyUsers = new Dictionary<string, LocalLobbyUserEx>();
            foreach (var player in session.Players)
            {
                if (LobbyUsers.ContainsKey(player.Id))
                {
                    lobbyUsers.Add(player.Id, LobbyUsers[player.Id]);
                    continue;
                }

                // If the player isn't connected to Relay, get the most recent data that the session knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                var incomingData = new LocalLobbyUserEx
                {
                    IsHost = session.IsHost, // Sessions API에서는 현재 플레이어가 호스트인지만 확인 가능
                    DisplayName = player.Id, // Sessions API에서는 기본적으로 Player ID 사용
                    ID = player.Id
                };

                lobbyUsers.Add(incomingData.ID, incomingData);
            }

            CopyDataFrom(info, lobbyUsers);
        }

        public void Reset(LocalLobbyUserEx localUser)
        {
            CopyDataFrom(new LobbyData(), new Dictionary<string, LocalLobbyUserEx>());
            if (localUser != null && !string.IsNullOrEmpty(localUser.ID))
            {
                AddUser(localUser);
            }
        }

        /// <summary>
        /// Creates session options from this LocalLobbyEx for use with Sessions API
        /// </summary>
        public SessionOptions ToSessionOptions()
        {
            var options = new SessionOptions
            {
                MaxPlayers = MaxPlayerCount > 0 ? MaxPlayerCount : 4, // Default to 4 if not set
                Name = LobbyName
            }.WithRelayNetwork();

            return options;
        }

        /// <summary>
        /// Updates this LocalLobbyEx from a Sessions API session
        /// </summary>
        public void UpdateFromSession(ISession session)
        {
            if (session != null)
            {
                ApplyRemoteData(session);
            }
        }

    }

}