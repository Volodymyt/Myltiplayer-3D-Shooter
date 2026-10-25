namespace Services.Networking
{
    public static class RelaySettings
    {
        public const int ProbeTimeoutMs = 5000;
        public const int HostValidationTimeoutMs = 10000;
        public const int HostValidationPollMs = 250;
        public const bool RunDiagnostics = true;
        public const bool DebugForceFirstAttemptToFail = false;

        public static readonly RelayFilter[][] HostAttempts =
        {
            null,
            Continent("Europe"),
            Continent("Asia"),
            Continent("North America"),
        };

        private static RelayFilter[] Continent(string name) =>
            new[] { new RelayFilter { field = "continent", values = new[] { name }, filter_type = "any" } };
    }
}
