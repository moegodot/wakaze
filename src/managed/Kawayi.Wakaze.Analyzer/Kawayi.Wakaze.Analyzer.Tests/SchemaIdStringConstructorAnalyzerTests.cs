namespace Kawayi.Wakaze.Analyzer.Tests;

public class SchemaIdStringConstructorAnalyzerTests
{
    [Test]
    public async Task Valid_String_Literal_Does_Not_Report()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            var schema = new SchemaId("semantic://wakaze.dev/tag/v2");
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task Invalid_String_Literal_Reports_AB0002()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            var schema = new SchemaId("semantic://wakaze.dev/tag");
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source, SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Invalid_Const_String_Reports_AB0002()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            const string schemaText = "semantic://wakaze.dev/tag/v01";
            var schema = new SchemaId(schemaText);
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source, SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Invalid_Constant_Concatenation_Reports_AB0002()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            const string prefix = "semantic://wakaze.dev/tag/";
            const string version = "version2";
            var schema = new SchemaId(prefix + version);
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source, SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task Null_Constant_Reports_AB0002()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            var schema = new SchemaId(null);
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source, SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }

    [Test]
    public async Task NonConstant_Value_Does_Not_Report()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            string version = "v2";
            var schema = new SchemaId($"semantic://wakaze.dev/tag/{version}");
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TargetTyped_New_Reports_AB0002()
    {
        var source = """
            using Kawayi.Wakaze.Abstractions;

            SchemaId schema = new("semantic://wakaze.dev/tag/v0");
            """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source, SchemaStringConstructorAnalyzer.SchemaIdRuleId);
    }
}
