using UnityEngine;

namespace Services
{
    public class AssetProviderService : IAssetProviderService
    {
        public T LoadAssetFromResources<T>(string path) where T : UnityEngine.Object =>
            Resources.Load<T>(path);
    }
}