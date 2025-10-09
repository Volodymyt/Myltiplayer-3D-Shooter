using System;
using System.Collections;
using Mirror;
using Services;
using UnityEngine;

namespace Gameplay
{
    public class GameplayMediator : IDisposable
    {
        private readonly NetworkManagerFactory _networkManagerFactory;
        private readonly PlayerMovement _playerMovement;

        private NetworkManager _networkManager;

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
            bool isLocal = player.GetComponent<NetworkIdentity>().isLocalPlayer;
            _playerMovement.Construct(player.GetComponent<Rigidbody>(), isLocal);
        }


        public void Dispose()
        {
            if (_networkManager != null)
                _networkManager.StopAllCoroutines();

            if (NetworkServer.active)
                _networkManager.StopHost();
            else if (NetworkClient.isConnected)
                _networkManager.StopClient();

            if (NetworkManager.singleton == _networkManager)
                NetworkManager.singleton = null;

            _playerMovement?.Dispose();

            if (_networkManager != null)
                UnityEngine.Object.Destroy(_networkManager.gameObject);

            _networkManager = null;
        }
    }
}