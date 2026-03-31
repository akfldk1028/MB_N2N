using System;
using Unity.Netcode;
using UnityEngine;

namespace MB.Infrastructure.Network
{
    /// <summary>
    /// 네트워크 오브젝트 스폰 유틸
    ///
    /// 패턴: Configure → Spawn (NV를 Spawn 전에 설정해야 Client가 올바른 초기값 수신)
    ///
    /// 사용법:
    ///   NetworkSpawnUtil.SpawnAsServer(netObj, go => {
    ///       go.GetComponent<CannonBullet>().SetOwner(cannon, color, id);
    ///       go.GetComponent<CannonBullet>().Fire(dir, speed);
    ///   });
    /// </summary>
    public static class NetworkSpawnUtil
    {
        /// <summary>
        /// 서버 권한 스폰: Configure → Spawn
        /// </summary>
        public static bool SpawnAsServer(NetworkObject netObj, Action<GameObject> configure = null)
        {
            if (!CanSpawn(netObj)) return false;

            configure?.Invoke(netObj.gameObject);
            netObj.Spawn();
            return true;
        }

        /// <summary>
        /// Owner 권한 스폰: Configure → SpawnWithOwnership
        /// </summary>
        public static bool SpawnWithOwner(NetworkObject netObj, ulong clientId, Action<GameObject> configure = null)
        {
            if (!CanSpawn(netObj)) return false;

            configure?.Invoke(netObj.gameObject);
            netObj.SpawnWithOwnership(clientId);
            return true;
        }

        /// <summary>
        /// 스폰 가능 여부 체크
        /// </summary>
        public static bool CanSpawn(NetworkObject netObj)
        {
            if (netObj == null)
            {
                GameLogger.Error("NetworkSpawnUtil", "NetworkObject가 null입니다");
                return false;
            }

            if (netObj.IsSpawned)
            {
                GameLogger.Warning("NetworkSpawnUtil", $"이미 Spawn됨: {netObj.gameObject.name}");
                return false;
            }

            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer)
            {
                GameLogger.Warning("NetworkSpawnUtil", "Server가 아닙니다");
                return false;
            }

            // 런타임 프리팹 hash=0 체크
            if (netObj.PrefabIdHash == 0)
            {
                GameLogger.Error("NetworkSpawnUtil", $"PrefabIdHash=0 (런타임 프리팹): {netObj.gameObject.name} — Client에서 찾을 수 없음");
                return false;
            }

            return true;
        }
    }
}
