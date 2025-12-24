using UnityEngine;

/// <summary>
/// 맵에 붙는 컴포넌트 인터페이스
/// BOMB, HARVEST, MULTIPLY 같은 특수 기능들이 구현
///
/// 사용 예:
/// - BombComponent : MonoBehaviour, IMapComponent
/// - HarvestComponent : MonoBehaviour, IMapComponent
/// - MultiplyComponent : MonoBehaviour, IMapComponent
/// </summary>
public interface IMapComponent
{
    #region 식별

    /// <summary>
    /// 컴포넌트 고유 ID (예: "bomb", "harvest", "multiply")
    /// </summary>
    string ComponentID { get; }

    /// <summary>
    /// UI 표시명 (예: "BOMB", "HARVEST", "x2")
    /// </summary>
    string DisplayName { get; }

    #endregion

    #region 소유권

    /// <summary>
    /// 소유 플레이어 ID (-1 = 중립/공용)
    /// </summary>
    int OwnerPlayerID { get; set; }

    #endregion

    #region 상태

    /// <summary>
    /// 활성화 상태
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 사용 가능 여부 (쿨다운, 조건 등)
    /// </summary>
    bool CanUse { get; }

    #endregion

    #region 생명주기

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    void Initialize(IMap map, int ownerPlayerID);

    /// <summary>
    /// 컴포넌트 활성화
    /// </summary>
    void Activate();

    /// <summary>
    /// 컴포넌트 비활성화
    /// </summary>
    void Deactivate();

    /// <summary>
    /// 컴포넌트 사용 (버튼 눌렀을 때)
    /// </summary>
    void Use();

    #endregion

    #region 이벤트 콜백

    /// <summary>
    /// 블록 점령 시 호출
    /// </summary>
    void OnBlockCaptured(GameObject block, int oldOwnerID, int newOwnerID);

    /// <summary>
    /// 매 프레임 업데이트 (선택적 구현)
    /// </summary>
    void OnTick(float deltaTime);

    #endregion
}

/// <summary>
/// 쿨다운이 있는 액티브 컴포넌트
/// </summary>
public interface IActivatableComponent : IMapComponent
{
    /// <summary>
    /// 쿨다운 시간 (초)
    /// </summary>
    float Cooldown { get; }

    /// <summary>
    /// 남은 쿨다운 시간
    /// </summary>
    float RemainingCooldown { get; }
}

/// <summary>
/// 충전식 컴포넌트 (HARVEST처럼 자원 모으는 타입)
/// </summary>
public interface IChargeableComponent : IMapComponent
{
    /// <summary>
    /// 현재 충전량
    /// </summary>
    int CurrentCharge { get; }

    /// <summary>
    /// 최대 충전량
    /// </summary>
    int MaxCharge { get; }

    /// <summary>
    /// 충전 추가
    /// </summary>
    void AddCharge(int amount);
}
