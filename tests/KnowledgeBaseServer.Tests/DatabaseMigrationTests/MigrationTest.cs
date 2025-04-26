using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KnowledgeBaseServer.Tests.DatabaseMigrationTests;

[SuppressMessage(
    "Major Code Smell",
    "S3881:\"IDisposable\" should be implemented correctly",
    Justification = "Disposal is handled by XUnit, we don't need the full pattern"
)]
public abstract class MigrationTest : IDisposable
{
    // We must hold a connection open for the lifetime of the test, otherwise the in-memory database will be destroyed.
    private readonly IDbConnection _connection;

    protected MigrationTest(string version, ITestOutputHelper outputHelper)
    {
        LogFactory = LoggerFactory.Create(builder =>
        {
            builder.AddXunit(outputHelper);
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        ConnectionString = new ConnectionString($"Data Source={version};Mode=Memory;Cache=Shared;Foreign Keys=True;");
        _connection = ConnectionString.CreateConnection();

        using var migrationConnection = new SqliteConnection(
            $"Data Source=./DatabaseMigrationTests/databases/{version}.sqlite;"
        );
        migrationConnection.Open();
        migrationConnection.BackupDatabase((SqliteConnection)_connection);
    }

    protected ConnectionString ConnectionString { get; }

    protected ILoggerFactory LogFactory { get; }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();
}
