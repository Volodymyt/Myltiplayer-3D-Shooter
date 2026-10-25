using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Edgegap;

namespace Services.Networking
{
    internal static class RelayProbe
    {
        private const int PingIntervalMs = 500;

        private class Result
        {
            public int LocalPort;
            public int Replies;
            public ConnectionState LastState = ConnectionState.Disconnected;
            public readonly Dictionary<byte, int> Counts = new Dictionary<byte, int>();
            public readonly List<byte> Sequence = new List<byte>();
            public string Error;
        }

        public static async Task<ConnectionState> WaitForValidationAsync(
            string relayIp, int port, uint userToken, uint sessionToken, int timeoutMs)
        {
            var result = await ProbeAsync(relayIp, port, userToken, sessionToken, timeoutMs,
                state => state == ConnectionState.Valid || state == ConnectionState.Invalid);
            return result.LastState;
        }

        public static async Task<string> RunAsync(string relayIp, int port, uint userToken, uint sessionToken, int durationMs)
        {
            var r = await ProbeAsync(relayIp, port, userToken, sessionToken, durationMs, _ => false);

            if (r.Error != null)
                return $"error after {r.Replies} replies: {r.Error}";

            return $"localPort={r.LocalPort} replies={r.Replies} " +
                   $"states=[{string.Join(", ", r.Counts.Select(s => $"{Name(s.Key)} x{s.Value}"))}] " +
                   $"sequence=[{string.Join(" -> ", r.Sequence.Select(Name))}]";
        }

        private static async Task<Result> ProbeAsync(string relayIp, int port, uint userToken, uint sessionToken,
            int durationMs, Func<ConnectionState, bool> stopOn)
        {
            var r = new Result();
            Task<UdpReceiveResult> receiveTask = null;

            try
            {
                using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
                {
                    udp.Connect(IPAddress.Parse(relayIp), port);
                    r.LocalPort = ((IPEndPoint)udp.Client.LocalEndPoint).Port;
                    var ping = BuildPing(userToken, sessionToken);

                    var clock = Stopwatch.StartNew();
                    long nextPingAt = 0;
                    receiveTask = udp.ReceiveAsync();

                    while (clock.ElapsedMilliseconds < durationMs)
                    {
                        if (clock.ElapsedMilliseconds >= nextPingAt)
                        {
                            await udp.SendAsync(ping, ping.Length);
                            nextPingAt += PingIntervalMs;
                        }

                        var finished = await Task.WhenAny(receiveTask, Task.Delay(50));
                        if (finished != receiveTask)
                            continue;

                        var data = receiveTask.Result.Buffer;
                        receiveTask = udp.ReceiveAsync();

                        if (data.Length < 2 || data[0] != (byte)MessageType.Ping)
                            continue;

                        var state = data[1];
                        r.Replies++;
                        r.LastState = (ConnectionState)state;
                        r.Counts[state] = r.Counts.TryGetValue(state, out var n) ? n + 1 : 1;
                        if (r.Sequence.Count == 0 || r.Sequence[r.Sequence.Count - 1] != state)
                            r.Sequence.Add(state);

                        if (stopOn(r.LastState))
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                var inner = e is AggregateException ae && ae.InnerException != null ? ae.InnerException : e;
                r.Error = $"{inner.GetType().Name}: {inner.Message}";
            }
            finally
            {
                receiveTask?.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            }

            return r;
        }

        private static byte[] BuildPing(uint userToken, uint sessionToken)
        {
            return new[]
            {
                (byte)userToken, (byte)(userToken >> 8), (byte)(userToken >> 16), (byte)(userToken >> 24),
                (byte)sessionToken, (byte)(sessionToken >> 8), (byte)(sessionToken >> 16), (byte)(sessionToken >> 24),
                (byte)MessageType.Ping
            };
        }

        private static string Name(byte state) =>
            Enum.IsDefined(typeof(ConnectionState), state)
                ? ((ConnectionState)state).ToString()
                : $"Unknown({state})";
    }
}
