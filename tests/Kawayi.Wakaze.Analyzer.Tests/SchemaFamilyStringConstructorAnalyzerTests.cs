namespace Kawayi.Wakaze.Analyzer.Tests;

public class SchemaFamilyStringConstructorAnalyzerTests
{
    [Test]
    public async Task Valid_String_Literal_Does_Not_Report()
    {
        var source = """
                     var family = new SchemaFamily("semantic://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task Invalid_String_Literal_Reports_KWA0001()
    {
        var source = """
                     var family = new SchemaFamily("semantic://wakaze.dev");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Invalid_Const_String_Reports_KWA0001()
    {
        var source = """
                     const string familyText = "semantic://wakaze.dev/tag/";
                     var family = new SchemaFamily(familyText);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Invalid_Constant_Interpolated_String_Reports_KWA0001()
    {
        var source = """
                     const string scheme = "semantic";
                     const string suffix = "";
                     var family = new SchemaFamily($"{scheme}://wakaze.dev{suffix}");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Null_Constant_Reports_KWA0001()
    {
        var source = """
                     const string familyText = null;
                     var family = new SchemaFamily(familyText);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task NonConstant_Value_Does_Not_Report()
    {
        var source = """
                     string scheme = "semantic";
                     var family = new SchemaFamily($"{scheme}://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TargetTyped_New_Reports_KWA0001()
    {
        var source = """
                     SchemaFamily family = new("semantic://wakaze.dev");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Parse_Invalid_String_Literal_Reports_KWA0001()
    {
        var source = """
                     var family = SchemaFamily.Parse("semantic://wakaze.dev", null);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Parse_Valid_String_Literal_Does_Not_Report()
    {
        var source = """
                     var family = SchemaFamily.Parse("semantic://wakaze.dev/tag", null);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TryParse_Invalid_String_Literal_Reports_KWA0001()
    {
        var source = """
                     var result = SchemaFamily.TryParse("semantic://wakaze.dev/tag/", null, out var family);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task EnableKWA0001_False_Suppresses_Only_KWA0001()
    {
        var source = """
                     var family = new SchemaFamily("semantic://wakaze.dev");
                     var schema = new SchemaId("semantic://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            new Dictionary<string, string>
            {
                ["WakazeDisabledAnas"] = "KWA0001"
            },
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }
}
