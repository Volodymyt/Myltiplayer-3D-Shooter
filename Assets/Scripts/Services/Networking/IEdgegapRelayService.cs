using System.Threading.Tasks;

namespace Services.Networking
{
    public interface IEdgegapRelayService
    {
        Task<string> GetPublicIpAsync();
        Task<RelaySession> CreateSessionAsync(string hostIp);
        Task AuthorizeUserAsync(string sessionId, string userIp);
        Task<RelaySession> GetSessionAsync(string sessionId);
        Task<RelaySession> WaitUntilReadyAsync(string sessionId, int timeoutMs = 15000);
        Task DeleteSessionAsync(string sessionId);
    }
}