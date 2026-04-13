using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions;

public sealed class PluginSchema : ISchemaUriSchemeDefinition
{
    public static string UriScheme { get; } = "plugin";
}
