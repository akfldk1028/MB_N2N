using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Owner Authoritative NetworkTransform
///
/// Unity 공식 권장 패턴:
/// - Player 오브젝트(Plank, Ball)에 사용
/// - Owner(Client)가 Transform 직접 제어
/// - 자동으로 Server와 다른 Client에 동기화
///
/// 사용법:
/// 1. Prefab에서 NetworkTransform 대신 이 컴포넌트 추가
/// 2. SpawnWithOwnership(clientId)로 스폰
/// 3. Owner가 transform.position 변경하면 자동 동기화
///
/// 참고: https://docs-multiplayer.unity3d.com/netcode/current/components/networktransform/
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Server Authoritative가 아닌 Owner Authoritative로 설정
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Owner(Client)가 Authority
    }
}
