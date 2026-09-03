using System.Data;
using Dapper;
using Snowflake.Data.Client;

namespace DoD2026_Snowflake
{
    /// <summary>
    /// Thin wrapper around <see cref="SnowflakeDbConnection"/> (snowflake-connector-net) with Dapper mapping.
    /// </summary>
    public sealed class QueryProvider : IDisposable
    {
        private readonly SnowflakeDbConnection _connection;

        public QueryProvider(string connectionString)
        {
            _connection = new SnowflakeDbConnection(connectionString);
        }

        public async Task<int> ExecuteAsync(string sql, DynamicParameters? parameters = null, CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            return await _connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, DynamicParameters? parameters = null, CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            return await _connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, DynamicParameters? parameters = null, CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            return await _connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose() => _connection.Dispose();
    }
}
