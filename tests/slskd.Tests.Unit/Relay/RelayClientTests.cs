// <copyright file="RelayClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Net.Http;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Common.Security;
using slskd.Files;
using slskd.Relay;
using slskd.Shares;
using Xunit;

namespace slskd.Tests.Unit.Relay;

public class RelayClientTests
{
    [Theory]
    [InlineData("https://relay.example", "https://relay.example/")]
    [InlineData("https://relay.example/", "https://relay.example/")]
    [InlineData("https://relay.example/base///", "https://relay.example/base/")]
    public void NormalizeControllerAddress_PreservesBasePathAndAddsOneTrailingSlash(string address, string expected)
    {
        var method = typeof(RelayClient).GetMethod("NormalizeControllerAddress", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var normalized = method!.Invoke(null, new object[] { address });

        Assert.Equal(expected, normalized);
        Assert.Equal(new System.Uri($"{expected}hub/relay"), new System.Uri(new System.Uri(expected), "hub/relay"));
    }

    [Fact]
    public async Task StopAsync_CancelsStartRetryToken()
    {
        var client = CreateClient(new Options());

        using var cts = new CancellationTokenSource();
        SetStartCancellationTokenSource(client, cts);

        await client.StopAsync();

        Assert.True(cts.IsCancellationRequested);
        Assert.Null(GetStartCancellationTokenSource(client));
    }

    [Fact]
    public async Task StartAsync_CancelsPreviousStartRetryTokenBeforeReplacingIt()
    {
        var options = new Options
        {
            Relay = new Options.RelayOptions
            {
                Mode = RelayMode.Agent.ToString().ToLowerInvariant(),
                Controller = new Options.RelayOptions.RelayControllerConfigurationOptions
                {
                    Address = "http://127.0.0.1:1",
                    ApiKey = "1234567890abcdef",
                    Secret = "1234567890abcdef",
                },
            },
        };

        var client = CreateClient(options);
        using var previousCts = new CancellationTokenSource();
        SetStartCancellationTokenSource(client, previousCts);

        using var startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var cancelReplacementTask = Task.Run(async () =>
        {
            while (!startTimeout.IsCancellationRequested)
            {
                var replacement = GetStartCancellationTokenSource(client);
                if (replacement != null && !ReferenceEquals(replacement, previousCts))
                {
                    replacement.Cancel();
                    return;
                }

                await Task.Delay(10, startTimeout.Token);
            }
        }, CancellationToken.None);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.StartAsync());
        await cancelReplacementTask;

        Assert.True(previousCts.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_UnsubscribesOptionsMonitor()
    {
        var optionsMonitor = new TestOptionsMonitor<Options>(new Options());
        var client = CreateClient(optionsMonitor);

        Assert.Equal(1, optionsMonitor.ListenerCount);

        client.Dispose();

        Assert.Equal(0, optionsMonitor.ListenerCount);
    }

    [Fact]
    public async Task HandleNotifyFileDownloadCompleted_QuarantinesExecutableContent()
    {
        var downloadsRoot = Path.Combine(Path.GetTempPath(), $"slskdn-relay-safety-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(downloadsRoot);
        var filename = "relay-disguised.mp3";
        var destinationPath = Path.Combine(downloadsRoot, filename);
        var maliciousContent = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        var handler = new RelayDownloadHandler(maliciousContent);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(OutboundUriGuard.NoRedirectHttpClientName))
            .Returns(() => new HttpClient(handler));

        var optionsMonitor = new TestOptionsMonitor<Options>(new Options
        {
            Directories = new Options.DirectoriesOptions { Downloads = downloadsRoot },
            Relay = new Options.RelayOptions
            {
                Mode = RelayMode.Agent.ToString().ToLowerInvariant(),
                Controller = new Options.RelayOptions.RelayControllerConfigurationOptions
                {
                    Address = "http://relay.example",
                    ApiKey = "1234567890abcdef",
                    Secret = "1234567890abcdef",
                    Downloads = true
                }
            }
        });
        var client = new RelayClient(Mock.Of<IShareService>(), new FileService(optionsMonitor), optionsMonitor, httpClientFactory.Object);

        try
        {
            var method = typeof(RelayClient).GetMethod("HandleNotifyFileDownloadCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            await (Task)method!.Invoke(client, new object[] { filename, System.Guid.NewGuid() })!;

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!File.Exists(Path.Combine(downloadsRoot, ".quarantine", filename)))
            {
                await Task.Delay(20, timeout.Token);
            }

            Assert.False(File.Exists(destinationPath));
            Assert.Equal(maliciousContent, await File.ReadAllBytesAsync(Path.Combine(downloadsRoot, ".quarantine", filename)));
        }
        finally
        {
            client.Dispose();
            Directory.Delete(downloadsRoot, recursive: true);
        }
    }

    private sealed class RelayDownloadHandler : HttpMessageHandler
    {
        private readonly byte[] content;

        public RelayDownloadHandler(byte[] content)
        {
            this.content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }
    }

    private static RelayClient CreateClient(Options options)
    {
        return CreateClient(new TestOptionsMonitor<Options>(options));
    }

    private static RelayClient CreateClient(TestOptionsMonitor<Options> optionsMonitor)
    {
        return new RelayClient(
            Mock.Of<IShareService>(),
            new FileService(optionsMonitor),
            optionsMonitor,
            Mock.Of<IHttpClientFactory>());
    }

    private static CancellationTokenSource? GetStartCancellationTokenSource(RelayClient client)
    {
        var property = typeof(RelayClient).GetProperty("StartCancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return (CancellationTokenSource?)property!.GetValue(client);
    }

    private static void SetStartCancellationTokenSource(RelayClient client, CancellationTokenSource cts)
    {
        var property = typeof(RelayClient).GetProperty("StartCancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(client, cts);
    }
}
