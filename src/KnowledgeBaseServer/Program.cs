using System;
using System.IO;
using System.Threading;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args is ["--init-db", ..])
{
    if (args is not ["--init-db", _])
    {
        Console.WriteLine("Usage: dotnet run --init-db <path>");
        return 1;
    }

    // Initialize the database at the specified path
    return 0;
}

var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "default.sqlite";
var defaultPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "KnowledgeBaseServer",
    databaseName
);
var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? defaultPath;

if (Path.GetDirectoryName(databasePath) is { Length: > 0 } directory && !Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
}

var connectionString = $"Data Source={Path.Join(databasePath, databaseName)};";

try
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();
    connection.Execute("PRAGMA journal_mode=WAL;");
}
catch (Exception e)
{
    await Console.Error.WriteLineAsync($"Failed to open or create the database. {e.GetType().Name}: {e.Message}");
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer().WithStdioServerTransport().WithPromptsFromAssembly().WithToolsFromAssembly();

var app = builder.Build();

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};
await app.RunAsync(cancellationTokenSource.Token);
return 0;
