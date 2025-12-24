using UnityEngine;

/// <summary>
/// 맵 인터페이스
/// 모든 맵 타입이 구현해야 하는 공통 API
///
/// 사용 예:
/// - IsometricGridGenerator (현재 땅따먹기 맵)
/// - 향후 FourPlayerMap, TeamBattleMap 등
/// </summary>
public interface IMap
{
    #region 블록 소유권 관리

    /// <summary>
    /// 블록 소유자 ID 가져오기
    /// </summary>
    int GetBlockOwner(GameObject block);

    /// <summary>
    /// 블록 소유권 변경
    /// </summary>
    bool SetBlockOwner(GameObject block, int playerID, Color playerColor);

    /// <summary>
    /// 블록 소유권 로컬 변경 (네트워크 동기화 없이)
    /// </summary>
    void SetBlockOwnerLocal(GameObject block, int playerID, Color playerColor);

    /// <summary>
    /// 블록 이름으로 찾기
    /// </summary>
    GameObject FindBlockByName(string blockName);

    #endregion

    #region 통계

    /// <summary>
    /// 특정 플레이어의 블록 수
    /// </summary>
    int GetBlockCountByPlayer(int playerID);

    /// <summary>
    /// 전체 블록 수
    /// </summary>
    int GetTotalBlockCount();

    #endregion

    #region 플레이어

    /// <summary>
    /// 플레이어 색상 가져오기
    /// </summary>
    Color GetPlayerColor(int playerID);

    #endregion

    #region 캐논

    /// <summary>
    /// 모든 캐논 가져오기
    /// </summary>
    Cannon[] GetAllCannons();

    /// <summary>
    /// 씬에서 캐논 찾아서 갱신 (CLIENT용)
    /// </summary>
    void RefreshCannonsFromScene();

    #endregion
}
