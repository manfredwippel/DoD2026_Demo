using System.Collections.Immutable;

namespace DoD2026_Snowflake
{
    public static class DatabaseService
    {
        public static async Task<TableSchema> GetTableSchemaAsync(string tableName, SnowflakeConnection connectionInfo, CancellationToken cancellationToken = default)
        {
            const string sql = """
                               SELECT c.COLUMN_NAME               AS "ColumnName",
                                      c.DATA_TYPE                 AS "DataType",
                                      c.CHARACTER_MAXIMUM_LENGTH  AS "CharacterMaximumLength",
                                      c.NUMERIC_PRECISION         AS "NumericPrecision",
                                      c.NUMERIC_SCALE             AS "NumericScale",
                                      c.IS_NULLABLE               AS "IsNullable",
                                      c.IS_IDENTITY               AS "IsIdentity",
                                      c.COLLATION_NAME            AS "Collation",
                                      c.ORDINAL_POSITION          AS "OrdinalPosition"
                               FROM INFORMATION_SCHEMA.COLUMNS c
                               WHERE c.TABLE_CATALOG = :dbName
                                 AND c.TABLE_SCHEMA  = :schemaName
                                 AND c.TABLE_NAME    = :tableName
                               ORDER BY c.ORDINAL_POSITION
                               """;

            using var queryProvider = connectionInfo.GetQueryProvider();

            var parameters = connectionInfo.CreateParameters();
            parameters.Add("dbName", connectionInfo.DbName);
            parameters.Add("schemaName", connectionInfo.Schema);
            parameters.Add("tableName", tableName);

            var rows = (await queryProvider.QueryAsync<ColumnRow>(sql, parameters, cancellationToken).ConfigureAwait(false)).ToList();

            if (rows.Count == 0)
            {
                throw new InternalServerErrorDetailsException(new ProblemDetails
                {
                    Title = "Table not found",
                    Detail = $"Table '{connectionInfo.DbName}.{connectionInfo.Schema}.{tableName}' does not exist or is not accessible.",
                    Status = 404,
                    TableName = tableName
                });
            }

            var primaryKeys = await GetPrimaryKeyColumnsAsync(queryProvider, connectionInfo, tableName, cancellationToken).ConfigureAwait(false);

            var columns = rows.Select(r => new ColumnInfo
            {
                Field = r.ColumnName,
                SnowflakeType = r.DataType,
                DotNetType = MapToDotNetType(r, tableName),
                CharacterMaximumLength = r.CharacterMaximumLength,
                NumericPrecision = r.NumericPrecision,
                NumericScale = r.NumericScale,
                IsNullable = string.Equals(r.IsNullable, "YES", StringComparison.OrdinalIgnoreCase),
                IsIdentity = string.Equals(r.IsIdentity, "YES", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = primaryKeys.Contains(r.ColumnName),
                Collation = r.Collation
            }).ToImmutableList();

            return new TableSchema
            {
                DbName = connectionInfo.DbName,
                Schema = connectionInfo.Schema,
                TableName = tableName,
                ColumnInfos = columns
            };
        }

        private static async Task<HashSet<string>> GetPrimaryKeyColumnsAsync(QueryProvider queryProvider, SnowflakeConnection connectionInfo, string tableName, CancellationToken cancellationToken)
        {
            var sql = $"SHOW PRIMARY KEYS IN TABLE \"{connectionInfo.DbName}\".\"{connectionInfo.Schema}\".\"{tableName}\"";
            var rows = await queryProvider.QueryAsync<dynamic>(sql, null, cancellationToken).ConfigureAwait(false);

            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                if (dict.TryGetValue("column_name", out var col) && col is string name)
                {
                    result.Add(name);
                }
            }

            return result;
        }

        // see https://docs.snowflake.com/en/sql-reference/intro-summary-data-types
        private static string MapToDotNetType(ColumnRow row, string tableName)
        {
            var type = row.DataType.ToUpperInvariant();

            return type switch
            {
                "NUMBER" or "DECIMAL" or "NUMERIC" when row.NumericScale is > 0 => typeof(decimal).FullName!,
                "NUMBER" or "DECIMAL" or "NUMERIC" when row.NumericPrecision is <= 18 => typeof(long).FullName!,
                "NUMBER" or "DECIMAL" or "NUMERIC" => typeof(decimal).FullName!,
                "INT" or "INTEGER" or "BIGINT" or "SMALLINT" or "TINYINT" or "BYTEINT" => typeof(long).FullName!,
                "FLOAT" or "FLOAT4" or "FLOAT8" or "DOUBLE" or "DOUBLE PRECISION" or "REAL" => typeof(double).FullName!,
                "DECFLOAT" => typeof(string).FullName!,
                "TEXT" or "VARCHAR" or "CHAR" or "CHARACTER" or "STRING" => typeof(string).FullName!,
                "BINARY" or "VARBINARY" => typeof(byte[]).FullName!,
                "BOOLEAN" => typeof(bool).FullName!,
                "DATE" => typeof(DateTime).FullName!,
                "TIME" => typeof(TimeSpan).FullName!,
                "DATETIME" or "TIMESTAMP" or "TIMESTAMP_NTZ" => typeof(DateTime).FullName!,
                "TIMESTAMP_LTZ" => typeof(DateTimeOffset).FullName!,
                "TIMESTAMP_TZ" => typeof(DateTimeOffset).FullName!,
                "UUID" => typeof(Guid).FullName!,
                "VARIANT" or "FILE" or "GEOGRAPHY" or "GEOMETRY" or "VECTOR" or "MAP" => typeof(string).FullName!,
                _ when type.StartsWith("ARRAY", StringComparison.Ordinal) => typeof(string).FullName!,
                _ when type.StartsWith("OBJECT", StringComparison.Ordinal) => typeof(string).FullName!,
                _ when type.StartsWith("MAP", StringComparison.Ordinal) => typeof(string).FullName!,
                _ when type.StartsWith("VECTOR", StringComparison.Ordinal) => typeof(string).FullName!,
                _ => throw new InternalServerErrorDetailsException(new ProblemDetails
                {
                    Title = "Snowflake column type not supported",
                    Detail = $"Column '{row.ColumnName}' of table '{tableName}' has the unknown Snowflake type '{row.DataType}'.",
                    TableName = tableName,
                    ColumnName = row.ColumnName,
                    ColumnType = row.DataType
                })
            };
        }

        private sealed record ColumnRow
        {
            public string ColumnName { get; init; } = string.Empty;
            public string DataType { get; init; } = string.Empty;
            public long? CharacterMaximumLength { get; init; }
            public int? NumericPrecision { get; init; }
            public int? NumericScale { get; init; }
            public string IsNullable { get; init; } = string.Empty;
            public string IsIdentity { get; init; } = string.Empty;
            public string? Collation { get; init; }
            public int OrdinalPosition { get; init; }
        }
    }
}
