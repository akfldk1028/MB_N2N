using System;
using UnityEngine;

/// <summary>
/// 연결 이벤트 메시지 구조체
/// 클라이언트 연결/해제 시 발생하는 이벤트 정보를 담음
/// </summary>
[Serializable]
public struct ConnectionEventMessage
{
    public ulong ClientId;
    public ConnectStatus ConnectStatus;
    public string Reason;

    public ConnectionEventMessage(ulong clientId, ConnectStatus connectStatus, string reason = "")
    {
        ClientId = clientId;
        ConnectStatus = connectStatus;
        Reason = reason;
    }
}

/// <summary>
/// 연결 페이로드 구조체
/// 클라이언트가 서버에 연결할 때 전송하는 데이터
/// </summary>
[Serializable]
public struct ConnectionPayload
{
    public string playerName;
    public string playerGuid;
    public int buildVersion;
    public bool isDebugConnection;

    public ConnectionPayload(string name, string guid, int version = 1, bool debug = false)
    {
        playerName = name;
        playerGuid = guid;
        buildVersion = version;
        isDebugConnection = debug;
    }
}