using System;
using Gameplay;
using Mirror;
using Services;
using UnityEngine;

namespace UI
{
    public class UIGameMediator : IDisposable
    {
        private readonly NetworkManagerFactory _networkManagerFactory;
        private readonly LobbyFactory _lobbyFactory;
        private readonly IAssetProviderService _assetProviderService;
        private readonly PlayerMovement _playerMovement;
        
        private NetworkManager _networkManager;
        
        private Lobby _lobby;

        public UIGameMediator(NetworkManagerFactory networkManagerFactory,
            LobbyFactory lobbyFactory,
            IAssetProviderService assetProviderService,
            PlayerMovement playerMovement
            
            )
        {
            _networkManagerFactory = networkManagerFactory;
            _lobbyFactory = lobbyFactory;
            _assetProviderService = assetProviderService;
            _playerMovement = playerMovement;
        }

        public void Construct()
        {
            _networkManager = _networkManagerFactory.CreateNetworkManager();
            NetworkManager.singleton = _networkManager;
           _lobby = _lobbyFactory.CreateLobby();
            
           _lobby.OnHostAddRequest += AddHost;
           _lobby.OnClientAddRequest += AddClient;
        }
        
        private void AddHost()
        {
            _networkManager.autoCreatePlayer = true;
            _networkManager.playerPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.PlayerPath);
            _networkManager.StartHost();

            _networkManager.StartCoroutine(WaitForPlayer());
        }

        private System.Collections.IEnumerator WaitForPlayer()
        {
            yield return new WaitUntil(() => NetworkClient.localPlayer != null);

            GameObject player = NetworkClient.localPlayer.gameObject;
            bool isLocal = player.GetComponent<NetworkIdentity>().isLocalPlayer;
            _playerMovement.Construct(player.GetComponent<Rigidbody>(), isLocal);
        }

        private void AddClient()
        {
            _networkManager.autoCreatePlayer = true;
            _networkManager.playerPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.PlayerPath);
            _networkManager.StartClient();
            
            _networkManager.StartCoroutine(WaitForPlayer());
        }

        public void Dispose()
        {
            _lobby.OnHostAddRequest -= AddHost;
            _lobby.OnClientAddRequest -= AddClient;
        }
    }
}