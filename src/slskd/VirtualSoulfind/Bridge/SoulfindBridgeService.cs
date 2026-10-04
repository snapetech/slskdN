// <copyright file="SoulfindBridgeService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.Bridge;

using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using slskd;
using OptionsModel = slskd.Options;

/// <summary>
/// Interface for Soulfind bridge service lifecycle management.
/// </summary>
public interface ISoulfindBridgeService
{
    /// <summary>
    /// Is the bridge service running?
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Start the Soulfind bridge.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stop the Soulfind bridge.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Get bridge health status.
    /// </summary>
    Task<BridgeHealthStatus> GetHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Record a live client connection for bridge health reporting.
    /// </summary>
    void RecordClientConnection(string clientId);

    /// <summary>
    /// Record a live client disconnection for bridge health reporting.
    /// </summary>
    void RecordClientDisconnection(string clientId);
}

/// <summary>
/// Bridge health status.
/// </summary>
public class BridgeHealthStatus
{
    public bool IsHealthy { get; set; }
    public string? Version { get; set; }
    public int ActiveConnections { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Soulfind bridge service - allows legacy clients to use VSF mesh.
/// </summary>
public sealed class SoulfindBridgeService : ISoulfindBridgeService, IAsyncDisposable
{
    private readonly ILogger<SoulfindBridgeService> logger;
    private readonly IOptionsMonitor<OptionsModel> optionsMonitor;
    private readonly ConcurrentDictionary<string, byte> connectedClients = new();
    private readonly SemaphoreSlim lifecycleSemaphore = new(1, 1);
    private bool isRunning;
    private DateTimeOffset? startedAt;
    private Process? soulfindProcess;
    private Task? stdoutDrainTask;
    private Task? stderrDrainTask;
    private int disposed;

    public SoulfindBridgeService(
        ILogger<SoulfindBridgeService> logger,
        IOptionsMonitor<OptionsModel> optionsMonitor)
    {
        this.logger = logger;
        this.optionsMonitor = optionsMonitor;
    }

    public bool IsRunning => isRunning && soulfindProcess is { HasExited: false };

    public async Task StartAsync(CancellationToken ct)
    {
        await lifecycleSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);

            if (IsRunning)
            {
                logger.LogWarning("[VSF-BRIDGE] Bridge already running");
                return;
            }

            if (soulfindProcess is not null)
            {
                await StopProcessAsync().ConfigureAwait(false);
            }

            var options = optionsMonitor.CurrentValue;
            if (options.VirtualSoulfind?.Bridge?.Enabled != true)
            {
                logger.LogInformation("[VSF-BRIDGE] Bridge disabled in configuration");
                return;
            }

            logger.LogInformation("[VSF-BRIDGE] Starting Soulfind bridge service");

            try
            {
                // Start Soulfind in proxy mode.
                var soulfindPath = string.IsNullOrWhiteSpace(options.VirtualSoulfind.Bridge.SoulfindPath)
                    ? "soulfind"
                    : options.VirtualSoulfind.Bridge.SoulfindPath;
                var bridgePort = options.VirtualSoulfind.Bridge.Port > 0
                    ? options.VirtualSoulfind.Bridge.Port
                    : 2242;

                var startInfo = new ProcessStartInfo
                {
                    FileName = soulfindPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("--port");
                startInfo.ArgumentList.Add(bridgePort.ToString(System.Globalization.CultureInfo.InvariantCulture));

                // Set PROXY_MODE environment variable.
                startInfo.Environment["PROXY_MODE"] = "true";
                startInfo.Environment["SLSKDN_API_URL"] = $"http://localhost:{options.Web?.Port ?? 5030}";

                soulfindProcess = Process.Start(startInfo);

                if (soulfindProcess is null)
                {
                    throw new InvalidOperationException("Failed to start Soulfind process.");
                }

                stdoutDrainTask = soulfindProcess.StandardOutput.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);
                stderrDrainTask = soulfindProcess.StandardError.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);

                // Wait for startup while continuing to drain both redirected pipes.
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                if (soulfindProcess.HasExited)
                {
                    throw new InvalidOperationException($"Soulfind exited during startup with code {soulfindProcess.ExitCode}.");
                }

                isRunning = true;
                startedAt = DateTimeOffset.UtcNow;

                logger.LogInformation("[VSF-BRIDGE] Soulfind bridge started on port {Port}", bridgePort);
            }
            catch (Exception ex)
            {
                await StopProcessAsync().ConfigureAwait(false);
                logger.LogError(ex, "[VSF-BRIDGE] Failed to start bridge: {Message}", ex.Message);
                throw;
            }
        }
        finally
        {
            lifecycleSemaphore.Release();
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await lifecycleSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (soulfindProcess is null)
            {
                logger.LogDebug("[VSF-BRIDGE] Bridge not running");
                return;
            }

            logger.LogInformation("[VSF-BRIDGE] Stopping Soulfind bridge");
            await StopProcessAsync().ConfigureAwait(false);
            connectedClients.Clear();
            logger.LogInformation("[VSF-BRIDGE] Soulfind bridge stopped");
        }
        finally
        {
            lifecycleSemaphore.Release();
        }
    }

    private async Task StopProcessAsync()
    {
        var process = soulfindProcess;
        soulfindProcess = null;
        isRunning = false;
        startedAt = null;

        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)
                .ConfigureAwait(false);

            await Task.WhenAll(
                    stdoutDrainTask ?? Task.CompletedTask,
                    stderrDrainTask ?? Task.CompletedTask)
                .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[VSF-BRIDGE] Error stopping bridge process: {Message}", ex.Message);
        }
        finally
        {
            process.Dispose();
            stdoutDrainTask = null;
            stderrDrainTask = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        lifecycleSemaphore.Dispose();
    }

    public Task<BridgeHealthStatus> GetHealthAsync(CancellationToken ct)
    {
        var health = new BridgeHealthStatus
        {
            IsHealthy = isRunning && (soulfindProcess?.HasExited == false),
            Version = "1.0.0-proxy",
            ActiveConnections = connectedClients.Count,
            StartedAt = startedAt ?? DateTimeOffset.MinValue
        };

        return Task.FromResult(health);
    }

    public void RecordClientConnection(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        connectedClients.TryAdd(clientId, 0);
    }

    public void RecordClientDisconnection(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        connectedClients.TryRemove(clientId, out _);
    }
}

/// <summary>
/// Bridge configuration options.
/// </summary>
public class BridgeOptions
{
    /// <summary>
    /// Enable legacy client bridge.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Path to Soulfind binary.
    /// </summary>
    public string? SoulfindPath { get; set; }

    /// <summary>
    /// Bridge listening port (Soulseek protocol).
    /// </summary>
    public int Port { get; set; } = 2242;

    /// <summary>
    /// IP address on which the bridge listens. Loopback is the secure default.
    /// </summary>
    public string BindAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// Maximum concurrent legacy clients.
    /// </summary>
    public int MaxClients { get; set; } = 10;

    /// <summary>
    /// Require authentication for bridge connections.
    /// </summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// Bridge authentication password (required if RequireAuth is true).
    /// </summary>
    [Secret]
    public string? Password { get; set; }

    /// <summary>
    /// Maximum protocol requests accepted from one client per minute.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Maximum downloads initiated during one client session.
    /// </summary>
    public int MaxTransfersPerSession { get; set; } = 10;
}
