using Services;
using UnityEngine;

namespace Gameplay
{
    public class SpearFactory
    { 
        private readonly IAssetProviderService _assetProviderService;

        public SpearFactory(IAssetProviderService assetProviderService)
        {
            _assetProviderService = assetProviderService;
        }

        public Transform CreateSpear()
        {
            var spearPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.SpearPath);
            Transform spearTransform = Object.Instantiate(spearPrefab).transform;
            
            return spearTransform;
        }
    }
}