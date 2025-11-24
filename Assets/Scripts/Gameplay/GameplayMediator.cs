using System;
using System.Collections;
using Mirror;
using Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class GameplayMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private readonly PlayerMovement _playerMovement;
        private readonly SpearThrow _spearThrow;

        private NetworkManager _networkManager;
        private Camera _sceneCamera;
        private Transform _spearContainer;

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
            
            _spearContainer = new GameObject("SpearsContainer").transform;

            Debug.Log(_spearContainer.name);
            
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
            _spearThrow.Construct(playerView, _spearContainer);
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