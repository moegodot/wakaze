namespace Kawayi.Wakaze.Analyzer.Tests;

public class SchemaIdStringConstructorAnalyzerTests
{
    [Test]
    public async Task Valid_String_Literal_Does_Not_Report()
    {
        var source = """
                     var schema = new SchemaId("semantic://wakaze.dev/tag/v2");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task Invalid_String_Literal_Reports_KWA0002()
    {
        var source = """
                     var schema = new SchemaId("semantic://wakaze.dev/tag");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Invalid_Const_String_Reports_KWA0002()
    {
        var source = """
                     const string schemaText = "semantic://wakaze.dev/tag/v01";
                     var schema = new SchemaId(schemaText);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Invalid_Constant_Concatenation_Reports_KWA0002()
    {
        var source = """
                     const string prefix = "semantic://wakaze.dev/tag/";
                     const string version = "version2";
                     var schema = new SchemaId(prefix + version);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Null_Constant_Reports_KWA0002()
    {
        var source = """
                     var schema = new SchemaId(null);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task NonConstant_Value_Does_Not_Report()
    {
        var source = """
                     string version = "v2";
                     var schema = new SchemaId($"semantic://wakaze.dev/tag/{version}");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TargetTyped_New_Reports_KWA0002()
    {
        var source = """
                     SchemaId schema = new("semantic://wakaze.dev/tag/v0");
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Parse_Invalid_String_Literal_Reports_KWA0002()
    {
        var source = """
                     var schema = SchemaId.Parse("semantic://wakaze.dev/tag", null);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Parse_Valid_String_Literal_Does_Not_Report()
    {
        var source = """
                     var schema = SchemaId.Parse("semantic://wakaze.dev/tag/v2", null);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TryParse_Invalid_String_Literal_Reports_KWA0002()
    {
        var source = """
                     var result = SchemaId.TryParse("semantic://wakaze.dev/tag/v01", null, out var schema);
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }
}
