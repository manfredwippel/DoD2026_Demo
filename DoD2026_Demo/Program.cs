using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using DoD2026_Snowflake;
using Snowflake.Data.Client;

// Provisioning of the DoD2026 demo objects in Snowflake.
// Usage:   dotnet run --project DoD2026_Demo            (setup)
//          dotnet run --project DoD2026_Demo -- --cleanup (cleanup)

var isCleanup = args.Any(a =>
    a.Equals("--cleanup", StringComparison.OrdinalIgnoreCase) ||
    a.Equals("cleanup", StringComparison.OrdinalIgnoreCase));
var scriptName = isCleanup ? "DoD2026_Cleanup.sql" : "DoD2026_Setup.sql";

var admin = SnowflakeSettings.Load("SnowflakeAdmin");
var patName = $"DOD2026_TOKEN_{DateTime.UtcNow:yyyyMMddHHmmss}";

var scriptPath = Path.Combine(AppContext.BaseDirectory, "Setup", scriptName);
var script = (await File.ReadAllTextAsync(scriptPath))
    .Replace("{{DOD2026_PAT_NAME}}", patName);

Console.WriteLine($"Connecting as {admin.User} to account {admin.Account} ...");

using var connection = new SnowflakeDbConnection(admin.ToConnectionString());
await connection.OpenAsync();
Console.WriteLine($"Connected. Server version: {connection.ServerVersion}");

string? programmaticAccessToken = null;

foreach (var statement in SplitStatements(script))
{
    Console.WriteLine();
    Console.WriteLine($"> {statement.Split('\n')[0].Trim()}");

    using var command = connection.CreateCommand();
    command.CommandText = statement;

    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var tokenSecretOrdinal = GetOrdinal(reader, "token_secret");
        if (tokenSecretOrdinal >= 0)
        {
            programmaticAccessToken = reader.GetString(tokenSecretOrdinal);
            Console.WriteLine($"  PAT {patName} was created; the token value is not displayed.");
            continue;
        }

        var values = new object[reader.FieldCount];
        reader.GetValues(values);
        Console.WriteLine("  " + string.Join(" | ", values.Take(6).Select(v => v?.ToString())));
    }
}

if (!isCleanup)
{
    if (string.IsNullOrWhiteSpace(programmaticAccessToken))
    {
        throw new InvalidOperationException("Snowflake did not return a PAT.");
    }

    var localSettingsPath = GetLocalSettingsPath();
    var localSettings = File.Exists(localSettingsPath)
        ? JsonNode.Parse(await File.ReadAllTextAsync(localSettingsPath)) as JsonObject
            ?? throw new InvalidOperationException($"Local configuration must contain a JSON object: {localSettingsPath}")
        : new JsonObject();
    var snowflakeSettings = localSettings["Snowflake"] as JsonObject ?? new JsonObject();
    localSettings["Snowflake"] = snowflakeSettings;
    snowflakeSettings["ProgrammaticAccessToken"] = programmaticAccessToken;

    await File.WriteAllTextAsync(
        localSettingsPath,
        JsonSerializer.Serialize(localSettings, new JsonSerializerOptions { WriteIndented = true }));

    var demoUser = SnowflakeSettings.Load(basePath: Path.GetDirectoryName(localSettingsPath));
    using var validationConnection = new SnowflakeDbConnection(demoUser.ToConnectionString());
    await validationConnection.OpenAsync();
    Console.WriteLine($"PAT connection as {demoUser.User} validated successfully.");
    Console.WriteLine($"Local configuration: {localSettingsPath}");
}

Console.WriteLine();
Console.WriteLine(isCleanup ? "Cleanup completed." : "Setup completed.");

static int GetOrdinal(System.Data.Common.DbDataReader reader, string columnName)
{
    for (var index = 0; index < reader.FieldCount; index++)
    {
        if (reader.GetName(index).Equals(columnName, StringComparison.OrdinalIgnoreCase))
        {
            return index;
        }
    }

    return -1;
}

static string GetLocalSettingsPath()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
        currentDirectory,
        Path.Combine(currentDirectory, "DoD2026_Demo")
    };

    var settingsDirectory = candidates.FirstOrDefault(directory => File.Exists(Path.Combine(directory, "appsettings.json")))
        ?? throw new InvalidOperationException("Source directory with appsettings.json was not found.");

    return Path.Combine(settingsDirectory, "appsettings.local.json");
}

static IEnumerable<string> SplitStatements(string script)
{
    var statement = new StringBuilder();
    var inString = false;
    var inLineComment = false;

    for (var index = 0; index < script.Length; index++)
    {
        var current = script[index];
        var next = index + 1 < script.Length ? script[index + 1] : '\0';

        if (inLineComment)
        {
            if (current is '\r' or '\n')
            {
                inLineComment = false;
                statement.Append(current);
            }

            continue;
        }

        if (!inString && current == '-' && next == '-')
        {
            inLineComment = true;
            index++;
            continue;
        }

        if (current == '\'')
        {
            statement.Append(current);
            if (inString && next == '\'')
            {
                statement.Append(next);
                index++;
                continue;
            }

            inString = !inString;
            continue;
        }

        if (!inString && current == ';')
        {
            var sql = statement.ToString().Trim();
            statement.Clear();
            if (sql.Length > 0)
            {
                yield return sql;
            }

            continue;
        }

        statement.Append(current);
    }

    var remainingSql = statement.ToString().Trim();
    if (remainingSql.Length > 0)
    {
        yield return remainingSql;
    }
}
