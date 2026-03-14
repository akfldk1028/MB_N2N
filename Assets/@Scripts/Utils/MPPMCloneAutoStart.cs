#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MPPM 클론이 Play 진입 시 자동으로 Start 버튼 클릭.
/// 메인 에디터에서는 동작하지 않음.
/// </summary>
public class MPPMCloneAutoStart : MonoBehaviour
{
    private static MPPMCloneAutoStart _instance;

    private float _pollInterval = 0.5f;
    private float _timeout = 30f;
    private float _elapsed;
    private float _pollTimer;
    private bool _clicked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        // 메인 에디터에서는 동작하지 않음
        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
            return;

        if (_instance != null) return;
        var go = new GameObject("[MPPMCloneAutoStart]");
        _instance = go.AddComponent<MPPMCloneAutoStart>();
        DontDestroyOnLoad(go);
        Debug.Log("[MPPMCloneAutoStart] 클론 감지 - Start 버튼 자동 클릭 대기 중...");
    }

    private void Update()
    {
        if (_clicked) return;

        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed > _timeout)
        {
            Debug.LogWarning("[MPPMCloneAutoStart] 타임아웃 (30초) - Start 버튼을 찾지 못함");
            Destroy(gameObject);
            return;
        }

        _pollTimer += Time.unscaledDeltaTime;
        if (_pollTimer < _pollInterval) return;
        _pollTimer = 0f;

        TryClickStartButton();
    }

    private void TryClickStartButton()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == "StartButton" && btn.gameObject.activeInHierarchy && btn.interactable)
            {
                if (EventSystem.current == null)
                {
                    Debug.LogWarning("[MPPMCloneAutoStart] EventSystem 없음 - 다음 폴링에서 재시도");
                    return;
                }
                Debug.Log("[MPPMCloneAutoStart] StartButton 발견 - 클릭!");
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(btn.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                _clicked = true;
                Debug.Log("[MPPMCloneAutoStart] 클릭 완료, 자동 파괴");
                Destroy(gameObject);
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
#endif
