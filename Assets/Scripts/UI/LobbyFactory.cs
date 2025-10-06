using Services;
using UnityEngine;

namespace UI
{
    public class LobbyFactory
    {
        private readonly IAssetProviderService _assetProviderService;

        public LobbyFactory(IAssetProviderService assetProviderService)
        {
            _assetProviderService = assetProviderService;
        }

        public Lobby CreateLobby()
        {
            var lobbyPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.LobbyPath);
            GameObject lobby = Object.Instantiate(lobbyPrefab);

            return lobby.GetComponent<Lobby>();
        }
    }
}