using Kawayi.Wakaze.Process;

namespace Kawayi.Wakaze.Process.Tests;

public sealed class ProcessCommandRunnerTests
{
    [Test]
    public async Task RunAsync_ReturnsExitCodeForSuccessfulCommand()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine("ok");
                                                                 """);

        var result = await ProcessCommandRunner.RunAsync(
            script.CreateRequest(true, true));

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput.Contains("ok", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RunAsync_CapturesStandardOutputAndError()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine("stdout-line");
                                                                 Console.Error.WriteLine("stderr-line");
                                                                 """);

        var result = await ProcessCommandRunner.RunAsync(
            script.CreateRequest(true, true));

        await Assert.That(result.StandardOutput.Contains("stdout-line", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.StandardError.Contains("stderr-line", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RunAsync_AppliesEnvironmentVariableOverrides()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine(Environment.GetEnvironmentVariable("WAKAZE_PROCESS_TEST") ?? "<null>");
                                                                 """);

        var request = script.CreateRequest(true, true) with
        {
            EnvironmentVariables = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["WAKAZE_PROCESS_TEST"] = "override"
            }
        };

        var result = await ProcessCommandRunner.RunAsync(request);

        await Assert.That(result.StandardOutput.Contains("override", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RunAsync_RemovesEnvironmentVariables()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine(Environment.GetEnvironmentVariable("WAKAZE_PROCESS_TEST") ?? "<null>");
                                                                 """);

        Environment.SetEnvironmentVariable("WAKAZE_PROCESS_TEST", "ambient");
        try
        {
            var request = script.CreateRequest(true, true) with
            {
                EnvironmentVariables = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["WAKAZE_PROCESS_TEST"] = null
                }
            };

            var result = await ProcessCommandRunner.RunAsync(request);

            await Assert.That(result.StandardOutput.Contains("<null>", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("WAKAZE_PROCESS_TEST", null);
        }
    }

    [Test]
    public async Task RunAsync_ReturnsResultForNonZeroExitWhenThrowOnNonZeroExitIsFalse()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine("stdout-fail");
                                                                 Console.Error.WriteLine("stderr-fail");
                                                                 return 7;
                                                                 """);

        var result = await ProcessCommandRunner.RunAsync(
            script.CreateRequest(true, false));

        await Assert.That(result.ExitCode).IsEqualTo(7);
        await Assert.That(result.StandardOutput.Contains("stdout-fail", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.StandardError.Contains("stderr-fail", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RunAsync_ThrowsForNonZeroExitWhenThrowOnNonZeroExitIsTrue()
    {
        using var script = await TemporaryFileScript.CreateAsync("""
                                                                 Console.WriteLine("stdout-fail");
                                                                 Console.Error.WriteLine("stderr-fail");
                                                                 return 9;
                                                                 """);

        await Assert.That(async () => await ProcessCommandRunner.RunAsync(
                script.CreateRequest(false, true)))
            .Throws<InvalidOperationException>();
    }

    private sealed class TemporaryFileScript : IDisposable
    {
        private TemporaryFileScript(string scriptPath)
        {
            ScriptPath = scriptPath;
        }

        public string ScriptPath { get; }

        public static async Task<TemporaryFileScript> CreateAsync(string body)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"wakaze-process-tests-{Guid.NewGuid():N}.cs");
            var contents = $$"""
                             #!/usr/bin/env dotnet

                             {{body}}
                             """;
            await File.WriteAllTextAsync(scriptPath, contents);
            return new TemporaryFileScript(scriptPath);
        }

        public ProcessCommandRequest CreateRequest(bool captureOutput, bool throwOnNonZeroExit)
        {
            return new ProcessCommandRequest(
                "dotnet",
                ["run", "--file", ScriptPath, "--"],
                Path.GetTempPath(),
                captureOutput,
                null,
                throwOnNonZeroExit);
        }

        public void Dispose()
        {
            if (File.Exists(ScriptPath)) File.Delete(ScriptPath);
        }
    }
}
