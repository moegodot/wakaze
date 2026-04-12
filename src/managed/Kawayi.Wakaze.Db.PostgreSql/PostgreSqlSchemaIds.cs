using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Db.Abstractions;

namespace Kawayi.Wakaze.Db.PostgreSql;

internal static class PostgreSqlSchemaIds
{
    public static readonly SchemaId<DatabaseScheme> Provider = new("database://wakaze.dev/postgresql/v1");

    public static readonly SchemaId<DatabaseScheme> Engine = new("database://wakaze.dev/postgresql/v1");
}
