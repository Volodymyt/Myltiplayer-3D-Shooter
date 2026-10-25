using System;
using System.Threading;
using System.Threading.Tasks;
using Edgegap;
using Mirror;
using UnityEngine;
using ConnectionState = Edgegap.ConnectionState;

namespace Services.Networking
{
    public class NetworkConnectionService
    {
        private readonly IEdgegapRelayService _relayService;
        private readonly RelayPicker _relayPicker;
        private readonly RelayDiagnostics _diagnostics;

        public event Action<string> RoomCodeReady;
        public event Action<string> ConnectionLost;

        private NetworkManager _networkManager;
        private CancellationTokenSource _cancel;
        private string _sessionId;
        private uint _myToken;
        private bool _isHost;
        private bool _sessionJoined;
        private bool _connectedToHost;

        public NetworkConnectionService(
            IEdgegapRelayService relayService,
            RelayPicker relayPicker,
            RelayDiagnostics diagnostics)
        {
            _relayService = relayService;
            _relayPicker = relayPicker;
            _diagnostics = diagnostics;
        }

        public async Task HostAsync(NetworkManager networkManager)
        {
            if (!TryBegin(networkManager, out var transport, out var cancel))
                return;

            try
            {
                var myIp = await _relayService.GetPublicIpAsync();
                var picked = await _relayPicker.PickForHostAsync(myIp, cancel);

                if (picked == null)
                {
                    Fail("No Edgegap server confirmed the room right now. Please try again a bit later.");
                    return;
                }

                var (session, token) = picked.Value;
                if (cancel.IsCancellationRequested)
                {
                    _ = _relayService.DeleteSessionAsync(session.session_id);
                    return;
                }

                transport.ApplyRelaySession(session, token, true);
                Remember(session.session_id, token, true);

                _networkManager.StartHost();
                await VerifyHostAsync(transport, session);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Failed to host via Edgegap relay: {e}");
                Fail("Couldn't create the room. Check your internet connection and try again.");
            }
        }

        public async Task JoinAsync(NetworkManager networkManager, string joinCode)
        {
            if (!TryBegin(networkManager, out var transport, out var cancel))
                return;

            joinCode = joinCode?.Trim();
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogWarning("[Relay] Join code is required to connect as a client.");
                Fail("Enter the room code to connect.");
                return;
            }

            try
            {
                var myIp = await _relayService.GetPublicIpAsync();
                var me = await _relayService.AuthorizeUserAsync(joinCode, myIp);
                var session = await _relayService.WaitUntilReadyAsync(joinCode);

                if (cancel.IsCancellationRequested)
                    return;

                transport.ApplyRelaySession(session, me.authorization_token, false);
                Remember(session.session_id, me.authorization_token, false);

                _networkManager.StartClient();
                NetworkClient.OnConnectedEvent += HandleClientConnected;
                NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Failed to join via Edgegap relay: {e}");
                Fail("Couldn't find the room. Check the code and try again.");
            }
        }

        public Task LeaveSessionAsync()
        {
            if (!_sessionJoined || string.IsNullOrEmpty(_sessionId))
                return Task.CompletedTask;

            _sessionJoined = false;
            return LeaveRelaySessionAsync(_sessionId, _isHost, _myToken);
        }

        public void Stop()
        {
            NetworkClient.OnConnectedEvent -= HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
            _cancel?.Cancel();

            if (_sessionJoined)
                _ = LeaveSessionAsync();

            if (_networkManager != null)
            {
                if (NetworkServer.active)
                    _networkManager.StopHost();
                else if (NetworkClient.active)
                    _networkManager.StopClient();
            }

            _networkManager = null;
        }

        private bool TryBegin(NetworkManager networkManager, out EdgegapKcpTransport transport,
            out CancellationToken cancel)
        {
            _networkManager = networkManager;
            _connectedToHost = false;
            _cancel = new CancellationTokenSource();
            cancel = _cancel.Token;

            transport = networkManager.GetComponent<EdgegapKcpTransport>();
            if (transport != null)
                return true;

            Debug.LogError("[Relay] EdgegapKcpTransport not found on NetworkManager GameObject.");
            Fail("Network setup error: the Edgegap transport is missing.");
            return false;
        }

        private void Remember(string sessionId, uint token, bool isHost)
        {
            _sessionId = sessionId;
            _myToken = token;
            _isHost = isHost;
            _sessionJoined = true;
        }

        private async Task VerifyHostAsync(EdgegapKcpTransport transport, RelaySession session)
        {
            var server = transport.GetRelayServer();
            if (server == null)
            {
                Debug.LogWarning("[Relay] Can't read relay state from the transport, skipping the check.");
                RoomCodeReady?.Invoke(session.session_id);
                return;
            }

            var state = await WaitForHostValidationAsync(server);

            if (_networkManager == null || !NetworkServer.active)
                return;

            if (state == ConnectionState.Valid)
            {
                Debug.Log("[Relay] Host validated by the relay.");
                RoomCodeReady?.Invoke(session.session_id);
                return;
            }

            Debug.LogError(
                $"[Relay] Host was NOT validated by the relay: state={state}, " +
                $"session_id={session.session_id}, relay={session.relay.ip}:{session.relay.ports.server.port}, " +
                $"time={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            if (RelaySettings.RunDiagnostics)
                await _diagnostics.RunAsync(session, transport.userId, transport.sessionId, state);

            Fail("The Edgegap server didn't confirm the room. Press Host again: " +
                 "a new room will most likely land on a working server.");
        }

        private static async Task<ConnectionState> WaitForHostValidationAsync(EdgegapKcpServer server)
        {
            var elapsed = 0;
            while (elapsed < RelaySettings.HostValidationTimeoutMs)
            {
                var state = server.state;

                if (state != ConnectionState.Checking && state != ConnectionState.Disconnected)
                    return state;

                await Task.Delay(RelaySettings.HostValidationPollMs);
                elapsed += RelaySettings.HostValidationPollMs;

                if (!NetworkServer.active)
                    return ConnectionState.Disconnected;
            }

            return server.state;
        }

        private void HandleClientConnected()
        {
            _connectedToHost = true;
        }

        private void HandleClientDisconnected()
        {
            NetworkClient.OnConnectedEvent -= HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;

            if (_connectedToHost)
            {
                Debug.LogWarning("[Relay] Connection to the host was lost. Returning to main menu.");
                Fail("Connection to the host was lost.");
            }
            else
            {
                Debug.LogWarning("[Relay] Failed to connect to the room. Returning to main menu.");
                Fail("Couldn't connect to the room. The host may have left, or the Edgegap server isn't responding.");
            }
        }

        private void Fail(string message)
        {
            if (_networkManager != null)
                ConnectionLost?.Invoke(message);
        }

        private async Task LeaveRelaySessionAsync(string sessionId, bool isHost, uint token)
        {
            try
            {
                if (isHost)
                    await _relayService.DeleteSessionAsync(sessionId);
                else
                    await _relayService.RevokeUserAsync(sessionId, token);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Relay] Failed to clean up relay session: {e}");
            }
        }
    }
}
