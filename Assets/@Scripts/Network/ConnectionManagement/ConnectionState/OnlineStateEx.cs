using Unity.Assets.Scripts.Network;

/// <summary>
/// Base class representing an online connection state - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// </summary>
abstract class OnlineStateEx : ConnectionStateEx
{
    public override void OnUserRequestedShutdown()
    {
        // This behaviour will be the same for every online state
        PublishConnectStatus(ConnectStatus.UserRequestedDisconnect);
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
    }

    public override void OnTransportFailure()
    {
        // This behaviour will be the same for every online state
        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
    }
}