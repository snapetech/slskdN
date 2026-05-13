// <copyright file="UpstreamSlskdFullInstanceRunner.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Integration.Harness;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Full upstream slskd instance runner for opt-in live compatibility tests.
/// </summary>
public sealed class UpstreamSlskdFullInstanceRunner : IAsyncDisposable
{
    private static readonly TimeSpan ApiStartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ApiStartupProbeDelay = TimeSpan.FromMilliseconds(500);
    private const int CapturedLogLineLimit = 200;

    private readonly ILogger<UpstreamSlskdFullInstanceRunner> logger;
    private readonly string testId;
    private readonly string appDir;
    private readonly string stdoutLogPath;
    private readonly string stderrLogPath;
    private readonly ConcurrentQueue<string> stdoutLines = new();
    private readonly ConcurrentQueue<string> stderrLines = new();
    private Process? process;
    private int apiPort;
    private int soulseekListenPort;
    private string apiHost = "127.0.0.1";
    private string? vpnNamespaceName;
    private string? vpnNamespaceHostIp;

    public UpstreamSlskdFullInstanceRunner(ILogger<UpstreamSlskdFullInstanceRunner> logger, string testId)
    {
        this.logger = logger;
        this.testId = testId;
        appDir = Path.Combine(Path.GetTempPath(), "slskdn-test", testId);
        stdoutLogPath = Path.Combine(appDir, "upstream-slskd.stdout.log");
        stderrLogPath = Path.Combine(appDir, "upstream-slskd.stderr.log");

        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(Path.Combine(appDir, "config"));
        Directory.CreateDirectory(Path.Combine(appDir, "downloads"));
        Directory.CreateDirectory(Path.Combine(appDir, "incomplete"));
        Directory.CreateDirectory(Path.Combine(appDir, "shares"));
    }

    public string ApiUrl => $"http://{apiHost}:{apiPort}";
    public string SharesDirectory => Path.Combine(appDir, "shares");
    public int SoulseekListenPort => soulseekListenPort;

    public async Task StartAsync(
        string soulseekUsername,
        string soulseekPassword,
        CancellationToken ct = default)
    {
        apiPort = AllocateEphemeralPort();
        soulseekListenPort = AllocateEphemeralPort();

        var configPath = Path.Combine(appDir, "config", "slskd.yml");
        await File.WriteAllTextAsync(configPath, BuildConfigYaml(soulseekUsername, soulseekPassword), ct);

        var binaryPath = await DiscoverOrBuildUpstreamSlskdBinaryAsync(ct);
        var startInfo = BuildStartInfo(binaryPath, configPath);
        startInfo.Environment["APP_DIR"] = appDir;

        logger.LogInformation("[TEST-UPSTREAM-SLSKD] Starting upstream slskd {TestId}", testId);
        process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start upstream slskd process: {binaryPath}");

        process.OutputDataReceived += (_, args) => CaptureProcessLogLine(stdoutLines, stdoutLogPath, args.Data);
        process.ErrorDataReceived += (_, args) => CaptureProcessLogLine(stderrLines, stderrLogPath, args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await WaitForApiReadyAsync(ct);
        await ConfigureVpnWrapperTestRouteAsync(ct);
        logger.LogInformation("[TEST-UPSTREAM-SLSKD] Instance {TestId} ready on API port {ApiPort}", testId, apiPort);
    }

    public async Task StopAsync()
    {
        if (process != null && !process.HasExited)
        {
            logger.LogInformation("[TEST-UPSTREAM-SLSKD] Stopping upstream slskd {TestId}", testId);

            try
            {
                process.CancelOutputRead();
                process.CancelErrorRead();
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[TEST-UPSTREAM-SLSKD] Error stopping upstream slskd");
            }
            finally
            {
                await CleanupVpnNamespaceAsync(CancellationToken.None);
            }

            process.Dispose();
            process = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        try
        {
            if (Directory.Exists(appDir) && !ShouldKeepArtifacts())
            {
                Directory.Delete(appDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[TEST-UPSTREAM-SLSKD] Failed to clean up directory: {Dir}", appDir);
        }
    }

    private string BuildConfigYaml(string soulseekUsername, string soulseekPassword)
    {
        var sb = new StringBuilder();
        sb.AppendLine("web:");
        sb.AppendLine($"  port: {apiPort}");
        sb.AppendLine($"  address: {(VpnNamespaceLeaseAllocator.IsConfigured() ? "0.0.0.0" : "127.0.0.1")}");
        sb.AppendLine("  https:");
        sb.AppendLine("    disabled: true");
        sb.AppendLine("    force: false");
        sb.AppendLine("  authentication:");
        sb.AppendLine("    disabled: true");
        sb.AppendLine("    username: admin");
        sb.AppendLine("    password: admin");
        sb.AppendLine("directories:");
        sb.AppendLine($"  downloads: {Path.Combine(appDir, "downloads")}");
        sb.AppendLine($"  incomplete: {Path.Combine(appDir, "incomplete")}");
        sb.AppendLine("shares:");
        sb.AppendLine("  directories:");
        sb.AppendLine($"    - {Path.Combine(appDir, "shares")}");
        sb.AppendLine("  cache:");
        sb.AppendLine("    storage_mode: disk");
        sb.AppendLine("soulseek:");
        sb.AppendLine($"  address: {GetSoulseekAddressForConfig()}");
        sb.AppendLine("  port: 2271");
        sb.AppendLine($"  username: {YamlEscape(soulseekUsername)}");
        sb.AppendLine($"  password: {YamlEscape(soulseekPassword)}");
        sb.AppendLine("  listen_ip_address: 0.0.0.0");
        sb.AppendLine($"  listen_port: {soulseekListenPort}");
        sb.AppendLine("flags:");
        sb.AppendLine("  no_connect: false");
        return sb.ToString();
    }

    private ProcessStartInfo BuildStartInfo(string binaryPath, string configPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = appDir,
        };

        var vpnLease = VpnNamespaceLeaseAllocator.Allocate();
        if (vpnLease != null)
        {
            apiHost = vpnLease.NamespaceIp;
            vpnNamespaceName = vpnLease.NamespaceName;
            vpnNamespaceHostIp = vpnLease.NamespaceHostIp;

            startInfo.FileName = vpnLease.Wrapper;
            startInfo.ArgumentList.Add(vpnLease.NamespaceName);
            startInfo.ArgumentList.Add(vpnLease.Config);
            startInfo.ArgumentList.Add(GetVpnEntrypointPath());
            startInfo.ArgumentList.Add(binaryPath);
            startInfo.ArgumentList.Add("--config");
            startInfo.ArgumentList.Add(configPath);
            startInfo.ArgumentList.Add("--app-dir");
            startInfo.ArgumentList.Add(appDir);

            startInfo.Environment["SLSKR_NETNS_HOST_IP"] = vpnLease.NamespaceHostIp;
            startInfo.Environment["SLSKR_NETNS_IP"] = vpnLease.NamespaceIp;
            startInfo.Environment["SLSKR_NETNS_SUBNET"] = vpnLease.NamespaceSubnet;
            startInfo.Environment["SLSKDN_VPN_TEST_FORWARD_PORT"] = soulseekListenPort.ToString();
            startInfo.Environment["SLSKDN_VPN_TEST_FORWARD_NAMESPACE"] = vpnLease.NamespaceName;
            startInfo.Environment["SLSKDN_VPN_TEST_FORWARD_STATE_FILE"] = Path.Combine(appDir, "vpn-port-forward.env");

            logger.LogInformation(
                "[TEST-UPSTREAM-SLSKD] Routing instance {TestId} through VPN namespace {Namespace} using config {Config}",
                testId,
                vpnLease.NamespaceName,
                Path.GetFileName(vpnLease.Config));

            return startInfo;
        }

        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(configPath);
        startInfo.ArgumentList.Add("--app-dir");
        startInfo.ArgumentList.Add(appDir);
        return startInfo;
    }

    private static string GetSoulseekAddressForConfig()
    {
        var configuredAddress = Environment.GetEnvironmentVariable("SLSKDN_TEST_SOULSEEK_ADDRESS") ?? "vps.slsknet.org";
        if (!VpnNamespaceLeaseAllocator.IsConfigured() || IPAddress.TryParse(configuredAddress, out _))
        {
            return configuredAddress;
        }

        var address = Dns.GetHostAddresses(configuredAddress)
            .FirstOrDefault(candidate => candidate.AddressFamily == AddressFamily.InterNetwork);

        return address?.ToString() ?? configuredAddress;
    }

    private static string GetVpnEntrypointPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Harness", "run-with-vpn-port-forward.sh");
    }

    private static async Task<string> DiscoverOrBuildUpstreamSlskdBinaryAsync(CancellationToken ct)
    {
        var envPath = Environment.GetEnvironmentVariable("SLSKDN_UPSTREAM_SLSKD_BINARY_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        var sourceDirectory = Environment.GetEnvironmentVariable("SLSKDN_UPSTREAM_SLSKD_SOURCE_DIR")
            ?? Path.Combine(Path.GetTempPath(), "slskdn-upstream-compat", "slskd");
        var releaseCandidate = Path.Combine(sourceDirectory, "src", "slskd", "bin", "Release", "net10.0", "slskd");
        var debugCandidate = Path.Combine(sourceDirectory, "src", "slskd", "bin", "Debug", "net10.0", "slskd");
        if (File.Exists(releaseCandidate))
        {
            return releaseCandidate;
        }

        if (File.Exists(debugCandidate))
        {
            return debugCandidate;
        }

        if (!ShouldBuildUpstreamSlskd())
        {
            throw new InvalidOperationException(
                "Upstream slskd binary not found. Set SLSKDN_UPSTREAM_SLSKD_BINARY_PATH or set SLSKDN_BUILD_UPSTREAM_SLSKD=1 to clone/build upstream for the opt-in compatibility test.");
        }

        await CloneOrUpdateUpstreamAsync(sourceDirectory, ct);
        await RunProcessAsync("dotnet", ["build", "src/slskd/slskd.csproj", "-c", "Release"], sourceDirectory, TimeSpan.FromMinutes(8), ct);

        if (File.Exists(releaseCandidate))
        {
            return releaseCandidate;
        }

        throw new InvalidOperationException($"Upstream slskd build completed but binary was not found at {releaseCandidate}");
    }

    private static bool ShouldBuildUpstreamSlskd()
    {
        var value = Environment.GetEnvironmentVariable("SLSKDN_BUILD_UPSTREAM_SLSKD");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task CloneOrUpdateUpstreamAsync(string sourceDirectory, CancellationToken ct)
    {
        var parent = Directory.GetParent(sourceDirectory)?.FullName
            ?? throw new InvalidOperationException($"Could not resolve parent directory for {sourceDirectory}");
        Directory.CreateDirectory(parent);

        if (!Directory.Exists(Path.Combine(sourceDirectory, ".git")))
        {
            await RunProcessAsync(
                "git",
                ["clone", "--depth", "1", "https://github.com/slskd/slskd.git", sourceDirectory],
                parent,
                TimeSpan.FromMinutes(4),
                ct);
            return;
        }

        await RunProcessAsync("git", ["fetch", "--depth", "1", "origin", "master"], sourceDirectory, TimeSpan.FromMinutes(2), ct);
        await RunProcessAsync("git", ["checkout", "FETCH_HEAD"], sourceDirectory, TimeSpan.FromMinutes(1), ct);
    }

    private static async Task RunProcessAsync(string fileName, string[] arguments, string workingDirectory, TimeSpan timeout, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
        await process.WaitForExitAsync(timeoutCts.Token);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} {string.Join(' ', arguments)} failed with exit code {process.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }

    private async Task WaitForApiReadyAsync(CancellationToken ct)
    {
        var maxAttempts = (int)Math.Ceiling(ApiStartupTimeout.TotalMilliseconds / ApiStartupProbeDelay.TotalMilliseconds);
        for (var attempt = 0; attempt < maxAttempts && !ct.IsCancellationRequested; attempt++)
        {
            if (process?.HasExited == true)
            {
                throw BuildProcessExitException();
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync($"{ApiUrl}/api/v0/session/enabled", ct);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(ApiStartupProbeDelay, ct);
        }

        throw new TimeoutException(
            $"upstream slskd instance did not become ready after {ApiStartupTimeout.TotalSeconds:n0}s" +
            $"{Environment.NewLine}STDOUT:{Environment.NewLine}{FormatCapturedLogs(stdoutLines)}" +
            $"{Environment.NewLine}STDERR:{Environment.NewLine}{FormatCapturedLogs(stderrLines)}");
    }

    private InvalidOperationException BuildProcessExitException()
    {
        return new InvalidOperationException(
            $"upstream slskd process exited before API became ready" +
            $"{Environment.NewLine}STDOUT:{Environment.NewLine}{FormatCapturedLogs(stdoutLines)}" +
            $"{Environment.NewLine}STDERR:{Environment.NewLine}{FormatCapturedLogs(stderrLines)}");
    }

    private async Task ConfigureVpnWrapperTestRouteAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vpnNamespaceName) || string.IsNullOrWhiteSpace(vpnNamespaceHostIp))
        {
            return;
        }

        var testCidr = Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_TEST_CIDR") ?? "10.224.0.0/11";
        var nsVeth = $"v-{vpnNamespaceName}n";
        await RunProcessAsync(
            "sudo",
            ["ip", "netns", "exec", vpnNamespaceName, "ip", "route", "replace", testCidr, "via", vpnNamespaceHostIp, "dev", nsVeth],
            Directory.GetCurrentDirectory(),
            TimeSpan.FromSeconds(15),
            ct);
    }

    private async Task CleanupVpnNamespaceAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vpnNamespaceName))
        {
            return;
        }

        var namespacePids = await RunBestEffortWithOutputAsync("sudo", ["ip", "netns", "pids", vpnNamespaceName], ct);
        foreach (var pid in namespacePids.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await RunBestEffortAsync("sudo", ["kill", pid], ct);
        }

        await RunBestEffortAsync("sudo", ["ip", "netns", "delete", vpnNamespaceName], ct);
    }

    private static async Task RunBestEffortAsync(string fileName, string[] arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync(ct);
        }
    }

    private static async Task<string> RunBestEffortWithOutputAsync(string fileName, string[] arguments, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return string.Empty;
        }

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return output;
    }

    private static void CaptureProcessLogLine(ConcurrentQueue<string> queue, string path, string? line)
    {
        if (line == null)
        {
            return;
        }

        queue.Enqueue(line);
        while (queue.Count > CapturedLogLineLimit && queue.TryDequeue(out _))
        {
        }

        try
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
        }
    }

    private static string FormatCapturedLogs(ConcurrentQueue<string> lines)
    {
        return string.Join(Environment.NewLine, lines.ToArray());
    }

    private static int AllocateEphemeralPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string YamlEscape(string value)
    {
        return value.Contains(':') || value.Contains('#') || value.Contains('"') || value.Contains('\'') || value.Contains(' ')
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }

    private static bool ShouldKeepArtifacts()
    {
        var value = Environment.GetEnvironmentVariable("SLSKDN_TEST_KEEP_ARTIFACTS");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
