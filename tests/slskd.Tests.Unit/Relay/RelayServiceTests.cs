// <copyright file="RelayServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Cryptography;
using slskd.Files;
using slskd.Relay;
using slskd.Shares;
using Xunit;

namespace slskd.Tests.Unit.Relay;

public class RelayServiceTests
{
    [Fact]
    public void Configure_WhenOnlyTopLevelRelayOptionsChange_ReconfiguresInsteadOfReturningEarly()
    {
        var initialOptions = CreateOptions(enabled: false, RelayMode.Controller);
        var optionsMonitor = CreateOptionsMonitor(initialOptions);
        var previousClient = new TestRelayClient();

        var service = CreateService(optionsMonitor, previousClient);

        InvokeConfigure(service, CreateOptions(enabled: true, RelayMode.Controller));

        Assert.Equal(RelayMode.Controller, service.StateMonitor.CurrentValue.Mode);
        Assert.IsType<NullRelayClient>(service.Client);
        Assert.True(previousClient.Disposed);
    }

    [Fact]
    public void Configure_WhenReplacingAgentClient_DisposesPreviousClient()
    {
        var initialOptions = CreateOptions(enabled: false, RelayMode.Agent);
        var optionsMonitor = CreateOptionsMonitor(initialOptions);
        var previousClient = new TestRelayClient();

        var service = CreateService(optionsMonitor, previousClient);

        InvokeConfigure(service, CreateOptions(enabled: true, RelayMode.Agent));

        Assert.IsType<RelayClient>(service.Client);
        Assert.True(previousClient.Disposed);
    }

    [Fact]
    public void ReplaceClient_DisposesPreviousStateMonitorSubscription()
    {
        var optionsMonitor = CreateOptionsMonitor(CreateOptions(enabled: false, RelayMode.Controller));
        var service = CreateService(optionsMonitor, new NullRelayClient());
        var previousClient = new TestRelayClient();
        var nextClient = new TestRelayClient();

        InvokeReplaceClient(service, previousClient);
        InvokeAttachClientStateMonitor(service, previousClient);
        previousClient.SetState(RelayClientState.Connected);

        Assert.Equal(RelayClientState.Connected, service.StateMonitor.CurrentValue.Controller.State);

        InvokeReplaceClient(service, nextClient);
        InvokeAttachClientStateMonitor(service, nextClient);

        previousClient.SetState(RelayClientState.Disconnected);

        Assert.Equal(RelayClientState.Connected, service.StateMonitor.CurrentValue.Controller.State);

        nextClient.SetState(RelayClientState.Reconnecting);

        Assert.Equal(RelayClientState.Reconnecting, service.StateMonitor.CurrentValue.Controller.State);
    }

    [Fact]
    public void Dispose_UnsubscribesOptionsMonitor_AndDisposesCurrentClient()
    {
        var optionsMonitor = new TestOptionsMonitor<Options>(CreateOptions(enabled: false, RelayMode.Controller));
        var client = new TestRelayClient();
        var service = CreateService(optionsMonitor, client);

        Assert.Equal(1, optionsMonitor.ListenerCount);

        service.Dispose();

        Assert.Equal(0, optionsMonitor.ListenerCount);
        Assert.True(client.Disposed);
    }

    [Fact]
    public async Task TryValidateFileDownloadCredential_ReturnsTrustedFilenameFromTokenCache()
    {
        const string agentName = "test-agent";
        const string connectionId = "connection-1";
        const string secret = "0123456789abcdef";

        var notifiedToken = Guid.Empty;
        var relayClient = new Mock<IRelayHub>();
        relayClient
            .Setup(x => x.NotifyFileDownloadCompleted("trusted.mp3", It.IsAny<Guid>()))
            .Callback<string, Guid>((_, token) => notifiedToken = token)
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients<IRelayHub>>();
        hubClients
            .Setup(x => x.Client(connectionId))
            .Returns(relayClient.Object);

        var relayHub = new Mock<IHubContext<RelayHub, IRelayHub>>();
        relayHub
            .Setup(x => x.Clients)
            .Returns(hubClients.Object);

        var options = CreateOptions(enabled: true, RelayMode.Controller, agentName, secret);
        var service = CreateService(CreateOptionsMonitor(options), new NullRelayClient(), relayHub.Object);
        service.RegisterAgent(connectionId, new Agent { Name = agentName, IPAddress = "127.0.0.1" });

        await service.NotifyFileDownloadCompleteAsync("trusted.mp3");

        var credential = ComputeCredential(notifiedToken, agentName, secret);
        var validated = service.TryValidateFileDownloadCredential(notifiedToken, credential, out var validatedAgentName, out var validatedFilename);

        Assert.True(validated);
        Assert.Equal(agentName, validatedAgentName);
        Assert.Equal("trusted.mp3", validatedFilename);
        Assert.False(service.TryValidateFileDownloadCredential(notifiedToken, agentName, "evil.mp3", credential));
    }

    private static RelayService CreateService(IOptionsMonitor<Options> optionsMonitor, IRelayClient relayClient, IHubContext<RelayHub, IRelayHub>? relayHub = null)
    {
        return new RelayService(
            Mock.Of<IWaiter>(),
            new FileService(optionsMonitor),
            Mock.Of<IShareService>(),
            Mock.Of<IShareRepositoryFactory>(),
            optionsMonitor,
            relayHub ?? Mock.Of<IHubContext<RelayHub, IRelayHub>>(),
            Mock.Of<IHttpClientFactory>(),
            relayClient);
    }

    private static IOptionsMonitor<Options> CreateOptionsMonitor(Options options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<Options>>();
        optionsMonitor.SetupGet(x => x.CurrentValue).Returns(options);
        optionsMonitor.Setup(x => x.OnChange(It.IsAny<Action<Options, string?>>())).Returns(Mock.Of<IDisposable>());
        return optionsMonitor.Object;
    }

    private static Options CreateOptions(bool enabled, RelayMode mode, string? agentName = null, string? agentSecret = null)
    {
        return new Options
        {
            Relay = new Options.RelayOptions
            {
                Enabled = enabled,
                Mode = mode.ToString(),
                Agents = agentName == null
                    ? new Dictionary<string, Options.RelayOptions.RelayAgentConfigurationOptions>()
                    : new Dictionary<string, Options.RelayOptions.RelayAgentConfigurationOptions>
                    {
                        [agentName] = new()
                        {
                            InstanceName = agentName,
                            Secret = agentSecret ?? "0123456789abcdef",
                        },
                    },
                Controller = new Options.RelayOptions.RelayControllerConfigurationOptions
                {
                    Address = "https://relay.example",
                    ApiKey = "api-key",
                    Secret = "shared-secret",
                },
            },
        };
    }

    private static string ComputeCredential(Guid token, string agentName, string secret)
    {
        var key = Pbkdf2.GetKey(password: secret, salt: agentName, length: 48);
        var tokenBytes = Encoding.UTF8.GetBytes(token.ToString());
        return Convert.ToBase64String(HMACSHA256.HashData(key, tokenBytes));
    }

    private static void InvokeConfigure(RelayService service, Options options)
    {
        var method = typeof(RelayService).GetMethod("Configure", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(service, [options]);
    }

    private static void InvokeAttachClientStateMonitor(RelayService service, IRelayClient client)
    {
        var method = typeof(RelayService).GetMethod("AttachClientStateMonitor", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(service, [client]);
    }

    private static void InvokeReplaceClient(RelayService service, IRelayClient client)
    {
        var method = typeof(RelayService).GetMethod("ReplaceClient", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(service, [client]);
    }

    private sealed class TestRelayClient : IRelayClient, IDisposable
    {
        private ManagedState<RelayClientState> State { get; } = new();

        public IStateMonitor<RelayClientState> StateMonitor => State;

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public void SetState(RelayClientState state)
        {
            State.SetValue(_ => state);
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SynchronizeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
