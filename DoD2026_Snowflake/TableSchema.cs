using System.Collections.Immutable;

namespace DoD2026_Snowflake
{
    public sealed record ColumnInfo
    {
        public required string Field { get; init; }
        public required string SnowflakeType { get; init; }
        public required string DotNetType { get; init; }
        public long? CharacterMaximumLength { get; init; }
        public int? NumericPrecision { get; init; }
        public int? NumericScale { get; init; }
        public bool IsNullable { get; init; }
        public bool IsIdentity { get; init; }
        public bool IsPrimaryKey { get; init; }
        public string? Collation { get; init; }
    }

    public sealed record TableSchema
    {
        public required string DbName { get; init; }
        public required string Schema { get; init; }
        public required string TableName { get; init; }
        public ImmutableList<ColumnInfo> ColumnInfos { get; init; } = ImmutableList<ColumnInfo>.Empty;
    }

    /// <summary>
    /// Comparison-friendly view of <see cref="TableSchema"/> (without connection details in the snapshot).
    /// </summary>
    public sealed record SnowflakeTableSchema
    {
        public required string DbName { get; init; }
        public required string Schema { get; init; }
        public required string TableName { get; init; }
        public ImmutableList<ColumnInfo> ColumnInfos { get; init; } = ImmutableList<ColumnInfo>.Empty;
    }
}
