using Microsoft.Extensions.Configuration;

namespace DoD2026_Snowflake
{
    public sealed record SnowflakeSettings
    {
        public const string SectionName = "Snowflake";

        public string Account { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string ProgrammaticAccessToken { get; init; } = string.Empty;
        public string Role { get; init; } = "DOD_DEVELOPER";
        public string Warehouse { get; init; } = "DOD_WAREHOUSE";
        public string Database { get; init; } = "DOD2026_DB";
        public string Schema { get; init; } = "DOD2026_DEMO";

        /// <summary>Maximum number of pooled sessions (connector default: 10).</summary>
        public int MaxPoolSize { get; init; } = 50;

        /// <summary>Wait time in seconds for a free session in the pool (connector default: 30).</summary>
        public int WaitingForIdleSessionTimeoutSeconds { get; init; } = 120;

        public string ToConnectionString()
        {
            var credential = !string.IsNullOrWhiteSpace(ProgrammaticAccessToken)
                ? ProgrammaticAccessToken
                : Password;

            var parts = new List<string>
            {
                $"account={Account}",
                $"user={User}",
                $"password={credential}",
                $"maxPoolSize={MaxPoolSize}",
                $"waitingForIdleSessionTimeout={WaitingForIdleSessionTimeoutSeconds}s"
            };

            if (!string.IsNullOrWhiteSpace(Role)) parts.Add($"role={Role}");
            if (!string.IsNullOrWhiteSpace(Warehouse)) parts.Add($"warehouse={Warehouse}");
            if (!string.IsNullOrWhiteSpace(Database)) parts.Add($"db={Database}");
            if (!string.IsNullOrWhiteSpace(Schema)) parts.Add($"schema={Schema}");

            return string.Join(';', parts) + ";";
        }

        public static SnowflakeSettings Load(string sectionName = SectionName, string? basePath = null)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath ?? AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables("DOD2026_")
                .Build();

            var settings = configuration.GetSection(sectionName).Get<SnowflakeSettings>()
                           ?? throw new InvalidOperationException($"Configuration section '{sectionName}' is missing.");

            var credentialCount = new[] { settings.Password, settings.ProgrammaticAccessToken }
                .Count(value => !string.IsNullOrWhiteSpace(value));

            if (string.IsNullOrWhiteSpace(settings.Account) || string.IsNullOrWhiteSpace(settings.User) || credentialCount != 1)
            {
                throw new InvalidOperationException(
                    $"Snowflake credentials in '{sectionName}' require Account, User, and exactly one of Password or ProgrammaticAccessToken.");
            }

            return settings;
        }
    }
}
