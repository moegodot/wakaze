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

        foreach (var ruleId in RuleIds) builder[ruleId] = true;

        var key = "build_property.WakazeDisabledAnas";

        if (optionsProvider.GlobalOptions.TryGetValue(key, out var disabledRules))
            foreach (var rules in disabledRules.Split([';'],
                         StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
                builder[rules] = false;

        return new KwaAnalyzerOptions(builder.ToImmutable());
    }

    public bool IsEnabled(string ruleId)
    {
        return !_enabledRules.TryGetValue(ruleId, out var enabled) || enabled;
    }
}
