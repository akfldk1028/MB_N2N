using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using Unity.Networking.Transport;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// 로비 연결 상태 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    ///
    /// 로비 연결 상태를 관리하며, 매칭 서비스를 통해 로비 생성/참가를 처리합니다.
    /// </summary>
    public class LobbyConnectingStateEx : ConnectionStateEx
    {
        // VContainer 의존성 제거
        private LobbyServiceFacadeEx m_LobbyServiceFacade;
        private LocalLobbyEx m_LocalLobby;
        private SceneManagerEx _sceneManagerEx;
        private ProfileManagerEx m_ProfileManager;

        public override void Initialize(ConnectionManagerEx connectionManager)
        {
            base.Initialize(connectionManager);

            // Managers 패턴을 통해 참조 획득
            m_LobbyServiceFacade = Managers.Lobby;
            m_LocalLobby = Managers.LocalLobby;
            _sceneManagerEx = Managers.Scene;
            m_ProfileManager = new ProfileManagerEx(); // ProfileManager는 별도로 생성
        }

        public override void Enter()
        {
            GameLogger.SystemStart("LobbyConnectingStateEx", "🔌 로비 연결 상태 진입");
        }

        public override void Exit()
        {
            GameLogger.Info("LobbyConnectingStateEx", "🔌 로비 연결 상태 종료");
        }

        public override void StartClientLobby(string playerName)
        {
            GameLogger.Progress("LobbyConnectingStateEx", $"👤 Client 연결 설정 시작: {playerName}");
            
            // ✅ 1단계: 세션 정보 검증
            if (m_LobbyServiceFacade == null)
            {
                GameLogger.Error("LobbyConnectingStateEx", "❌ LobbyServiceFacade가 null! 시스템 초기화 오류");
                PublishConnectStatus(ConnectStatus.StartClientFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                return;
            }

            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                GameLogger.Error("LobbyConnectingStateEx", "❌ 세션 정보 없음 - Lobby.QuickJoinAsync() 먼저 호출 필요");
                PublishConnectStatus(ConnectStatus.StartClientFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                return;
            }
            
            GameLogger.Success("LobbyConnectingStateEx", $"✅ 세션 참가 확인됨 (세션 ID: {m_LobbyServiceFacade.CurrentUnityLobby.Id})");

            // ✅ 2단계: ConnectionMethod 설정
            GameLogger.Info("LobbyConnectingStateEx", "⚙️ ConnectionMethod 설정 중...");
            var connectionMethod = new ConnectionMethodRelayEx(
                m_LobbyServiceFacade,
                m_LocalLobby,
                m_ConnectionManager,
                m_ProfileManager,
                playerName);

            // ✅ 3단계: ClientConnecting 상태로 전환
            GameLogger.Progress("LobbyConnectingStateEx", "🔄 ClientConnecting 상태로 전환");
            var clientConnectingState = m_ConnectionManager.m_ClientConnecting.Configure(connectionMethod);
            m_ConnectionManager.ChangeState(clientConnectingState);
        }

        public override void StartHostLobby(string playerName)
        {
            GameLogger.Progress("LobbyConnectingStateEx", $"👑 Host 연결 설정 시작: {playerName}");
            
            // ✅ 1단계: 세션 정보 검증
            if (m_LobbyServiceFacade == null)
            {
                GameLogger.Error("LobbyConnectingStateEx", "❌ LobbyServiceFacade가 null! 시스템 초기화 오류");
                PublishConnectStatus(ConnectStatus.StartHostFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                return;
            }

            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                GameLogger.Error("LobbyConnectingStateEx", "❌ 세션 정보 없음 - Lobby.CreateLobbyAsync() 먼저 호출 필요");
                PublishConnectStatus(ConnectStatus.StartHostFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                return;
            }
            
            GameLogger.Success("LobbyConnectingStateEx", $"✅ 세션 생성 확인됨 (세션 ID: {m_LobbyServiceFacade.CurrentUnityLobby.Id})");

            // ✅ 2단계: ConnectionMethod 설정
            GameLogger.Info("LobbyConnectingStateEx", "⚙️ ConnectionMethod 설정 중...");
            var connectionMethod = new ConnectionMethodRelayEx(
                m_LobbyServiceFacade,
                m_LocalLobby,
                m_ConnectionManager,
                m_ProfileManager,
                playerName);

            // ✅ 3단계: StartingHost 상태로 전환
            GameLogger.Progress("LobbyConnectingStateEx", "🔄 StartingHost 상태로 전환");
            var startingHostState = m_ConnectionManager.m_StartingHost.Configure(connectionMethod);
            m_ConnectionManager.ChangeState(startingHostState);
        }
    }
}