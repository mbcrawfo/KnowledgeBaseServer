using System;
using System.IO;
using System.Reflection;
using Dapper;
using DbUp;
using Microsoft.Extensions.Logging;

namespace KnowledgeBaseServer;

public static class Migrator
{
    private static readonly Assembly MigrationsAssembly = typeof(Migrator).Assembly;

    /// <summary>
    ///     Creates the database path and file if they do not exist, and puts the database in WAL mode.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool InitializeDatabase(ILoggerFactory loggerFactory, string path)
    {
        var logger = loggerFactory.CreateLogger(nameof(Migrator));

        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } directory && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = ConnectionString.Create(path).CreateConnection();
            connection.Execute("PRAGMA journal_mode=WAL;");
            return true;
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed to initialize database");
            return false;
        }
    }

    /// <summary>
    ///     Applies the migrations to the database.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static bool ApplyMigrations(ILoggerFactory loggerFactory, ConnectionString connectionString)
    {
        var logger = loggerFactory.CreateLogger(nameof(Migrator));
        var migrator = DeployChanges
            .To.SqliteDatabase(connectionString.Value)
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
