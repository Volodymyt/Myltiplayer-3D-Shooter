using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edgegap;
using UnityEngine;

namespace Services.Networking
{
    public class RelayPicker
    {
        private readonly IEdgegapRelayService _relayService;

        public RelayPicker(IEdgegapRelayService relayService)
        {
            _relayService = relayService;
        }

        public async Task<(RelaySession session, uint token)?> PickForHostAsync(string myIp, CancellationToken cancel)
        {
            var attempts = RelaySettings.HostAttempts;
            var failedRelayHosts = new HashSet<string>();

            for (var i = 0; i < attempts.Length; i++)
            {
                var filters = attempts[i];
                var label = filters == null ? "no filter" : string.Join("; ", filters.Select(f => f.ToString()));
                var attempt = $"Attempt {i + 1}/{attempts.Length} ({label})";
                RelaySession session = null;

                try
                {
                    session = await _relayService.CreateSessionAsync(myIp, filters);
                    session = await _relayService.WaitUntilReadyAsync(session.session_id);
                    var token = FindMyToken(session, myIp);

                    if (failedRelayHosts.Contains(session.relay.host))
                    {
                        Debug.Log($"[Relay] {attempt}: got {session.relay.host} again, " +
                                  "it already failed in this run - skipping without a check.");
                        throw new SkipRelayException();
                    }

                    var forceFail = RelaySettings.DebugForceFirstAttemptToFail && i == 0;
                    if (forceFail)
                        Debug.LogWarning($"[Relay] {attempt}: TEST MODE, probing with a wrong token on purpose.");

                    var state = await RelayProbe.WaitForValidationAsync(
                        session.relay.ip, session.relay.ports.server.port,
                        forceFail ? token ^ 0x5A5A5A5Au : token, session.authorization_token,
                        RelaySettings.ProbeTimeoutMs);

                    Debug.Log($"[Relay] {attempt}: session={session.session_id} " +
                              $"relay={session.relay.ip}:{session.relay.ports.server.port} " +
                              $"({session.relay.host}) -> {state}");

                    if (state == ConnectionState.Valid && !cancel.IsCancellationRequested)
                        return (session, token);

                    failedRelayHosts.Add(session.relay.host);
                }
                catch (SkipRelayException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Relay] {attempt} failed: {e.Message}");
                }

                if (session != null && !string.IsNullOrEmpty(session.session_id))
                    _ = DeleteSessionSafeAsync(session.session_id);

                if (cancel.IsCancellationRequested)
                    return null;
            }

            return null;
        }

        private static uint FindMyToken(RelaySession session, string myIp)
        {
            foreach (var user in session.session_users)
            {
                if (user.ip_address == myIp)
                    return user.authorization_token;
            }

            Debug.LogWarning("[Relay] Own IP not found in session_users, using first entry as fallback.");
            return session.session_users.Length > 0 ? session.session_users[0].authorization_token : 0;
        }

        private async Task DeleteSessionSafeAsync(string sessionId)
        {
            try
            {
                await _relayService.DeleteSessionAsync(sessionId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Relay] Failed to delete session {sessionId}: {e.Message}");
            }
        }

        private class SkipRelayException : Exception
        {
        }
    }
}
