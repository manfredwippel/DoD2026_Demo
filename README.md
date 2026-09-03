# DoD 2026 Demo

Console application for creating and removing the demo objects in Snowflake.

## Prerequisites

- .NET 10 SDK
- A Snowflake account with a role that can create and remove the required objects

## Configure local credentials

For the demo to run, set both `SnowflakeAdmin:User` and `SnowflakeAdmin:Password` in either `DoD2026_Demo/appsettings.json` or `DoD2026_Demo/appsettings.local.json`.

General settings are stored in `DoD2026_Demo/appsettings.json`. Passwords and other secrets should be stored in:

`DoD2026_Demo/appsettings.local.json`

Example:

```json
{
  "SnowflakeAdmin": {
	"User": "<SNOWFLAKE-ADMIN-USER>",
	"Password": "<SNOWFLAKE-ADMIN-PASSWORD>"
  },
  "Snowflake": {
	"ProgrammaticAccessToken": "<OPTIONAL-PAT>"
  }
}
```

Values from `appsettings.local.json` override the corresponding values from `appsettings.json`. Do not commit the local file to version control.

## Run the setup

From the repository root:

```powershell
dotnet run --project DoD2026_Demo
```

The setup creates the Snowflake demo objects, generates a Programmatic Access Token (PAT), stores it in `DoD2026_Demo/appsettings.local.json`, and then validates the demo user's connection.

## Run the cleanup

```powershell
dotnet run --project DoD2026_Demo -- --cleanup
```

Alternatively, `cleanup` is accepted without leading dashes:

```powershell
dotnet run --project DoD2026_Demo -- cleanup
```
