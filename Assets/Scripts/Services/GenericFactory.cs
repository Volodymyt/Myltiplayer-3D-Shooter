using UnityEngine;

namespace Services
{
    public class GenericFactory
    {
        private readonly IAssetProviderService _assetProviderService;

        public GenericFactory(IAssetProviderService assetProviderService)
        {
            _assetProviderService = assetProviderService;
        }

        public T Create<T>(string resourcePath, Transform parent = null) where T : Component
        {
            var prefab = _assetProviderService.LoadAssetFromResources<GameObject>(resourcePath);

            if (prefab == null)
                throw new System.Exception($"Prefab not found at path: {resourcePath}");

            var instance = Object.Instantiate(prefab, parent);
            var component = instance.GetComponent<T>();

            if (component == null)
                throw new System.Exception($"Component of type {typeof(T).Name} not found on prefab: {prefab.name}");

            return component;
        }
    }
}