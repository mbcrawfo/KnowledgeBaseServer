using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace KnowledgeBaseServer;

public abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
    where T : IParsable<T>
{
    /// <inheritdoc />
    public override T? Parse(object value) => T.Parse((string)value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, T? value) => parameter.Value = value;
}

public sealed class GuidTypeHandler : SqliteTypeHandler<Guid>;

public sealed class DateTimeOffsetTypeHandler : SqliteTypeHandler<DateTimeOffset>;
