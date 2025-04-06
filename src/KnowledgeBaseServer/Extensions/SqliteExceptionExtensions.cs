using Microsoft.Data.Sqlite;

namespace KnowledgeBaseServer.Extensions;

public static class SqliteExceptionExtensions
{
    public static bool IsForeignKeyConstraintViolation(this SqliteException ex) =>
        ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 787 };

    public static bool IsPrimaryKeyConstraintViolation(this SqliteException ex) =>
        ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 };
}
