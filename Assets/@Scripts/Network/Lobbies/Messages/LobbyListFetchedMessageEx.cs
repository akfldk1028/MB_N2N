using System.Collections.Generic;
using Unity.Assets.Scripts.UnityServices.Lobbies;

    public struct LobbyListFetchedMessageEx
    {
        public readonly IReadOnlyList<LocalLobbyEx> LocalLobbies;

        public LobbyListFetchedMessageEx(List<LocalLobbyEx> localLobbies)
        {
            LocalLobbies = localLobbies;
        }
    }