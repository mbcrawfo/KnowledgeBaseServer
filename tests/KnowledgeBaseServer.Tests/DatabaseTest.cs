using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeBaseServer.Tests;

[SuppressMessage(
    "Major Code Smell",
    "S3881:\"IDisposable\" should be implemented correctly",
    Justification = "Disposal is handled by XUnit, we don't need the full pattern"
)]
public abstract class DatabaseTest : IDisposable
{
    private static int _counter = 1;

    // We must hold a connection open for the lifetime of the test, otherwise the in-memory database will be destroyed.
    private readonly IDbConnection _connection;

    private readonly string _fileName = $"testdb{_counter++}";

    protected DatabaseTest()
    {
        ConnectionString = new ConnectionString($"Data Source={_fileName};Mode=Memory;Cache=Shared;Foreign Keys=True;");
        _connection = ConnectionString.CreateConnection();

        if (!Migrator.ApplyMigrations(new NullLoggerFactory(), ConnectionString))
        {
            throw new InvalidOperationException("Failed to apply migrations");
        }
    }

    protected ConnectionString ConnectionString { get; }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();
}
