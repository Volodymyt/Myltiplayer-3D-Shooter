public class Constants
{
    // Gameplay
    public const string NetworkManagerPath = "NetworkManager";
    public const string SpearPath = "Spear";
    
    // UI
    public const string LobbyPath = "Lobby";

    public class PlayerSettings
    {
        public const float MoveSpeed = 3f;
        public const float JumpForce = 8f;
        
        public const float MouseSensitivity = 15f;
        public const float MinXRotation = -20;
        public const float MaxXRotation = 20;
        
        public const float RigidbodyGroundCheckDistance = 0.1f;
        public const float AnimatorGroundCheckDistance = 0.4f;
        
        public const float ThrowForce = 20f;
        public const float RespawnDelay = 2f;
        public const float ThrowAngle = 15f;
    }
}