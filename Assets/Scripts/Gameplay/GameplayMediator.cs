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

        private NetworkManager _networkManager;
        private Camera _sceneCamera;

        public GameplayMediator(
            NetworkManagerFactory networkManagerFactory,
            PlayerMovement playerMovement)
        {
            _networkManagerFactory = networkManagerFactory;
            _playerMovement = playerMovement;
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

            var player = NetworkClient.localPlayer.gameObject;
            var identity = player.GetComponent<NetworkIdentity>();

            yield return new WaitUntil(() => identity.isLocalPlayer);

            _sceneCamera.enabled = false;
            var playerCamera = player.GetComponentInChildren<Camera>(true);
            playerCamera.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _playerMovement.Construct(player.GetComponent<Rigidbody>(), playerCamera.transform, true);
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