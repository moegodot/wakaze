using System.Text.Json.Serialization;

namespace Kawayi.Wakaze.Db.PostgreSql;

internal sealed record PostgreSqlDumpManifest(
    [property: JsonPropertyName("providerId")]
    string ProviderId,
    [property: JsonPropertyName("engine")] string Engine,
    [property: JsonPropertyName("locationKind")]
    string LocationKind,
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("port")] int? Port,
    [property: JsonPropertyName("databaseName")]
    string DatabaseName,
    [property: JsonPropertyName("archiveFileName")]
    string ArchiveFileName,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("createdUtc")]
    DateTimeOffset CreatedUtc);
