namespace Kawayi.Wakaze.Process;

/// <summary>
/// Represents the result of a process command invocation.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">The captured standard output.</param>
/// <param name="StandardError">The captured standard error.</param>
public sealed record ProcessCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
