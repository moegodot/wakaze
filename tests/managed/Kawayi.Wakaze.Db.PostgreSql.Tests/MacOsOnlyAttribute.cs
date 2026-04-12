using TUnit.Core;

namespace Kawayi.Wakaze.Db.PostgreSql.Tests;

public sealed class MacOsOnlyAttribute : SkipAttribute
{
    public MacOsOnlyAttribute()
        : base("This test requires macOS and otool.")
    {
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext testContext)
    {
        return Task.FromResult(!OperatingSystem.IsMacOS());
    }
}
