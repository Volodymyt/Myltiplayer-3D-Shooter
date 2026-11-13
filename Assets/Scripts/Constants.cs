public class Constants
{
    public const string NetworkManagerPath = "NetworkManager";
    public const string SpearPath = "Spear";
    public const string IcebergChunkPath = "IcebergChunk"; 
    
    public const string LobbyPath = "Lobby";
    public const string GameHudPath = "GameHud";

    public class PlayerSettings
    {
        public const float MoveSpeed = 3f;
        
        public const float MouseSensitivity = 15f;
        public const float MinXRotation = -20;
        public const float MaxXRotation = 20;
        
        public const float ThrowForce = 20f;
        public const float RespawnDelay = 2f;
        public const float ThrowAngle = 15f;
    }
    
    public class IcebergMeltSettings
    {
        public const float StartDelay = 8f;  
        public const float TimeBetweenBreaks = 8f;  
        public const float WarningTime = 2.5f;     
        public const float Randomness = 4f;      
    }
}