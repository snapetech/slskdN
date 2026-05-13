// <copyright file="SlskdnSoulseekRuntimeInteropTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Integration.SoulseekInterop;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Tests.Integration.Harness;
using Soulseek;
using Xunit;

[Trait("Category", "L2-Integration")]
[Trait("Category", "FullInstance")]
[Trait("Category", "LiveSoulseek")]
public class SlskdnSoulseekRuntimeInteropTests
{
    private const int LiveOperationTimeoutMilliseconds = 45000;
    private const int LiveConnectRetryCount = 3;

    [Fact]
    public async Task OptionalLiveAccounts_RawSoulseekRuntimeCanBrowseAndDownloadFromSlskdn()
    {
        if (!ShouldRunLiveSoulseekInteropTests())
        {
            return;
        }

        if (!TryLoadLocalMeshAccounts(out var accounts))
        {
            return;
        }

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        await using var slskdn = new SlskdnFullInstanceRunner(
            loggerFactory.CreateLogger<SlskdnFullInstanceRunner>(),
            $"soulseek-interop-{Guid.NewGuid():N}"[..27]);

        var probeId = Guid.NewGuid().ToString("N")[..12];
        var probeFilename = $"runtime-interop-probe-{probeId}.flac";
        var probeBytes = Enumerable.Range(0, 4096).Select(i => (byte)((i * 29) % 251)).ToArray();
        var runtimeListenPort = GetAvailablePort();
        await System.IO.File.WriteAllBytesAsync(Path.Combine(slskdn.SharesDirectory, probeFilename), probeBytes);

        await slskdn.StartAsync(
            disableAuthentication: true,
            noConnect: false,
            soulseekUsername: accounts.AlphaUsername,
            soulseekPassword: accounts.AlphaPassword,
            soulseekEndpointOverrides: new Dictionary<string, int>
            {
                [accounts.BetaUsername] = runtimeListenPort,
            });

        Assert.True(slskdn.SoulseekListenPort.HasValue);

        using var slskdnClient = new HttpClient { BaseAddress = new Uri(slskdn.ApiUrl), Timeout = TimeSpan.FromSeconds(45) };
        slskdnClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "integration-test");

        await WaitForSoulseekLoggedInAsync(slskdnClient, "slskdN");

        var scanResponse = await slskdnClient.PutAsync("/api/v0/shares", content: null);
        Assert.True(
            scanResponse.IsSuccessStatusCode,
            $"Share scan request failed: {(int)scanResponse.StatusCode} {await scanResponse.Content.ReadAsStringAsync()}");

        using var runtimeClient = CreateRuntimeClient(accounts.AlphaUsername, slskdn.SoulseekListenPort.Value, runtimeListenPort);
        await ConnectWithRetryAsync(runtimeClient, accounts.BetaUsername, accounts.BetaPassword);

        using var cancellationTokenSource = new CancellationTokenSource(LiveOperationTimeoutMilliseconds);
        var browsedFilename = await WaitForBrowsableFileAsync(
            runtimeClient,
            accounts.AlphaUsername,
            probeFilename,
            probeBytes.Length,
            cancellationTokenSource.Token);

        using var output = new MemoryStream();
        var transfer = await runtimeClient.DownloadAsync(
            accounts.AlphaUsername,
            browsedFilename,
            () => Task.FromResult((Stream)output),
            size: probeBytes.Length,
            options: new TransferOptions(maximumLingerTime: 10000),
            cancellationToken: cancellationTokenSource.Token);

        Assert.True(transfer.State.HasFlag(TransferStates.Succeeded));
        Assert.Equal(probeBytes.Length, transfer.BytesTransferred);
        Assert.Equal(probeBytes, output.ToArray());
    }

    [Fact]
    public async Task OptionalLiveAccounts_SlskdnCanDownloadFromUpstreamSlskd()
    {
        if (!ShouldRunUpstreamSlskdCompatTests())
        {
            return;
        }

        if (!TryLoadLocalMeshAccounts(out var accounts))
        {
            return;
        }

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        await using var slskdn = new SlskdnFullInstanceRunner(
            loggerFactory.CreateLogger<SlskdnFullInstanceRunner>(),
            $"upstream-compat-n-{Guid.NewGuid():N}"[..27]);
        await using var upstream = new UpstreamSlskdFullInstanceRunner(
            loggerFactory.CreateLogger<UpstreamSlskdFullInstanceRunner>(),
            $"upstream-compat-u-{Guid.NewGuid():N}"[..27]);

        var probeId = Guid.NewGuid().ToString("N")[..12];
        var probeFilename = $"upstream-compat-probe-{probeId}.flac";
        var probeBytes = Enumerable.Range(0, 4096).Select(i => (byte)((i * 31) % 251)).ToArray();
        await System.IO.File.WriteAllBytesAsync(Path.Combine(upstream.SharesDirectory, probeFilename), probeBytes);

        await upstream.StartAsync(accounts.BetaUsername, accounts.BetaPassword);
        await slskdn.StartAsync(
            disableAuthentication: true,
            noConnect: false,
            soulseekUsername: accounts.AlphaUsername,
            soulseekPassword: accounts.AlphaPassword,
            soulseekEndpointOverrides: VpnNamespaceLeaseAllocator.IsConfigured()
                ? null
                : new Dictionary<string, int>
                {
                    [accounts.BetaUsername] = upstream.SoulseekListenPort,
                });

        using var upstreamClient = new HttpClient { BaseAddress = new Uri(upstream.ApiUrl), Timeout = TimeSpan.FromSeconds(45) };
        using var slskdnClient = new HttpClient { BaseAddress = new Uri(slskdn.ApiUrl), Timeout = TimeSpan.FromSeconds(45) };
        upstreamClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "integration-test");
        slskdnClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", "integration-test");

        await WaitForSoulseekLoggedInAsync(upstreamClient, "upstream slskd");
        await WaitForSoulseekLoggedInAsync(slskdnClient, "slskdN");

        var scanResponse = await upstreamClient.PutAsync("/api/v0/shares", content: null);
        Assert.True(
            scanResponse.IsSuccessStatusCode,
            $"Upstream share scan request failed: {(int)scanResponse.StatusCode} {await scanResponse.Content.ReadAsStringAsync()}");

        var remoteFilename = await WaitForSharedFilePathFromApiAsync(
            upstreamClient,
            probeFilename,
            probeBytes.Length);

        var enqueueResponse = await slskdnClient.PostAsJsonAsync(
            $"/api/v0/transfers/downloads/{Uri.EscapeDataString(accounts.BetaUsername)}",
            new[]
            {
                new
                {
                    filename = remoteFilename,
                    size = probeBytes.Length,
                },
            });
        Assert.True(
            enqueueResponse.IsSuccessStatusCode,
            $"slskdN enqueue request failed: {(int)enqueueResponse.StatusCode} {await enqueueResponse.Content.ReadAsStringAsync()}");

        await WaitForCompletedDownloadBytesAsync(
            slskdnClient,
            slskdn.DownloadsDirectory,
            probeFilename,
            probeBytes,
            TimeSpan.FromSeconds(90));
    }

    private static SoulseekClient CreateRuntimeClient(string slskdnUsername, int slskdnListenPort, int listenPort)
    {
        var endpointCache = new StaticUserEndPointCache();
        endpointCache.AddOrUpdate(slskdnUsername, new IPEndPoint(IPAddress.Loopback, slskdnListenPort));

        var connectionOptions = new ConnectionOptions(connectTimeout: LiveOperationTimeoutMilliseconds);
        return new SoulseekClient(
            minorVersion: 9999,
            options: new SoulseekClientOptions(
                listenIPAddress: IPAddress.Loopback,
                listenPort: listenPort,
                messageTimeout: LiveOperationTimeoutMilliseconds,
                serverConnectionOptions: connectionOptions,
                peerConnectionOptions: connectionOptions,
                transferConnectionOptions: connectionOptions,
                incomingConnectionOptions: connectionOptions,
                distributedConnectionOptions: connectionOptions,
                userEndPointCache: endpointCache));
    }

    private static async Task<string> WaitForBrowsableFileAsync(
        SoulseekClient runtimeClient,
        string username,
        string expectedFilename,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var browseResponse = await runtimeClient.BrowseAsync(username, cancellationToken: cancellationToken);
                foreach (var directory in browseResponse.Directories)
                {
                    var file = directory.Files.FirstOrDefault(candidate =>
                        candidate.Size == expectedSize &&
                        candidate.Filename.EndsWith(expectedFilename, StringComparison.OrdinalIgnoreCase));

                    if (file != null)
                    {
                        return string.IsNullOrWhiteSpace(directory.Name)
                            ? file.Filename
                            : $"{directory.Name}\\{file.Filename}";
                    }
                }
            }
            catch (Exception ex) when (IsTransientConnectFailure(ex))
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException(
            $"Raw Soulseek.NET client could not browse slskdN-hosted probe {expectedFilename}",
            lastException);
    }

    private static async Task WaitForSoulseekLoggedInAsync(HttpClient client, string nodeName)
    {
        string? failureDetails = null;
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync("/api/v0/application");
            var body = await response.Content.ReadAsStringAsync();
            failureDetails = body;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(body);
            if (document.RootElement
                .GetProperty("server")
                .GetProperty("isLoggedIn")
                .GetBoolean())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        Assert.Fail($"{nodeName} did not log in to Soulseek with the configured live test account\n" + failureDetails);
    }

    private static async Task ConnectWithRetryAsync(SoulseekClient client, string username, string password)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= LiveConnectRetryCount; attempt++)
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(LiveOperationTimeoutMilliseconds);
                await client.ConnectAsync(username, password, cancellationTokenSource.Token);
                return;
            }
            catch (Exception ex) when (IsTransientConnectFailure(ex) && attempt < LiveConnectRetryCount)
            {
                lastException = ex;
                client.Disconnect();
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
            }
        }

        throw lastException ?? new TimeoutException("Unable to connect to Soulseek after retries");
    }

    private static bool IsTransientConnectFailure(Exception ex)
        => ex is TimeoutException
            || ex is OperationCanceledException
            || (ex is SoulseekClientException clientException
                && (clientException.InnerException is ConnectionException || clientException.InnerException is IOException));

    private static bool ShouldRunLiveSoulseekInteropTests()
    {
        var value = Environment.GetEnvironmentVariable("SLSKDN_RUN_LIVE_SOULSEEK_INTEROP_TESTS");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRunUpstreamSlskdCompatTests()
    {
        var value = Environment.GetEnvironmentVariable("SLSKDN_RUN_UPSTREAM_SLSKD_COMPAT_TESTS");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> WaitForSharedFilePathFromApiAsync(HttpClient client, string expectedFilename, long expectedSize)
    {
        string? failureDetails = null;
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync("/api/v0/shares/contents");
            var body = await response.Content.ReadAsStringAsync();
            failureDetails = body;
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(body);
            foreach (var directory in document.RootElement.EnumerateArray())
            {
                var directoryName = directory.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : string.Empty;
                if (!directory.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var file in files.EnumerateArray())
                {
                    var filename = file.TryGetProperty("filename", out var filenameElement)
                        ? filenameElement.GetString()
                        : null;
                    var size = file.TryGetProperty("size", out var sizeElement)
                        ? sizeElement.GetInt64()
                        : -1;

                    if (size == expectedSize && filename?.EndsWith(expectedFilename, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return string.IsNullOrWhiteSpace(directoryName)
                            ? filename
                            : $"{directoryName}\\{filename}";
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Upstream slskd share API did not expose probe {expectedFilename}\n{failureDetails}");
    }

    private static async Task WaitForCompletedDownloadBytesAsync(
        HttpClient client,
        string downloadsDirectory,
        string expectedFilename,
        byte[] expectedBytes,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        string? failureDetails = null;
        Exception? lastException = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync("/api/v0/transfers/downloads");
                var body = await response.Content.ReadAsStringAsync();
                failureDetails = $"{(int)response.StatusCode} {body}";
                if (!response.IsSuccessStatusCode)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var user in document.RootElement.EnumerateArray())
                    {
                        if (!user.TryGetProperty("directories", out var directories) ||
                            directories.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var directory in directories.EnumerateArray())
                        {
                            if (!directory.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            foreach (var download in files.EnumerateArray())
                            {
                                var remotePath = download.TryGetProperty("filename", out var remotePathElement)
                                    ? remotePathElement.GetString()
                                    : null;
                                if (remotePath?.EndsWith(expectedFilename, StringComparison.OrdinalIgnoreCase) != true)
                                {
                                    continue;
                                }

                                var state = download.TryGetProperty("state", out var stateElement)
                                    ? stateElement.ToString()
                                    : string.Empty;
                                if (!state.Contains("Completed", StringComparison.OrdinalIgnoreCase) ||
                                    !state.Contains("Succeeded", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                var localPath = Path.Combine(downloadsDirectory, "shares", expectedFilename);
                                if (System.IO.File.Exists(localPath))
                                {
                                    var actual = await System.IO.File.ReadAllBytesAsync(localPath);
                                    if (actual.SequenceEqual(expectedBytes))
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException(
            $"slskdN did not complete upstream slskd download for {expectedFilename}\n{failureDetails}",
            lastException);
    }

    private static bool TryLoadLocalMeshAccounts(out LocalMeshAccounts accounts)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var envPath in GetLocalMeshAccountEnvPaths())
        {
            if (System.IO.File.Exists(envPath))
            {
                ReadEnvFile(values, envPath);
            }
        }

        var pool = LoadLocalMeshAccountPool(values);
        if (pool.Count < 2)
        {
            accounts = new LocalMeshAccounts(string.Empty, string.Empty, string.Empty, string.Empty);
            return false;
        }

        accounts = new LocalMeshAccounts(
            pool[0].Username,
            pool[0].Password,
            pool[1].Username,
            pool[1].Password);
        return true;
    }

    private static IEnumerable<string> GetLocalMeshAccountEnvPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "local-mesh-accounts.env");
        yield return Path.Combine(AppContext.BaseDirectory, "local-mesh-account-pool.env");

        var repositoryTestDirectory = Path.Combine(FindRepositoryRoot(), "tests", "slskd.Tests.Integration");
        yield return Path.Combine(repositoryTestDirectory, "local-mesh-accounts.env");
        yield return Path.Combine(repositoryTestDirectory, "local-mesh-account-pool.env");
    }

    private static void ReadEnvFile(Dictionary<string, string> values, string envPath)
    {
        foreach (var line in System.IO.File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex > 0)
            {
                values[trimmed[..separatorIndex].Trim()] = trimmed[(separatorIndex + 1)..].Trim();
            }
        }
    }

    private static List<LocalMeshAccount> LoadLocalMeshAccountPool(Dictionary<string, string> values)
    {
        var accounts = new List<LocalMeshAccount>();
        foreach (var suffix in new[] { "A", "B", "C", "D", "E", "F" })
        {
            AddAccountIfComplete(values, accounts, suffix);
        }

        for (var index = 1; index <= 16; index++)
        {
            AddAccountIfComplete(values, accounts, index.ToString());
        }

        return accounts
            .GroupBy(account => account.Username, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(16)
            .ToList();
    }

    private static void AddAccountIfComplete(Dictionary<string, string> values, List<LocalMeshAccount> accounts, string suffix)
    {
        var username = ReadCredential(values, $"SLSKDN_MESH_ACCOUNT_{suffix}_USERNAME");
        var password = ReadCredential(values, $"SLSKDN_MESH_ACCOUNT_{suffix}_PASSWORD");
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            accounts.Add(new LocalMeshAccount(username, password));
        }
    }

    private static string ReadCredential(Dictionary<string, string> values, string key)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        return values.TryGetValue(key, out var fileValue) ? fileValue : string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (System.IO.File.Exists(Path.Combine(directory.FullName, "slskd.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return System.IO.Directory.GetCurrentDirectory();
    }

    private static int GetAvailablePort()
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

    private sealed record LocalMeshAccounts(
        string AlphaUsername,
        string AlphaPassword,
        string BetaUsername,
        string BetaPassword);

    private sealed record LocalMeshAccount(string Username, string Password);

    private sealed class StaticUserEndPointCache : IUserEndPointCache
    {
        private readonly ConcurrentDictionary<string, IPEndPoint> endPoints = new();

        public void AddOrUpdate(string username, IPEndPoint endPoint)
        {
            endPoints.AddOrUpdate(username, endPoint, (_, _) => endPoint);
        }

        public bool TryGet(string username, out IPEndPoint endPoint)
        {
            return endPoints.TryGetValue(username, out endPoint);
        }
    }
}
