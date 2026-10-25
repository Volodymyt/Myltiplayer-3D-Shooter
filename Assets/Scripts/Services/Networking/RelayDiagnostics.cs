using System;
using System.Threading.Tasks;
using Edgegap;
using UnityEngine;

namespace Services.Networking
{
    public class RelayDiagnostics
    {
        private readonly IEdgegapRelayService _relayService;

        public RelayDiagnostics(IEdgegapRelayService relayService)
        {
            _relayService = relayService;
        }

        public async Task RunAsync(RelaySession used, uint userToken, uint sessionToken, ConnectionState mainState)
        {
            var ip = used.relay.ip;
            var port = used.relay.ports.server.port;
            Debug.Log("[RelayDiag] Host was not validated, running diagnostics (~12 s)...");

            var sessionCheck = await CheckSessionAsync(used);
            Debug.Log($"[RelayDiag] 1. Session re-check: {sessionCheck}");

            var freshSocket = await RelayProbe.RunAsync(ip, port, userToken, sessionToken, 6000);
            Debug.Log($"[RelayDiag] 2. Fresh socket, same tokens: {freshSocket}");

            var wrongUser = await RelayProbe.RunAsync(ip, port, userToken ^ 0x5A5A5A5A, sessionToken, 4000);
            Debug.Log($"[RelayDiag] 3. Wrong user token on purpose: {wrongUser}");

            Debug.LogWarning(
                "[RelayDiag] SUMMARY | " +
                $"time={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | session_id={used.session_id} | " +
                $"relay={ip}:{port} ({used.relay.host}) | main host socket={mainState} | " +
                $"1) {sessionCheck} | 2) {freshSocket} | 3) {wrongUser}");
        }

        private async Task<string> CheckSessionAsync(RelaySession used)
        {
            try
            {
                var now = await _relayService.GetSessionAsync(used.session_id);
                var changed = now.relay == null
                              || now.relay.ip != used.relay.ip
                              || now.relay.host != used.relay.host
                              || now.relay.ports?.server?.port != used.relay.ports.server.port;

                return $"status={now.status} ready={now.ready} linked={now.linked} " +
                       $"relay={now.relay?.ip}:{now.relay?.ports?.server?.port} host={now.relay?.host} " +
                       $"users={now.session_users?.Length ?? 0} changed={(changed ? "YES" : "NO")}";
            }
            catch (Exception e)
            {
                return $"error: {e.Message}";
            }
        }
    }
}
