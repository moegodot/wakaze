#!/usr/local/share/dotnet/dotnet
using System.Diagnostics;
using System.Runtime.CompilerServices;

return await RunAsync(args);

static async Task<int> RunAsync(string[] forwardedArguments)
{
    var scriptPath = GetScriptPath();
    var managedTestsDirectory = Path.GetDirectoryName(scriptPath)
        ?? throw new InvalidOperationException("Unable to resolve the managed tests directory.");
    var repositoryRoot = Path.GetFullPath(Path.Combine(managedTestsDirectory, "..", ".."));

    var testProjects = Directory
        .EnumerateFiles(managedTestsDirectory, "*.Tests.csproj", SearchOption.AllDirectories)
        .OrderBy(static path => path, StringComparer.Ordinal)
        .ToArray();

    if (testProjects.Length == 0)
    {
        Console.Error.WriteLine($"No test projects matching '*.Tests.csproj' were found under '{managedTestsDirectory}'.");
        return 1;
    }

    var failedProjects = new List<string>();
    var hasTestSelectionArguments = HasTestSelectionArguments(forwardedArguments);

    foreach (var testProject in testProjects)
    {
        var displayPath = Path.GetRelativePath(repositoryRoot, testProject);
        Console.WriteLine($"==> Running {displayPath}");

        using var process = CreateProcess(testProject, repositoryRoot, forwardedArguments);
        if (!process.Start())
        {
            Console.Error.WriteLine($"Failed to start test project '{displayPath}'.");
            failedProjects.Add(displayPath);
            continue;
        }

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            Console.WriteLine($"<== Passed {displayPath}");
            continue;
        }

        if (hasTestSelectionArguments && process.ExitCode == 8)
        {
            Console.WriteLine($"<== Skipped {displayPath} (no matching tests)");
            continue;
        }

        Console.Error.WriteLine($"<== Failed {displayPath} (exit code {process.ExitCode})");
        failedProjects.Add(displayPath);
    }

    Console.WriteLine();
    Console.WriteLine($"Executed {testProjects.Length} test project(s).");

    if (failedProjects.Count == 0)
    {
        Console.WriteLine("All managed test projects passed.");
        return 0;
    }

    Console.Error.WriteLine($"Managed test projects failed: {string.Join(", ", failedProjects)}");
    return 1;
}

static Process CreateProcess(string testProject, string workingDirectory, IEnumerable<string> forwardedArguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
    };

    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--project");
    startInfo.ArgumentList.Add(testProject);
    startInfo.ArgumentList.Add("--");

    foreach (var argument in forwardedArguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    return new Process
    {
        StartInfo = startInfo,
    };
}

static string GetScriptPath([CallerFilePath] string path = "")
{
    return path;
}

static bool HasTestSelectionArguments(IEnumerable<string> forwardedArguments)
{
    foreach (var argument in forwardedArguments)
    {
        if (argument is "--treenode-filter" or "--filter-uid")
        {
            return true;
        }

        if (argument.StartsWith("--treenode-filter=", StringComparison.Ordinal)
            || argument.StartsWith("--filter-uid=", StringComparison.Ordinal))
        {
            return true;
        }
    }

    return false;
}
