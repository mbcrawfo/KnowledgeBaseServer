using System;
using System.IO;
using System.Reflection;
using Dapper;
using DbUp;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace KnowledgeBaseServer;

public static class Migrator
{
    private static readonly Assembly MigrationsAssembly = typeof(Migrator).Assembly;

    public static bool InitializeDatabase(ILoggerFactory loggerFactory, string path)
    {
        var logger = loggerFactory.CreateLogger(nameof(Migrator));
        var connectionString = $"Data Source={path};";

        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } directory && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            connection.Execute("PRAGMA journal_mode=WAL;");
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to initialize database");
            return false;
        }

        var migrator = DeployChanges
            .To.SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                MigrationsAssembly,
                name => name.StartsWith("KnowledgeBaseServer.Migrations.") && name.EndsWith(".sql")
            )
            .WithTransactionPerScript()
            .JournalToSqliteTable("_migrations_history")
            .LogTo(logger)
            .Build();

        var result = migrator.PerformUpgrade();
        if (result is { Successful: false })
        {
            logger.LogCritical(result.Error, "Failed to apply migrations");
            return false;
        }

        return true;
    }
}
