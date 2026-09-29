using StateMachine.Base;

namespace StateMachine.Global
{
    public class GameplayPayload : PayloadBase
    {
        public bool IsHost { get; }
        public string JoinCode { get; }

        public GameplayPayload(bool isHost, string joinCode = null)
        {
            IsHost = isHost;
            JoinCode = joinCode;
        }
    }
}