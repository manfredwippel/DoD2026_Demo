using Dapper;

namespace DoD2026_Snowflake
{
    public sealed record SnowflakeConnection
    {
        public required string ConnectionString { get; init; }
        public required string DbName { get; init; }
        public required string Schema { get; init; }

        public static SnowflakeConnection From(SnowflakeSettings settings) => new()
        {
            ConnectionString = settings.ToConnectionString(),
            DbName = settings.Database,
            Schema = settings.Schema
        };

        public QueryProvider GetQueryProvider() => new(ConnectionString);

        public DynamicParameters CreateParameters() => new();
    }
}
