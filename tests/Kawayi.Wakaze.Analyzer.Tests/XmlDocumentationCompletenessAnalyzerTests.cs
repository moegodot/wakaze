namespace Kawayi.Wakaze.Analyzer.Tests;

public class XmlDocumentationCompletenessAnalyzerTests
{
    [Test]
    public async Task Public_Type_Without_Summary_Reports_KWA0005()
    {
        var source = """
                     public sealed class DemoType
                     {
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingDocumentationRuleId);
    }

    [Test]
    public async Task Public_Member_Without_Summary_Reports_KWA0005()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         public void Run()
                         {
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingDocumentationRuleId);
    }

    [Test]
    public async Task Missing_Param_Documentation_Reports_KWA0006()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Runs the operation.</summary>
                         public void Run(string value)
                         {
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingParamRuleId);
    }

    [Test]
    public async Task Missing_TypeParam_Documentation_Reports_KWA0007()
    {
        var source = """
                     /// <summary>Represents a generic demo type.</summary>
                     public sealed class DemoType<TValue>
                     {
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingTypeParamRuleId);
    }

    [Test]
    public async Task Missing_Returns_Documentation_Reports_KWA0008()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Gets the count.</summary>
                         public int Count()
                         {
                             return 1;
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingReturnsRuleId);
    }

    [Test]
    public async Task Missing_Value_Documentation_Reports_KWA0009()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Gets the name.</summary>
                         public string Name => "demo";
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingValueRuleId);
    }

    [Test]
    public async Task Missing_Exception_Documentation_Reports_KWA0010()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Runs the operation.</summary>
                         /// <param name="value">The value to validate.</param>
                         public void Run(string value)
                         {
                             ArgumentNullException.ThrowIfNull(value);
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingExceptionRuleId);
    }

    [Test]
    public async Task Inheritdoc_Suppresses_All_Xml_Documentation_Diagnostics()
    {
        var source = """
                     /// <summary>Base type.</summary>
                     public abstract class BaseType
                     {
                         /// <summary>Runs the operation.</summary>
                         /// <param name="value">The incoming value.</param>
                         /// <returns>The resulting length.</returns>
                         public abstract int Run(string value);
                     }

                     /// <inheritdoc />
                     public sealed class DemoType : BaseType
                     {
                         /// <inheritdoc />
                         public override int Run(string value)
                         {
                             return value.Length;
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(source);
    }

    [Test]
    public async Task Internal_Type_Public_Member_Does_Not_Report()
    {
        var source = """
                     internal sealed class DemoType
                     {
                         public void Run()
                         {
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(source);
    }

    [Test]
    public async Task Same_Member_Can_Report_Multiple_Xml_Documentation_Diagnostics()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Runs the operation.</summary>
                         public TResult Run<TResult>(string value)
                         {
                             throw new ArgumentException("bad", nameof(value));
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            XmlDocumentationCompletenessAnalyzer.MissingParamRuleId,
            XmlDocumentationCompletenessAnalyzer.MissingTypeParamRuleId,
            XmlDocumentationCompletenessAnalyzer.MissingReturnsRuleId,
            XmlDocumentationCompletenessAnalyzer.MissingExceptionRuleId);
    }

    [Test]
    public async Task Indirect_Exception_Source_Does_Not_Report_KWA0010()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         /// <summary>Runs the operation.</summary>
                         /// <param name="value">The incoming value.</param>
                         public void Run(string value)
                         {
                             Validate(value);
                         }

                         private static void Validate(string value)
                         {
                             ArgumentNullException.ThrowIfNull(value);
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(source);
    }

    [Test]
    public async Task EnableKWA0005_False_Suppresses_Only_KWA0005()
    {
        var source = """
                     /// <summary>Demo type.</summary>
                     public sealed class DemoType
                     {
                         public void Run(string value)
                         {
                         }
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyXmlDocumentationAsync(
            source,
            new Dictionary<string, string>
            {
                ["WakazeDisabledAnas"] = "KWA0005"
            },
            XmlDocumentationCompletenessAnalyzer.MissingParamRuleId);
    }
}
