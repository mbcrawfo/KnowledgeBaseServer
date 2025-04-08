using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace KnowledgeBaseServer;

public sealed record ConnectionString(string Value)
{
    // A bit hacky but this class is referenced by both the main app and tests, so we can ensure typ handlers are
    // registered in both places before any queries run.
    static ConnectionString()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(Value);
        connection.Open();
        return connection;
    }

    public static ConnectionString Create(string path) => new($"Data Source={path};Foreign Keys=True;");
}
