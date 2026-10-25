using System.Reflection;
using Edgegap;
using UnityEngine;

namespace Services.Networking
{
    public static class EdgegapTransportExtensions
    {
        public static void ApplyRelaySession(this EdgegapKcpTransport transport, RelaySession session, uint userToken,
            bool isHost)
        {
            transport.relayAddress = session.relay.ip;
            transport.sessionId = session.authorization_token;
            transport.userId = userToken;

            if (isHost)
                transport.relayGameServerPort = (ushort)session.relay.ports.server.port;
            else
                transport.relayGameClientPort = (ushort)session.relay.ports.client.port;

            Debug.Log($"[Relay] Transport configured -> relayAddress={transport.relayAddress} " +
                      $"sessionId={transport.sessionId} userId={transport.userId} " +
                      $"serverPort={transport.relayGameServerPort} clientPort={transport.relayGameClientPort}");
        }

        public static EdgegapKcpServer GetRelayServer(this EdgegapKcpTransport transport)
        {
            for (var type = transport.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("server",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                if (field != null)
                    return field.GetValue(transport) as EdgegapKcpServer;
            }

            return null;
        }
    }
}
