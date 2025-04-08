using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Dapper;
using KnowledgeBaseServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (
    Environment.GetEnvironmentVariable("LOG_LEVEL") is not { } logLevelString
    || !Enum.TryParse<LogLevel>(logLevelString, out var logLevel)
)
{
    logLevel = LogLevel.Information;
}

if (args is ["--init-db", ..])
{
    if (args is not ["--init-db", var path])
    {
        Console.WriteLine("Usage: dotnet run -- --init-db <path/to/db.sqlite>");
        return 1;
    }

    var initLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(logLevel));
    var initSuccess =
        Migrator.InitializeDatabase(initLoggerFactory, path)
        && Migrator.ApplyMigrations(initLoggerFactory, ConnectionString.Create(path));
    return initSuccess ? 0 : 1;
}

var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "knowledgebase.sqlite";
var defaultPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "KnowledgeBaseServer",
    databaseName
);
var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? defaultPath;
var connectionString = ConnectionString.Create(databasePath);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer().WithStdioServerTransport().WithPromptsFromAssembly().WithToolsFromAssembly();
builder.Services.AddSingleton(connectionString);

var app = builder.Build();

var appLoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
appLoggerFactory.CreateLogger(nameof(Program)).LogInformation("Using database at {Path}", databasePath);

var dbSetupSuccess =
    Migrator.InitializeDatabase(appLoggerFactory, databasePath)
    && Migrator.ApplyMigrations(appLoggerFactory, connectionString);
if (!dbSetupSuccess)
{
    return 1;
}

// Use WAL mode for real app databases (initialized here so that it does not affect test databases).
using (var connection = connectionString.CreateConnection())
{
    _ = connection.Execute("PRAGMA journal_mode=WAL;");
}

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};
await app.RunAsync(cancellationTokenSource.Token);
return 0;
