using UnityEngine;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 더미 플레이어 - 네트워크 동기화 테스트용
    /// </summary>
    public class DummyPlayer : NetworkBehaviour
    {
        [Header("더미 플레이어 설정")]
        public float moveSpeed = 5f;
        public float rotateSpeed = 90f;
        public Color playerColor = Color.white;

        [Header("자동 움직임")]
        public bool enableAutoMovement = true;
        public float autoMoveRadius = 5f;
        public float autoMoveSpeed = 2f;

        // 네트워크 변수들
        private NetworkVariable<Vector3> m_NetworkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> m_NetworkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<Color> m_NetworkColor = new NetworkVariable<Color>();

        // 로컬 변수들
        private Vector3 m_StartPosition;
        private float m_AutoMoveTime = 0f;
        private Renderer m_Renderer;

        public override void OnNetworkSpawn()
        {
            m_StartPosition = transform.position;
            m_Renderer = GetComponent<Renderer>();

            // 서버에서만 색상 설정
            if (IsServer)
            {
                // 클라이언트마다 다른 색상 할당
                Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
                int colorIndex = (int)OwnerClientId % colors.Length;
                m_NetworkColor.Value = colors[colorIndex];

                Log($"플레이어 생성됨 - ClientID: {OwnerClientId}, 색상: {m_NetworkColor.Value}");
            }

            // 네트워크 변수 변경 이벤트 구독
            m_NetworkPosition.OnValueChanged += OnPositionChanged;
            m_NetworkRotation.OnValueChanged += OnRotationChanged;
            m_NetworkColor.OnValueChanged += OnColorChanged;

            // 초기값 적용
            ApplyNetworkState();
        }

        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            m_NetworkPosition.OnValueChanged -= OnPositionChanged;
            m_NetworkRotation.OnValueChanged -= OnRotationChanged;
            m_NetworkColor.OnValueChanged -= OnColorChanged;

            Log($"플레이어 제거됨 - ClientID: {OwnerClientId}");
        }

        private void Update()
        {
            // 서버에서만 움직임 처리
            if (IsServer)
            {
                HandleMovement();
            }
            else
            {
                // 클라이언트에서는 네트워크 상태 보간
                InterpolateToNetworkState();
            }
        }

        private void HandleMovement()
        {
            Vector3 movement = Vector3.zero;
            bool hasMoved = false;

            // 자동 움직임
            if (enableAutoMovement)
            {
                m_AutoMoveTime += Time.deltaTime * autoMoveSpeed;
                Vector3 autoPosition = m_StartPosition + new Vector3(
                    Mathf.Sin(m_AutoMoveTime) * autoMoveRadius,
                    0,
                    Mathf.Cos(m_AutoMoveTime) * autoMoveRadius
                );

                movement = (autoPosition - transform.position);
                hasMoved = movement.magnitude > 0.01f;
            }

            // 수동 조작 (Host만 가능)
            if (IsHost && IsOwner)
            {
                Vector3 input = Vector3.zero;

                if (Input.GetKey(KeyCode.W)) input += Vector3.forward;
                if (Input.GetKey(KeyCode.S)) input += Vector3.back;
                if (Input.GetKey(KeyCode.A)) input += Vector3.left;
                if (Input.GetKey(KeyCode.D)) input += Vector3.right;

                if (input.magnitude > 0.1f)
                {
                    movement = input.normalized * moveSpeed * Time.deltaTime;
                    hasMoved = true;

                    // 회전도 적용
                    if (movement.magnitude > 0.01f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(movement);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                        m_NetworkRotation.Value = transform.rotation;
                    }
                }
            }

            // 움직임 적용 및 네트워크 동기화
            if (hasMoved)
            {
                transform.position += movement;
                m_NetworkPosition.Value = transform.position;
            }
        }

        private void InterpolateToNetworkState()
        {
            // 위치 보간
            transform.position = Vector3.Lerp(transform.position, m_NetworkPosition.Value, Time.deltaTime * 10f);

            // 회전 보간
            transform.rotation = Quaternion.Lerp(transform.rotation, m_NetworkRotation.Value, Time.deltaTime * 10f);
        }

        private void ApplyNetworkState()
        {
            transform.position = m_NetworkPosition.Value;
            transform.rotation = m_NetworkRotation.Value;

            if (m_Renderer != null)
            {
                m_Renderer.material.color = m_NetworkColor.Value;
            }
        }

        // 네트워크 변수 변경 이벤트 핸들러들
        private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            // 클라이언트에서는 부드러운 보간을 위해 즉시 적용하지 않음
        }

        private void OnRotationChanged(Quaternion previousValue, Quaternion newValue)
        {
            // 클라이언트에서는 부드러운 보간을 위해 즉시 적용하지 않음
        }

        private void OnColorChanged(Color previousValue, Color newValue)
        {
            if (m_Renderer != null)
            {
                m_Renderer.material.color = newValue;
            }
        }

        // RPC 테스트용 메서드들
        [Rpc(SendTo.Server)]
        public void TestRpcToServerRpc(string message)
        {
            Log($"서버가 RPC 받음: {message} (from ClientID: {OwnerClientId})");

            // 서버에서 모든 클라이언트에게 브로드캐스트
            TestRpcToAllClientsRpc($"서버 응답: {message}");
        }

        [Rpc(SendTo.NotServer)]
        public void TestRpcToAllClientsRpc(string message)
        {
            Log($"클라이언트가 RPC 받음: {message}");
        }

        [Rpc(SendTo.Owner)]
        public void TestRpcToOwnerRpc(string message)
        {
            Log($"소유자가 RPC 받음: {message}");
        }

        // GUI에서 호출할 테스트 메서드
        public void TestRpc()
        {
            if (IsOwner)
            {
                TestRpcToServerRpc($"안녕하세요! ClientID {OwnerClientId}입니다.");
            }
        }

        // 디버그 정보 표시
        private void OnGUI()
        {
            if (!IsSpawned) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 10, 100, 20),
                    $"Client {OwnerClientId}");
            }
        }

        private void Log(string message)
        {
            Debug.Log($"[DummyPlayer] {message}");
        }
    }
}