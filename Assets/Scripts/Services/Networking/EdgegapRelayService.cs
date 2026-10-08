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
        private const string BaseUrl = "https://api.edgegap.com/v1/relays/sessions";

        private readonly HttpClient _http;

        private readonly HttpClient _publicHttp = new HttpClient();

        public EdgegapRelayService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", EdgegapSecrets.ApiToken);
        }

        public async Task<string> GetPublicIpAsync()
        {
            var httpsTask = GetHttpsIpSafeAsync();
            var googleTask = StunClient.GetPublicIpAsync("stun.l.google.com", 19302);
            var cloudflareTask = StunClient.GetPublicIpAsync("stun.cloudflare.com", 3478);
            await Task.WhenAll(httpsTask, googleTask, cloudflareTask);

            var httpsIp = httpsTask.Result;
            var udpGoogle = googleTask.Result;
            var udpCloudflare = cloudflareTask.Result;

            Debug.Log($"[Relay] Public IP: https={httpsIp ?? "?"} " +
                      $"udp(google)={udpGoogle ?? "?"} udp(cloudflare)={udpCloudflare ?? "?"}");

            if (udpGoogle != null && udpCloudflare != null && udpGoogle != udpCloudflare)
                Debug.LogWarning("[Relay] UDP IP differs between STUN servers: this network changes " +
                                 "the public IP per connection, so the relay may see yet another IP.");

            var udpIp = udpGoogle ?? udpCloudflare;
            if (udpIp != null && httpsIp != null && udpIp != httpsIp)
                Debug.LogWarning($"[Relay] HTTPS IP ({httpsIp}) != UDP IP ({udpIp}). Using UDP IP for the relay.");

            var ip = udpIp ?? httpsIp;
            if (ip == null)
                throw new Exception("Could not determine public IP (both STUN and ipify failed).");

            return ip;
        }

        private async Task<string> GetHttpsIpSafeAsync()
        {
            try
            {
                var response = await _publicHttp.GetAsync("https://api.ipify.org?format=json");
                var json = await response.Content.ReadAsStringAsync();
                return JsonUtility.FromJson<IpifyResponse>(json)?.ip;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Relay] ipify failed: {e.Message}");
                return null;
            }
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

        public async Task<SessionUser> AuthorizeUserAsync(string sessionId, string userIp)
        {
            var body = new AuthorizeUserRequest { session_id = sessionId, user_ip = userIp };
            var json = JsonUtility.ToJson(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{BaseUrl}:authorize-user", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AuthorizeUser failed: {response.StatusCode} {responseJson}");

            var parsed = JsonUtility.FromJson<AuthorizeUserResponse>(responseJson);
            return parsed.session_user;
        }

        public async Task<RelaySession> GetSessionAsync(string sessionId)
        {
            var json = await GetSessionJsonAsync(sessionId);
            return JsonUtility.FromJson<RelaySession>(json);
        }

        private async Task<string> GetSessionJsonAsync(string sessionId)
        {
            var response = await _http.GetAsync($"{BaseUrl}/{sessionId}");
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetSession failed: {response.StatusCode} {json}");

            return json;
        }

        public async Task<RelaySession> WaitUntilReadyAsync(string sessionId, int timeoutMs = 15000)
        {
            var elapsed = 0;
            const int pollIntervalMs = 500;

            while (elapsed < timeoutMs)
            {
                var json = await GetSessionJsonAsync(sessionId);
                var session = JsonUtility.FromJson<RelaySession>(json);
                if (session.ready && session.linked)
                {
                    Debug.Log($"[Relay] Session JSON: {json}");
                    return session;
                }

                await Task.Delay(pollIntervalMs);
                elapsed += pollIntervalMs;
            }

            throw new TimeoutException($"Relay session {sessionId} was not ready in time.");
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            await _http.DeleteAsync($"{BaseUrl}/{sessionId}");
        }

        public async Task RevokeUserAsync(string sessionId, uint authorizationToken)
        {
            var body = new RevokeUserRequest { session_id = sessionId, authorization_token = authorizationToken };
            var json = JsonUtility.ToJson(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{BaseUrl}:revoke-user", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"RevokeUser failed: {response.StatusCode} {responseJson}");
        }

        public void Dispose()
        {
            _http?.Dispose();
            _publicHttp?.Dispose();
        }
    }
}