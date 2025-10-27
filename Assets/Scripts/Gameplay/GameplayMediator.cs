using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class GameplayMediator : IDisposable
    {
        private readonly NetworkManagerFactory _networkManagerFactory;
        private readonly PlayerMovement _playerMovement;
        private readonly SpearThrow _spearThrow;

        private NetworkManager _networkManager;
        private Camera _sceneCamera;

        public GameplayMediator(
            NetworkManagerFactory networkManagerFactory,
            PlayerMovement playerMovement,
            SpearThrow spearThrow)
        {
            _networkManagerFactory = networkManagerFactory;
            _playerMovement = playerMovement;
            _spearThrow = spearThrow;
        }

        public void Construct()
        {
            _networkManager = _networkManagerFactory.CreateNetworkManager();
            NetworkManager.singleton = _networkManager;

            _sceneCamera = Camera.main;
        }

        public void StartNetwork(bool isHost)
        {
            if (isHost)
                _networkManager.StartHost();
            else
                _networkManager.StartClient();

            _networkManager.StartCoroutine(WaitForPlayer());
        }

        private IEnumerator WaitForPlayer()
        {
            yield return new WaitUntil(() => NetworkClient.localPlayer != null);

            GameObject player = NetworkClient.localPlayer.gameObject;
            PlayerView playerView = player.GetComponent<PlayerView>();
            var identity = playerView.playerNetworkIdentity;

            yield return new WaitUntil(() => identity.isLocalPlayer);

            _sceneCamera.enabled = false;
            var playerCamera = playerView.playerCamera;
            playerCamera.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _playerMovement.Construct(playerView, true);
            _spearThrow.Construct(playerView.spearThrowPoint, playerCamera);
        }

        public void Dispose()
        {
            _sceneCamera.enabled = true;

            _networkManager.StopAllCoroutines();

            if (NetworkServer.active)
                _networkManager.StopHost();
            else if (NetworkClient.isConnected)
                _networkManager.StopClient();

            if (NetworkManager.singleton == _networkManager)
                NetworkManager.singleton = null;

            Object.Destroy(_networkManager.gameObject);

            _playerMovement.Dispose();
            
            _networkManager = null;
        }
    }
}