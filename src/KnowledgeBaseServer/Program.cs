using System;
using System.IO;
using System.Threading;
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
        Console.WriteLine("Usage: dotnet run --init-db <path>");
        return 1;
    }

    var initLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(logLevel));
    return Migrator.InitializeDatabase(initLoggerFactory, path) ? 0 : 1;
}

var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "default.sqlite";
var defaultPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "KnowledgeBaseServer",
    databaseName
);
var databasePath = Environment.GetEnvironmentVariable("DATABASE_PATH") ?? defaultPath;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer().WithStdioServerTransport().WithPromptsFromAssembly().WithToolsFromAssembly();
builder.Services.AddSingleton(new ConnectionString($"Data Source={databasePath};"));

var app = builder.Build();

var appLoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
appLoggerFactory.CreateLogger(nameof(Program)).LogInformation("Using database at {Path}", databasePath);

if (!Migrator.InitializeDatabase(appLoggerFactory, databasePath))
{
    return 1;
}

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};
await app.RunAsync(cancellationTokenSource.Token);
return 0;
