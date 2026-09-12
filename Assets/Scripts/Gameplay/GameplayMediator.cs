using System;
using Mirror;
using Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class GameplayMediator : IDisposable
    {
        public static Transform SpearContainer { get; private set; }

        private readonly GenericFactory _genericFactory;
        private readonly PlayerMovement _playerMovement;
        private readonly SpearThrow _spearThrow;

        private NetworkManager _networkManager;
        private Camera _sceneCamera;

        public GameplayMediator(
            GenericFactory genericFactory,
            PlayerMovement playerMovement,
            SpearThrow spearThrow)
        {
            _genericFactory = genericFactory;
            _playerMovement = playerMovement;
            _spearThrow = spearThrow;
        }

        public void Construct()
        {
            _networkManager = _genericFactory.Create<NetworkManager>(Constants.NetworkManagerPath);
            NetworkManager.singleton = _networkManager;

            SpearContainer = new GameObject("SpearsContainer").transform;
            _sceneCamera = Camera.main;

            PlayerView.LocalPlayerStarted += HandleLocalPlayerStarted;
        }

        public void StartNetwork(bool isHost)
        {
            if (isHost)
                _networkManager.StartHost();
            else
                _networkManager.StartClient();
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
            _networkManager = null;
            SpearContainer = null;
        }
    }
}
