using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Services.Networking
{
    public class EdgegapRelayService : IEdgegapRelayService, IDisposable
    {
        // TODO: перед білдом прибрати токен звідси (див. чекліст, п.5) —
        // або через проксі-сервер, або через захищене конфіг-сховище.
        private const string ApiToken = "fd3f1342-b5c4-498b-af38-7bb03d758c39";
        private const string BaseUrl = "https://api.edgegap.com/v1/relays/sessions";

        private readonly HttpClient _http;

        public EdgegapRelayService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", ApiToken);
        }

        public async Task<string> GetPublicIpAsync()
        {
            var response = await _http.GetAsync("https://api.ipify.org?format=json");
            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonUtility.FromJson<IpifyResponse>(json);
            return parsed.ip;
        }

        public async Task<RelaySession> CreateSessionAsync(string hostIp)
        {
            var body = new CreateSessionRequest { users = new[] { new IpUser { ip = hostIp } } };
            var json = JsonUtility.ToJson(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(BaseUrl, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"CreateSession failed: {response.StatusCode} {responseJson}");

            return JsonUtility.FromJson<RelaySession>(responseJson);
        }

        public async Task AuthorizeUserAsync(string sessionId, string userIp)
        {
            var body = new AuthorizeUserRequest { session_id = sessionId, user_ip = userIp };
            var json = JsonUtility.ToJson(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{BaseUrl}:authorize-user", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AuthorizeUser failed: {response.StatusCode} {responseJson}");
        }

        public async Task<RelaySession> GetSessionAsync(string sessionId)
        {
            var response = await _http.GetAsync($"{BaseUrl}/{sessionId}");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetSession failed: {response.StatusCode} {json}");

            return JsonUtility.FromJson<RelaySession>(json);
        }

        public async Task<RelaySession> WaitUntilReadyAsync(string sessionId, int timeoutMs = 15000)
        {
            var elapsed = 0;
            const int pollIntervalMs = 500;

            while (elapsed < timeoutMs)
            {
                var session = await GetSessionAsync(sessionId);
                if (session.ready && session.linked)
                    return session;

                await Task.Delay(pollIntervalMs);
                elapsed += pollIntervalMs;
            }

            throw new TimeoutException($"Relay session {sessionId} was not ready in time.");
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            await _http.DeleteAsync($"{BaseUrl}/{sessionId}");
        }

        public void Dispose() => _http?.Dispose();
    }
}