#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [USK] MPPM 클론이 Play 진입 시 자동으로 Start 버튼 클릭.
/// 메인 에디터에서는 동작하지 않음.
///
/// ★ 새 프로젝트 적용 시: ButtonName을 해당 프로젝트의 시작 버튼 이름으로 변경
/// </summary>
public class USK_MPPMCloneAutoStart : MonoBehaviour
{
    private static USK_MPPMCloneAutoStart _instance;

    /// <summary>자동 클릭할 버튼 이름 (프로젝트마다 변경)</summary>
    private const string ButtonName = "StartButton";

    private float _pollInterval = 0.5f;
    private float _timeout = 30f;
    private float _elapsed;
    private float _pollTimer;
    private bool _clicked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
            return;
        if (_instance != null) return;

        var go = new GameObject("[USK_CloneAutoStart]");
        _instance = go.AddComponent<USK_MPPMCloneAutoStart>();
        DontDestroyOnLoad(go);
        Debug.Log($"[USK_CloneAutoStart] 클론 감지 - '{ButtonName}' 자동 클릭 대기...");
    }

    private void Update()
    {
        if (_clicked) return;

        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed > _timeout)
        {
            Debug.LogWarning($"[USK_CloneAutoStart] 타임아웃 ({_timeout}초) - '{ButtonName}' 못 찾음");
            Destroy(gameObject);
            return;
        }

        _pollTimer += Time.unscaledDeltaTime;
        if (_pollTimer < _pollInterval) return;
        _pollTimer = 0f;

        var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == ButtonName && btn.gameObject.activeInHierarchy && btn.interactable)
            {
                if (EventSystem.current == null) return;
                Debug.Log($"[USK_CloneAutoStart] '{ButtonName}' 발견 → 클릭!");
                ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                _clicked = true;
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
