using System;
using System.Threading.Tasks;
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
        private readonly NetworkConnectionService _network;

        public static Transform SpearContainer { get; private set; }
        public event Action<string> RoomCodeReady;
        public event Action<string> ConnectionLost;

        private NetworkManager _networkManager;
        private Camera _sceneCamera;

        public GameplayMediator(
            GenericFactory genericFactory,
            PlayerMovement playerMovement,
            SpearThrow spearThrow,
            NetworkConnectionService network)
        {
            _genericFactory = genericFactory;
            _playerMovement = playerMovement;
            _spearThrow = spearThrow;
            _network = network;
        }

        public void Construct()
        {
            _networkManager = _genericFactory.Create<NetworkManager>(Constants.NetworkManagerPath);
            NetworkManager.singleton = _networkManager;

            SpearContainer = new GameObject("SpearsContainer").transform;
            _sceneCamera = Camera.main;

            PlayerView.LocalPlayerStarted += HandleLocalPlayerStarted;
            _network.RoomCodeReady += HandleRoomCodeReady;
            _network.ConnectionLost += HandleConnectionLost;
        }

        public Task StartNetworkAsync(bool isHost, string joinCode = null) =>
            isHost
                ? _network.HostAsync(_networkManager)
                : _network.JoinAsync(_networkManager, joinCode);

        public Task CleanupBeforeQuitAsync() => _network.LeaveSessionAsync();

        private void HandleRoomCodeReady(string code) => RoomCodeReady?.Invoke(code);

        private void HandleConnectionLost(string message) => ConnectionLost?.Invoke(message);

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
            _network.RoomCodeReady -= HandleRoomCodeReady;
            _network.ConnectionLost -= HandleConnectionLost;
            _network.Stop();

            if (_sceneCamera != null)
                _sceneCamera.enabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkManager.singleton == _networkManager)
                NetworkManager.singleton = null;

            if (_networkManager != null)
                Object.Destroy(_networkManager.gameObject);

            _playerMovement.Dispose();
            _spearThrow.Dispose();
            _networkManager = null;
        }
    }
}
