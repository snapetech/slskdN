// <copyright file="Obfs4VersionChecker.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Diagnostics;

namespace slskd.Common.Security;

/// <summary>
/// Default implementation that runs the obfs4proxy binary with --version.
/// </summary>
public sealed class Obfs4VersionChecker : IObfs4VersionChecker
{
    /// <inheritdoc />
    public async Task<int> RunVersionCheckAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        process.Start();
        var stdoutDrain = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);
        var stderrDrain = process.StandardError.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);

        try
        {
            await Task.WhenAll(
                process.WaitForExitAsync(cts.Token),
                stdoutDrain,
                stderrDrain).ConfigureAwait(false);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)
                .ConfigureAwait(false);
            await Task.WhenAll(stdoutDrain, stderrDrain)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }

        return process.ExitCode;
    }
}
