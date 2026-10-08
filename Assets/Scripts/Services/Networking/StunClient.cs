using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Services.Networking
{
    internal static class StunClient
    {
        public static async Task<string> GetPublicIpAsync(string host, int port, int timeoutMs = 1500, int attempts = 3)
        {
            Task<UdpReceiveResult> receiveTask = null;

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var server = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (server == null)
                    return null;

                var serverEndPoint = new IPEndPoint(server, port);
                var request = BuildRequest(out var transactionId);

                using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
                {
                    receiveTask = udp.ReceiveAsync();

                    for (var attempt = 0; attempt < attempts; attempt++)
                    {
                        await udp.SendAsync(request, request.Length, serverEndPoint);

                        var finished = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));
                        if (finished != receiveTask)
                            continue; 

                        var ip = ParseResponse(receiveTask.Result.Buffer, transactionId);
                        if (ip != null)
                            return ip;

                        receiveTask = udp.ReceiveAsync();
                    }
                }
            }
            catch (Exception)
            {
                
            }
            finally
            {
                receiveTask?.ContinueWith(t => { var _ = t.Exception; },
                    TaskContinuationOptions.OnlyOnFaulted);
            }

            return null;
        }

        private static byte[] BuildRequest(out byte[] transactionId)
        {
            var request = new byte[20];
            request[0] = 0x00; request[1] = 0x01;
            request[2] = 0x00; request[3] = 0x00; 
            request[4] = 0x21; request[5] = 0x12;
            request[6] = 0xA4; request[7] = 0x42;

            transactionId = new byte[12];
            new Random().NextBytes(transactionId);
            Array.Copy(transactionId, 0, request, 8, 12);
            return request;
        }

        private static string ParseResponse(byte[] data, byte[] transactionId)
        {
            if (data == null || data.Length < 20)
                return null;

            var messageType = (data[0] << 8) | data[1];
            if (messageType != 0x0101)
                return null;

            for (var i = 0; i < 12; i++)
                if (data[8 + i] != transactionId[i])
                    return null;

            var length = (data[2] << 8) | data[3];
            var end = Math.Min(data.Length, 20 + length);
            var offset = 20;
            string mappedAddress = null;

            while (offset + 4 <= end)
            {
                var attrType = (data[offset] << 8) | data[offset + 1];
                var attrLength = (data[offset + 2] << 8) | data[offset + 3];
                var value = offset + 4;
                if (value + attrLength > end)
                    break;

                if ((attrType == 0x0020 || attrType == 0x0001) && attrLength >= 8 && data[value + 1] == 0x01)
                {
                    var ip = new byte[4];
                    Array.Copy(data, value + 4, ip, 0, 4);

                    if (attrType == 0x0020)
                    {
                        ip[0] = (byte)(ip[0] ^ 0x21);
                        ip[1] = (byte)(ip[1] ^ 0x12);
                        ip[2] = (byte)(ip[2] ^ 0xA4);
                        ip[3] = (byte)(ip[3] ^ 0x42);
                        return new IPAddress(ip).ToString();
                    }

                    mappedAddress = new IPAddress(ip).ToString();
                }

                offset = value + ((attrLength + 3) & ~3); 
            }

            return mappedAddress;
        }
    }
}