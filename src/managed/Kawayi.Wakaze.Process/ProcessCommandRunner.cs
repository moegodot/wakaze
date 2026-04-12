using System.ComponentModel;
using System.Diagnostics;

namespace Kawayi.Wakaze.Process;

/// <summary>
/// Executes child processes with optional output capture and exit-code validation.
/// </summary>
public static class ProcessCommandRunner
{
    /// <summary>
    /// Runs a process command.
    /// </summary>
    /// <param name="request">The process command request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The process command result.</returns>
    public static async Task<ProcessCommandResult> RunAsync(
        ProcessCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        ArgumentNullException.ThrowIfNull(request.Arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkingDirectory);

        var shouldCapture = request.CaptureOutput || request.ThrowOnNonZeroExit;
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory,
            RedirectStandardOutput = shouldCapture,
            RedirectStandardError = shouldCapture,
            UseShellExecute = false
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var pair in request.EnvironmentVariables)
            {
                if (pair.Value is null)
                {
                    startInfo.Environment.Remove(pair.Key);
                }
                else
                {
                    startInfo.Environment[pair.Key] = pair.Value;
                }
            }
        }

        using var process = new global::System.Diagnostics.Process
        {
            StartInfo = startInfo
        };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException(
                $"Unable to start process '{request.FileName}'.",
                ex);
        }

        string standardOutput = string.Empty;
        string standardError = string.Empty;

        if (shouldCapture)
        {
            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            standardOutput = await standardOutputTask;
            standardError = await standardErrorTask;
        }
        else
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        var result = new ProcessCommandResult(
            process.ExitCode,
            request.CaptureOutput ? standardOutput : string.Empty,
            request.CaptureOutput ? standardError : string.Empty);

        if (request.ThrowOnNonZeroExit && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{Path.GetFileName(request.FileName)}' exited with code {process.ExitCode}.{Environment.NewLine}" +
                $"stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}" +
                $"stderr:{Environment.NewLine}{standardError}");
        }

        return result;
    }
}
