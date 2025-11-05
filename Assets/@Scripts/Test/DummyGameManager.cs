using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 더미 게임 매니저 - 플레이어 스폰 및 게임 로직 테스트
    /// </summary>
    public class DummyGameManager : NetworkBehaviour
    {
        [Header("플레이어 설정")]
        public GameObject playerPrefab;
        public Transform[] spawnPoints;
        public float spawnRadius = 2f;

        [Header("게임 설정")]
        public bool autoSpawnPlayers = true;
        public float gameTimeLimit = 300f; // 5분

        [Header("테스트 설정")]
        public bool enablePerformanceTest = false;
        public int maxTestObjects = 10;

        // 네트워크 변수들
        private NetworkVariable<float> m_GameTime = new NetworkVariable<float>();
        private NetworkVariable<int> m_PlayerCount = new NetworkVariable<int>();

        // 로컬 변수들
        private Dictionary<ulong, GameObject> m_SpawnedPlayers = new Dictionary<ulong, GameObject>();
        private List<GameObject> m_TestObjects = new List<GameObject>();
        private float m_GameStartTime;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Log("DummyGameManager 서버 시작됨");
                m_GameStartTime = Time.time;

                // 클라이언트 연결/해제 이벤트 구독
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                // 이미 연결된 클라이언트들 처리
                foreach (var client in NetworkManager.Singleton.ConnectedClients)
                {
                    OnClientConnected(client.Key);
                }
            }

            Log($"DummyGameManager 초기화됨 (IsServer: {IsServer})");
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            Log("DummyGameManager 정리됨");
        }

        private void Update()
        {
            if (IsServer)
            {
                // 게임 시간 업데이트
                m_GameTime.Value = Time.time - m_GameStartTime;
                m_PlayerCount.Value = m_SpawnedPlayers.Count;
            }
        }

        // ======== 클라이언트 연결 처리 ========

        private void OnClientConnected(ulong clientId)
        {
            Log($"클라이언트 연결됨: {clientId}");

            if (autoSpawnPlayers)
            {
                StartCoroutine(SpawnPlayerDelayed(clientId, 0.5f));
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Log($"클라이언트 연결 해제됨: {clientId}");

            // 플레이어 오브젝트 제거
            if (m_SpawnedPlayers.TryGetValue(clientId, out GameObject player))
            {
                if (player != null)
                {
                    player.GetComponent<NetworkObject>().Despawn();
                }
                m_SpawnedPlayers.Remove(clientId);
            }
        }

        private IEnumerator SpawnPlayerDelayed(ulong clientId, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnPlayer(clientId);
        }

        // ======== 플레이어 스폰 ========

        [ContextMenu("Spawn All Players")]
        public void SpawnAllPlayers()
        {
            if (!IsServer) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                SpawnPlayer(client.Key);
            }
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (!IsServer) return;

            // 이미 스폰된 플레이어는 스킵
            if (m_SpawnedPlayers.ContainsKey(clientId))
            {
                Log($"클라이언트 {clientId}의 플레이어가 이미 스폰됨");
                return;
            }

            // 플레이어 프리팹 확인
            if (playerPrefab == null)
            {
                LogError("플레이어 프리팹이 설정되지 않음!");
                return;
            }

            // 스폰 위치 결정
            Vector3 spawnPosition = GetSpawnPosition(clientId);

            // 플레이어 생성
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObject = player.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                // 클라이언트에게 소유권 부여하며 스폰
                networkObject.SpawnAsPlayerObject(clientId);
                m_SpawnedPlayers[clientId] = player;

                Log($"플레이어 스폰됨 - ClientID: {clientId}, 위치: {spawnPosition}");
            }
            else
            {
                LogError("플레이어 프리팹에 NetworkObject 컴포넌트가 없음!");
                Destroy(player);
            }
        }

        private Vector3 GetSpawnPosition(ulong clientId)
        {
            // 스폰 포인트가 설정된 경우
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
                Transform spawnPoint = spawnPoints[spawnIndex];

                if (spawnPoint != null)
                {
                    // 스폰 포인트 주변에 랜덤 위치
                    Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                    return spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                }
            }

            // 기본 스폰 위치 (원형 배치)
            float angle = (float)clientId * 360f / 8f; // 최대 8명 기준으로 원형 배치
            float radius = 5f;
            return new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );
        }

        // ======== 테스트 기능들 ========

        [ContextMenu("Test Network Performance")]
        public void TestNetworkPerformance()
        {
            if (!IsServer) return;

            Log("네트워크 성능 테스트 시작...");
            StartCoroutine(PerformanceTestCoroutine());
        }

        private IEnumerator PerformanceTestCoroutine()
        {
            for (int i = 0; i < maxTestObjects; i++)
            {
                // 테스트 오브젝트 생성 (간단한 큐브)
                GameObject testObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObj.name = $"TestObject_{i}";
                testObj.transform.position = Random.insideUnitSphere * 10f;

                // NetworkObject 추가
                NetworkObject netObj = testObj.AddComponent<NetworkObject>();
                netObj.Spawn();

                m_TestObjects.Add(testObj);

                Log($"테스트 오브젝트 생성됨: {i + 1}/{maxTestObjects}");
                yield return new WaitForSeconds(0.1f);
            }

            Log("성능 테스트 완료!");

            // 5초 후 정리
            yield return new WaitForSeconds(5f);
            CleanupTestObjects();
        }

        [ContextMenu("Cleanup Test Objects")]
        public void CleanupTestObjects()
        {
            if (!IsServer) return;

            foreach (GameObject obj in m_TestObjects)
            {
                if (obj != null)
                {
                    obj.GetComponent<NetworkObject>().Despawn();
                }
            }

            m_TestObjects.Clear();
            Log("테스트 오브젝트들 정리됨");
        }

        // ======== RPC 테스트 ========

        [Rpc(SendTo.NotServer)]
        public void BroadcastMessageRpc(string message)
        {
            Log($"브로드캐스트 메시지 받음: {message}");
        }

        [ContextMenu("Test Broadcast RPC")]
        public void TestBroadcastRpc()
        {
            if (IsServer)
            {
                BroadcastMessageRpc($"서버에서 보내는 테스트 메시지! 시간: {System.DateTime.Now:HH:mm:ss}");
            }
        }

        // ======== GUI ========

        private void OnGUI()
        {
            if (!IsSpawned) return;

            GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 400));

            GUILayout.Label("=== 더미 게임 매니저 ===");
            GUILayout.Label($"역할: {(IsServer ? "Server" : "Client")}");
            GUILayout.Label($"게임 시간: {m_GameTime.Value:F1}초");
            GUILayout.Label($"플레이어 수: {m_PlayerCount.Value}");

            if (IsServer)
            {
                GUILayout.Label($"스폰된 플레이어: {m_SpawnedPlayers.Count}");
                GUILayout.Label($"테스트 오브젝트: {m_TestObjects.Count}");

                GUILayout.Space(10);

                if (GUILayout.Button("모든 플레이어 스폰"))
                {
                    SpawnAllPlayers();
                }

                if (GUILayout.Button("성능 테스트 시작"))
                {
                    TestNetworkPerformance();
                }

                if (GUILayout.Button("테스트 오브젝트 정리"))
                {
                    CleanupTestObjects();
                }

                if (GUILayout.Button("브로드캐스트 RPC 테스트"))
                {
                    TestBroadcastRpc();
                }
            }

            // 플레이어별 정보
            GUILayout.Space(10);
            GUILayout.Label("=== 연결된 플레이어들 ===");

            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClients)
                {
                    string status = m_SpawnedPlayers.ContainsKey(client.Key) ? "스폰됨" : "대기중";
                    GUILayout.Label($"Client {client.Key}: {status}");
                }
            }

            GUILayout.EndArea();
        }

        // ======== 로깅 ========

        private void Log(string message)
        {
            Debug.Log($"[DummyGameManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[DummyGameManager] {message}");
        }
    }
}