using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using Unity.Services.Authentication;


    /// <summary>
    /// Data for a local lobby user instance. This will update data and is observed to know when to push local user changes to the entire lobby.
    /// </summary>
    ///
    namespace Unity.Assets.Scripts.UnityServices.Lobbies{
    [Serializable]
    public class LocalLobbyUserEx
    {
        public event Action<LocalLobbyUserEx> changed;

        public LocalLobbyUserEx()
        {
            m_UserData = new UserData(isHost: false, displayName: null, id: null);
        }

        public struct UserData
        {
            public bool IsHost { get; set; }
            public string DisplayName { get; set; }
            public string ID { get; set; }
            public bool IsReady { get; set; }

            public UserData(bool isHost, string displayName, string id, bool isReady = false)
            {
                IsHost = isHost;
                DisplayName = displayName;
                ID = id;
                IsReady = isReady;
            }
        }

        UserData m_UserData;

        public void ResetState()
        {
            m_UserData = new UserData(false, m_UserData.DisplayName, m_UserData.ID);
        }

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers
        {
            IsHost = 1,
            DisplayName = 2,
            ID = 4,
            IsReady = 8,
        }

        UserMembers m_LastChanged;

        public bool IsHost
        {
            get { return m_UserData.IsHost; }
            set
            {
                if (m_UserData.IsHost != value)
                {
                    m_UserData.IsHost = value;
                    m_LastChanged = UserMembers.IsHost;
                    OnChanged();
                }
            }
        }

        public string DisplayName
        {
            get => m_UserData.DisplayName;
            set
            {
                if (m_UserData.DisplayName != value)
                {
                    m_UserData.DisplayName = value;
                    m_LastChanged = UserMembers.DisplayName;
                    OnChanged();
                }
            }
        }

        public string ID
        {
            get => m_UserData.ID;
            set
            {
                if (m_UserData.ID != value)
                {
                    m_UserData.ID = value;
                    m_LastChanged = UserMembers.ID;
                    OnChanged();
                }
            }
        }

        public bool IsReady
        {
            get => m_UserData.IsReady;
            set
            {
                if (m_UserData.IsReady != value)
                {
                    m_UserData.IsReady = value;
                    m_LastChanged = UserMembers.IsReady;
                    OnChanged();
                }
            }
        }


        public void CopyDataFrom(LocalLobbyUserEx lobby)
        {
            var data = lobby.m_UserData;
            int lastChanged = // Set flags just for the members that will be changed.
                (m_UserData.IsHost == data.IsHost ? 0 : (int)UserMembers.IsHost) |
                (m_UserData.DisplayName == data.DisplayName ? 0 : (int)UserMembers.DisplayName) |
                (m_UserData.ID == data.ID ? 0 : (int)UserMembers.ID) |
                (m_UserData.IsReady == data.IsReady ? 0 : (int)UserMembers.IsReady);

            if (lastChanged == 0) // Ensure something actually changed.
            {
                return;
            }

            m_UserData = data;
            m_LastChanged = (UserMembers)lastChanged;

            OnChanged();
        }

        void OnChanged()
        {
            changed?.Invoke(this);
        }

        public Dictionary<string, string> GetDataForUnityServices() =>
            new Dictionary<string, string>()
            {
                {"DisplayName", DisplayName ?? ""},
                {"IsHost", IsHost.ToString()},
                {"ID", ID ?? ""},
                {"IsReady", IsReady.ToString()}
            };

        /// <summary>
        /// Creates player data dictionary for Sessions API compatibility
        /// </summary>
        public Dictionary<string, string> ToSessionsPlayerData()
        {
            return GetDataForUnityServices();
        }

        /// <summary>
        /// Updates this LocalLobbyUserEx from Sessions API player data
        /// </summary>
        public void UpdateFromSessionsPlayerData(string playerId, Dictionary<string, string> playerData)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            ID = playerId;

            if (playerData != null)
            {
                if (playerData.TryGetValue("DisplayName", out var displayName))
                    DisplayName = displayName;

                if (playerData.TryGetValue("IsHost", out var isHostStr) && bool.TryParse(isHostStr, out var isHost))
                    IsHost = isHost;

                if (playerData.TryGetValue("ID", out var id))
                    ID = id;

                if (playerData.TryGetValue("IsReady", out var isReadyStr) && bool.TryParse(isReadyStr, out var isReady))
                    IsReady = isReady;
            }
        }
    }
    }