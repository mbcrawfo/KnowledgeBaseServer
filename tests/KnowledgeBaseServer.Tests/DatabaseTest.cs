using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

    private readonly string _fileName = $"testdb{_counter++}.sqlite";

    protected DatabaseTest()
    {
        ConnectionString = ConnectionString.Create(_fileName);

        if (!Migrator.ApplyMigrations(new NullLoggerFactory(), ConnectionString))
        {
            throw new InvalidOperationException("Failed to apply migrations");
        }
    }

    protected ConnectionString ConnectionString { get; }

    /// <inheritdoc />
    public void Dispose() => File.Delete(_fileName);
}
