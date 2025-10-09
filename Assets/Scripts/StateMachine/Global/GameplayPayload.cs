using StateMachine.Base;

namespace StateMachine.Global
{
    public class GameplayPayload : PayloadBase
    {
        public bool IsHost { get; }

        public GameplayPayload(bool isHost)
        {
            IsHost = isHost;
        }
    }

}