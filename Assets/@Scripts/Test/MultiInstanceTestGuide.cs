using UnityEngine;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 다중 인스턴스 테스트 가이드
    /// 여러 Unity 창으로 네트워크 테스트하는 방법 안내
    /// </summary>
    public class MultiInstanceTestGuide : MonoBehaviour
    {
        [Header("테스트 가이드")]
        [TextArea(10, 20)]
        public string testInstructions = @"=== 다중 창 네트워크 테스트 가이드 ===

📋 1단계: 기본 설정
1. LocalNetworkTestManager가 포함된 씬 열기
2. NetworkManager 프리팹이 씬에 있는지 확인
3. UnityTransport 컴포넌트가 NetworkManager에 있는지 확인

🖥️ 2단계: ParrelSync 사용 (추천)
1. Window → Package Manager → Git URL로 ParrelSync 설치
   https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync
2. ParrelSync → Clones Manager 열기
3. 'Add new clone' 클릭하여 Clone 생성
4. 'Open in New Editor' 클릭

🎮 3단계: 테스트 실행
- 원본 Unity: 자동으로 Host로 시작
- Clone Unity: 자동으로 Client로 연결 (3초 후)
- GUI에서 연결 상태 실시간 확인

🔧 4단계: 수동 빌드 테스트 (선택사항)
Windows:
  YourGame.exe -mode host
  YourGame.exe -mode client

Mac:
  open -n YourGame.app
  (첫 번째는 Host, 나머지는 Client로 수동 시작)

📊 5단계: 테스트 확인사항
✅ Host GUI에서 '연결된 클라이언트: 1' 표시
✅ Client GUI에서 '서버에 성공적으로 연결됨' 표시
✅ DummyPlayer 사용시 다른 색상으로 플레이어 스폰
✅ 자동 움직임 및 네트워크 동기화 확인

🚨 문제해결:
- 연결 실패: 방화벽에서 7777 포트 허용
- 컴파일 오류: Netcode for GameObjects 패키지 확인
- Clone 감지 안됨: ParrelSync 설치 확인

💡 고급 테스트:
- 여러 Clone 생성하여 다중 클라이언트 테스트
- DummyGameManager로 플레이어 스폰 테스트
- RPC 통신 테스트 (버튼 클릭)
- 성능 테스트 (다수 오브젝트 동기화)";

        [Header("테스트 설정 확인")]
        public bool hasNetworkManager = false;
        public bool hasUnityTransport = false;
        public bool hasLocalTestManager = false;
        public bool parrelSyncInstalled = false;

        private void Start()
        {
            CheckTestEnvironment();
        }

        [ContextMenu("테스트 환경 확인")]
        public void CheckTestEnvironment()
        {
            // NetworkManager 확인
            var networkManager = FindObjectOfType<Unity.Netcode.NetworkManager>();
            hasNetworkManager = networkManager != null;

            // UnityTransport 확인
            if (networkManager != null)
            {
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                hasUnityTransport = transport != null;
            }

            // LocalNetworkTestManager 확인
            var testManager = FindObjectOfType<LocalNetworkTestManager>();
            hasLocalTestManager = testManager != null;

            // ParrelSync 확인
            parrelSyncInstalled = CheckParrelSyncInstalled();

            LogTestEnvironment();
        }

        private bool CheckParrelSyncInstalled()
        {
#if UNITY_EDITOR
            try
            {
                var clonesManagerType = System.Type.GetType("ParrelSync.ClonesManager");
                return clonesManagerType != null;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private void LogTestEnvironment()
        {
            Debug.Log("=== 테스트 환경 확인 결과 ===");
            Debug.Log($"NetworkManager: {(hasNetworkManager ? "✅ 있음" : "❌ 없음")}");
            Debug.Log($"UnityTransport: {(hasUnityTransport ? "✅ 있음" : "❌ 없음")}");
            Debug.Log($"LocalNetworkTestManager: {(hasLocalTestManager ? "✅ 있음" : "❌ 없음")}");
            Debug.Log($"ParrelSync: {(parrelSyncInstalled ? "✅ 설치됨" : "❌ 미설치")}");

            if (hasNetworkManager && hasUnityTransport && hasLocalTestManager)
            {
                Debug.Log("🎉 테스트 환경 준비 완료! Play 버튼을 눌러보세요.");
            }
            else
            {
                Debug.LogWarning("⚠️ 테스트 환경이 완전하지 않습니다. 위 체크리스트를 확인하세요.");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 350, Screen.height - 200, 340, 190));

            GUILayout.Label("=== 테스트 환경 상태 ===");
            GUILayout.Label($"NetworkManager: {(hasNetworkManager ? "✅" : "❌")}");
            GUILayout.Label($"UnityTransport: {(hasUnityTransport ? "✅" : "❌")}");
            GUILayout.Label($"LocalTestManager: {(hasLocalTestManager ? "✅" : "❌")}");
            GUILayout.Label($"ParrelSync: {(parrelSyncInstalled ? "✅" : "❌")}");

            GUILayout.Space(10);

            if (GUILayout.Button("환경 재확인"))
            {
                CheckTestEnvironment();
            }

            if (GUILayout.Button("테스트 가이드 로그 출력"))
            {
                Debug.Log(testInstructions);
            }

            GUILayout.EndArea();
        }
    }
}