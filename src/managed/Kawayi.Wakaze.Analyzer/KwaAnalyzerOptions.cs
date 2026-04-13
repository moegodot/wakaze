using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer;

internal sealed class KwaAnalyzerOptions
{
    private readonly ImmutableDictionary<string, bool> _enabledRules;

    private KwaAnalyzerOptions(ImmutableDictionary<string, bool> enabledRules)
    {
        _enabledRules = enabledRules;
    }

    public static ImmutableArray<string> RuleIds { get; } =
    [
        "KWA0001",
        "KWA0002",
        "KWA0003",
        "KWA0004",
        "KWA0005",
        "KWA0006",
        "KWA0007",
        "KWA0008",
        "KWA0009",
        "KWA0010"
    ];

    public static KwaAnalyzerOptions Create(AnalyzerConfigOptionsProvider optionsProvider)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, bool>(StringComparer.Ordinal);

        foreach (var ruleId in RuleIds)
        {
            var key = "build_property." + GetPropertyName(ruleId);

            if (optionsProvider.GlobalOptions.TryGetValue(key, out var value) &&
                bool.TryParse(value, out var enabled))
            {
                builder[ruleId] = enabled;
                continue;
            }

            builder[ruleId] = true;
        }

        return new KwaAnalyzerOptions(builder.ToImmutable());
    }

    public bool IsEnabled(string ruleId)
    {
        return !_enabledRules.TryGetValue(ruleId, out var enabled) || enabled;
    }

    public static string GetPropertyName(string ruleId)
    {
        return "Enable" + ruleId;
    }
}
