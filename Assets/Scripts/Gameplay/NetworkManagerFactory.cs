using Mirror;
using Services;
using UnityEngine;

namespace Gameplay
{
    public class NetworkManagerFactory
    {
        private readonly IAssetProviderService _assetProviderService;

        NetworkManagerFactory(IAssetProviderService assetProviderService)
        {
            _assetProviderService = assetProviderService;
        }

        public NetworkManager CreateNetworkManager()
        {
            var managerPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.NetworkManagerPath);
            GameObject mangerGameObject = Object.Instantiate(managerPrefab);
            
            return mangerGameObject.GetComponent<NetworkManager>();
        }
    }
}