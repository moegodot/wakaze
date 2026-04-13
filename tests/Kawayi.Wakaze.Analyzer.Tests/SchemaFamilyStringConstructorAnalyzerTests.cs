namespace Kawayi.Wakaze.Analyzer.Tests;

public class SchemaFamilyStringConstructorAnalyzerTests
{
    [Test]
    public async Task Valid_String_Literal_Does_Not_Report()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

                     var family = new SchemaFamily("semantic://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task Invalid_String_Literal_Reports_AB0001()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

                     var family = new SchemaFamily("semantic://wakaze.dev");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Invalid_Const_String_Reports_AB0001()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

                     const string familyText = "semantic://wakaze.dev/tag/";
                     var family = new SchemaFamily(familyText);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Invalid_Constant_Interpolated_String_Reports_AB0001()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

                     const string scheme = "semantic";
                     const string suffix = "";
                     var family = new SchemaFamily($"{scheme}://wakaze.dev{suffix}");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }

    [Test]
    public async Task Null_Constant_Reports_AB0001()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

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
                     using Kawayi.Wakaze.Abstractions;

                     string scheme = "semantic";
                     var family = new SchemaFamily($"{scheme}://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TargetTyped_New_Reports_AB0001()
    {
        var source = """
                     using Kawayi.Wakaze.Abstractions;

                     SchemaFamily family = new("semantic://wakaze.dev");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaFamilyRuleId);
    }
}
