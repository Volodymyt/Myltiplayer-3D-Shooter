namespace Services
{
    public interface IAssetProviderService
    {
        T LoadAssetFromResources<T>(string path) where T : UnityEngine.Object;
    }
}