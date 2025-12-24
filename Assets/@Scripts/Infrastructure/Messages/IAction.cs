using System;

namespace MB.Infrastructure.Messages
{
    public enum ActionId
    {
        System_Update,
        System_LateUpdate,
        System_FixedUpdate,
        UI_OpenView,
        UI_CloseView,
        Gameplay_StartSession,
        Gameplay_EndSession,
        Network_ClientConnected,
        Network_ClientDisconnected,
        Input_PrimaryAction,
        Input_SecondaryAction,
        // ✅ 입력 이벤트 (전역 InputManager가 발행)
        Input_ArrowKey,           // BrickGame 방향키 (←/→)
        Input_PlayerMove,         // 3D 게임 WASD 이동
        Input_MouseWorldClick,    // MapEditor 월드 클릭
        Input_MouseCellClick,     // MapEditor 셀 클릭
        Input_MouseCellDrag,      // MapEditor 셀 드래그
        Input_Interact,           // F키 상호작용 (음식 서빙)
        Input_CameraBackView,     // B키 카메라
        Input_CameraTopView,      // T키 카메라
        Input_RhythmGameStart,    // Space키 리듬게임 시작
        Input_RhythmGameSkip,     // Tab키 리듬게임 스킵
        Input_RhythmGameExit,     // Esc키 리듬게임 종료
        Input_Fire,               // Space키 발사 (BrickGame 대포)
        Input_CentralMapFire,     // Enter키 발사 (땅따먹기 대포)

        // ✅ BrickGame 이벤트 (멀티플레이어 동기화)
        BrickGame_ScoreChanged,      // 점수 변경 (네트워크 동기화 필수)
        BrickGame_LevelUp,           // 레벨업 (네트워크 동기화 필수)
        BrickGame_GameStateChanged,  // 게임 상태 변경 (Playing, Paused, GameOver 등)
        BrickGame_RowSpawned,        // 새 행 생성됨
        BrickGame_BrickDestroyed,    // 벽돌 파괴됨
        BrickGame_TerritoryChanged,  // 땅따먹기 영역 변경 (멀티플레이어 경쟁)
        BrickGame_BulletFired,       // 총알 발사됨 (GameRule에서 발행)
        BrickGame_GameOver           // 게임 오버 (대포 파괴됨)
    }

    public interface IActionPayload { }

    public readonly struct NoPayload : IActionPayload
    {
        public static readonly NoPayload Instance = new NoPayload();
    }
    // ✅ 입력 관련 Payload 타입들
    /// <summary>
    /// 방향키 입력 (-1: 왼쪽, 0: 정지, 1: 오른쪽)
    /// </summary>
    public readonly struct ArrowKeyPayload : IActionPayload
    {
        public float Horizontal { get; }

        public ArrowKeyPayload(float horizontal)
        {
            Horizontal = horizontal;
        }
    }

    // ✅ BrickGame 관련 Payload 타입들
    /// <summary>
    /// 점수 변경 (점수 + 레벨 정보)
    /// </summary>
    public readonly struct BrickGameScorePayload : IActionPayload
    {
        public int Score { get; }
        public int Level { get; }

        public BrickGameScorePayload(int score, int level)
        {
            Score = score;
            Level = level;
        }
    }

    /// <summary>
    /// 레벨업 (새 레벨 정보)
    /// </summary>
    public readonly struct BrickGameLevelPayload : IActionPayload
    {
        public int Level { get; }

        public BrickGameLevelPayload(int level)
        {
            Level = level;
        }
    }

    /// <summary>
    /// 게임 상태 변경 (GamePhase enum)
    /// </summary>
    public readonly struct BrickGameStatePayload : IActionPayload
    {
        public GamePhase Phase { get; }

        public BrickGameStatePayload(GamePhase phase)
        {
            Phase = phase;
        }
    }

    /// <summary>
    /// 벽돌 파괴 (점수 획득)
    /// </summary>
    public readonly struct BrickGameBrickDestroyedPayload : IActionPayload
    {
        public int ScoreValue { get; }

        public BrickGameBrickDestroyedPayload(int scoreValue)
        {
            ScoreValue = scoreValue;
        }
    }

    /// <summary>
    /// 총알 발사 (발사한 플레이어, 발사 개수)
    /// </summary>
    public readonly struct BulletFiredPayload : IActionPayload
    {
        public ulong FiringClientId { get; }
        public int BulletCount { get; }

        public BulletFiredPayload(ulong firingClientId, int bulletCount)
        {
            FiringClientId = firingClientId;
            BulletCount = bulletCount;
        }
    }

    public readonly struct ActionMessage
    {
        public ActionId Id { get; }
        public IActionPayload Payload { get; }

        ActionMessage(ActionId id, IActionPayload payload)
        {
            Id = id;
            Payload = payload ?? NoPayload.Instance;
        }

        public static ActionMessage From(ActionId id) =>
            new ActionMessage(id, NoPayload.Instance);

        public static ActionMessage From(ActionId id, IActionPayload payload) =>
            new ActionMessage(id, payload ?? NoPayload.Instance);

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : IActionPayload
        {
            if (Payload is TPayload typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }

    public interface IAction
    {
        ActionId Id { get; }
        void Execute(ActionMessage message);
    }
}
