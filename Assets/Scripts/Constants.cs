public class Constants
{
    // Gameplay
    public const string NetworkManagerPath = "NetworkManager";
    public const string SpearPath = "Spear";
    public const string IcebergChunkPath = "IcebergChunk"; 
    
    // UI
    public const string LobbyPath = "Lobby";
    public const string GameHudPath = "GameHud";

    public class PlayerSettings
    {
        public const float MoveSpeed = 3f;
        public const float JumpForce = 8f;
        
        public const float MouseSensitivity = 15f;
        public const float MinXRotation = -20;
        public const float MaxXRotation = 20;
        
        public const float ThrowForce = 20f;
        public const float RespawnDelay = 2f;
        public const float ThrowAngle = 15f;
    }
    
    public class IcebergMeltSettings
    {
        public const float StartDelay = 5f;          // через скільки секунд після появи айсберга почнеться танення
        public const float TimeBetweenBreaks = 3f;   // секунд між відколами
        public const float WarningTime = 1.5f;       // скільки шматок тремтить перед падінням
        public const float Randomness = 4f;          // 0 = строго від краю до центру, 3-5 = нерівно
    }
}