namespace Kawayi.Wakaze.Process;

/// <summary>
/// Describes a process command invocation.
/// </summary>
/// <param name="FileName">The executable path or command name.</param>
/// <param name="Arguments">The process arguments.</param>
/// <param name="WorkingDirectory">The working directory.</param>
/// <param name="CaptureOutput">Whether to capture stdout and stderr.</param>
/// <param name="EnvironmentVariables">Optional environment variable overrides.</param>
/// <param name="ThrowOnNonZeroExit">Whether to throw when the process exits with a non-zero exit code.</param>
public sealed record ProcessCommandRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    bool CaptureOutput = false,
    IReadOnlyDictionary<string, string?>? EnvironmentVariables = null,
    bool ThrowOnNonZeroExit = true);
