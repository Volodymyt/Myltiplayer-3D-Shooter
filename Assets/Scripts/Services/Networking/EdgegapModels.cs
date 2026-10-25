using System;

namespace Services.Networking
{
    [Serializable]
    public class RelaySession
    {
        public string session_id;
        public uint authorization_token; 
        public string status;
        public bool ready;
        public bool linked;
        public string error;
        public SessionUser[] session_users;
        public RelayInfo relay;
    }

    [Serializable]
    public class SessionUser
    {
        public string ip_address;
        public uint authorization_token;
    }

    [Serializable]
    public class RelayInfo
    {
        public string ip;
        public string host;
        public RelayPorts ports;
    }

    [Serializable]
    public class RelayPorts
    {
        public PortInfo server;
        public PortInfo client;
    }

    [Serializable]
    public class PortInfo
    {
        public int port;
        public string protocol;
    }

    [Serializable]
    internal class IpUser
    {
        public string ip;
    }

    [Serializable]
    internal class CreateSessionRequest
    {
        public IpUser[] users;
    }

    [Serializable]
    public class RelayFilter
    {
        public string field;
        public string[] values;
        public string filter_type;

        public override string ToString() => $"{field} {filter_type} [{string.Join(", ", values ?? new string[0])}]";
    }

    [Serializable]
    internal class CreateSessionWithFiltersRequest
    {
        public IpUser[] users;
        public RelayFilter[] filters;
    }

    [Serializable]
    internal class AuthorizeUserRequest
    {
        public string session_id;
        public string user_ip;
    }

    [Serializable]
    internal class RevokeUserRequest
    {
        public string session_id;
        public uint authorization_token;
    }

    [Serializable]
    internal class AuthorizeUserResponse
    {
        public string session_id;
        public SessionUser session_user;
    }

    [Serializable]
    internal class IpifyResponse
    {
        public string ip;
    }
}