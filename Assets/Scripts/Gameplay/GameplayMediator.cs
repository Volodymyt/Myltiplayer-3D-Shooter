using System;
using System.Threading.Tasks;
using Edgegap;
using Mirror;
using Services;
using Services.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class GameplayMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private readonly PlayerMovement _playerMovement;
        private readonly SpearThrow _spearThrow;
        private readonly IEdgegapRelayService _relayService;

        public static Transform SpearContainer { get; private set; }
        public event Action<string> RoomCodeReady; // код кімнати для показу хосту в UI

        private NetworkManager _networkManager;
        private Camera _sceneCamera;
        public Transform _spearContainer;

        public GameplayMediator(
            GenericFactory genericFactory,
            PlayerMovement playerMovement,
            SpearThrow spearThrow,
            IEdgegapRelayService relayService)
        {
            _genericFactory = genericFactory;
            _playerMovement = playerMovement;
            _spearThrow = spearThrow;
            _relayService = relayService;
        }

        public void Construct()
        {
            _networkManager = _genericFactory.Create<NetworkManager>(Constants.NetworkManagerPath);
            NetworkManager.singleton = _networkManager;

            _spearContainer = new GameObject("SpearsContainer").transform;
            SpearContainer = _spearContainer;
            _sceneCamera = Camera.main;

            PlayerView.LocalPlayerStarted += HandleLocalPlayerStarted;
        }

        public async Task StartNetworkAsync(bool isHost, string joinCode = null)
        {
            var transport = _networkManager.GetComponent<EdgegapKcpTransport>();
            if (transport == null)
            {
                Debug.LogError("EdgegapKcpTransport not found on NetworkManager GameObject.");
                return;
            }

            try
            {
                var myIp = await _relayService.GetPublicIpAsync();
                RelaySession session;

                if (isHost)
                {
                    session = await _relayService.CreateSessionAsync(myIp);
                    session = await _relayService.WaitUntilReadyAsync(session.session_id);
                }
                else
                {
                    if (string.IsNullOrEmpty(joinCode))
                    {
                        Debug.LogError("Join code is required to connect as a client.");
                        return;
                    }

                    await _relayService.AuthorizeUserAsync(joinCode, myIp);
                    session = await _relayService.WaitUntilReadyAsync(joinCode);
                }

                var myUserToken = FindMyAuthorizationToken(session, myIp);

                transport.relayAddress = session.relay.ip;
                transport.sessionId = session.authorization_token;
                transport.userId = myUserToken;

                if (isHost)
                    transport.relayGameServerPort = (ushort)session.relay.ports.server.port;
                else
                    transport.relayGameClientPort = (ushort)session.relay.ports.client.port;

                if (isHost)
                {
                    _networkManager.StartHost();
                    RoomCodeReady?.Invoke(session.session_id);
                }
                else
                {
                    _networkManager.StartClient();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start network via Edgegap relay: {e}");
            }
        }

        private uint FindMyAuthorizationToken(RelaySession session, string myIp)
        {
            foreach (var user in session.session_users)
            {
                if (user.ip_address == myIp)
                    return user.authorization_token;
            }

            Debug.LogWarning("Own IP not found in session_users, using first entry as fallback.");
            return session.session_users.Length > 0 ? session.session_users[0].authorization_token : 0;
        }

        private void HandleLocalPlayerStarted(PlayerView playerView)
        {
            _sceneCamera.enabled = false;
            playerView.playerCamera.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _playerMovement.Construct(playerView, true);
            _spearThrow.Construct(playerView);
        }

        public void Dispose()
        {
            PlayerView.LocalPlayerStarted -= HandleLocalPlayerStarted;

            _sceneCamera.enabled = true;

            if (NetworkServer.active)
                _networkManager.StopHost();
            else if (NetworkClient.isConnected)
                _networkManager.StopClient();

            if (NetworkManager.singleton == _networkManager)
                NetworkManager.singleton = null;

            Object.Destroy(_networkManager.gameObject);

            _playerMovement.Dispose();
            _spearThrow.Dispose();
            _networkManager = null;
        }
    }
}
