using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace KnowledgeBaseServer;

public sealed record ConnectionString(string Value)
{
    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(Value);
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }

    public static ConnectionString Create(string path) => new($"Data Source={path};");
}
