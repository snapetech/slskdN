// <copyright file="GluetunTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.VPN;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using slskd.Common.Security;
using slskd.Integrations.VPN;
using Xunit;

public class GluetunTests
{
    [Fact]
    public void Self_Hosted_Relay_Requires_Port_Forwarding()
    {
        var options = new Options.IntegrationOptions.VpnOptions
        {
            Enabled = true,
            SelfHostedRelay = true,
            Gluetun = new Options.IntegrationOptions.VpnOptions.GluetunVpnOptions
            {
                Url = "http://10.77.0.1:8010",
            },
        };

        var results = options.Validate(new ValidationContext(options)).ToArray();

        Assert.Contains(results, result => result.ErrorMessage == "Self-hosted relay mode requires VPN port forwarding");
    }

    [Fact]
    public async Task GetStatusAsync_Uses_Local_No_Redirect_Client_For_Configured_Control_Endpoint()
    {
        var handler = new StubHttpMessageHandler();
        var factory = new CapturingHttpClientFactory(handler);
        var options = new Options
        {
            Integration = new Options.IntegrationOptions
            {
                Vpn = new Options.IntegrationOptions.VpnOptions
                {
                    Gluetun = new Options.IntegrationOptions.VpnOptions.GluetunVpnOptions
                    {
                        Url = "http://127.0.0.1:8010",
                    },
                },
            },
        };

        var client = new Gluetun(factory, new TestOptionsMonitor<Options>(options));

        var status = await client.GetStatusAsync();

        Assert.True(status.IsConnected);
        Assert.Equal(IPAddress.Parse("203.0.113.10"), status.PublicIPAddress);
        Assert.Equal(OutboundUriGuard.LocalNoRedirectHttpClientName, factory.ClientNames.Single());
        Assert.Equal(new Uri("http://127.0.0.1:8010/v1/publicip/ip"), handler.Requests.Single());
    }

    [Fact]
    public async Task GetStatusAsync_Maps_Self_Hosted_Relay_Health()
    {
        var handler = new StubHttpMessageHandler(new Dictionary<string, string>
        {
            ["/v1/publicip/ip"] = """{"public_ip":"203.0.113.10"}""",
            ["/v1/portforward"] = """{"port":50300}""",
            ["/v1/slskdn/portforwards"] = """{"forwards":[]}""",
            ["/v1/slskdn/relay"] = """
                {
                  "mode":"self-hosted-relay",
                  "transport":"tailscale",
                  "connected":true,
                  "latencyMs":12.5,
                  "rxBytes":1048576,
                  "txBytes":2097152,
                  "activeConnections":4,
                  "connectionLimit":128,
                  "bandwidthLimitMbit":100,
                  "latestHandshakeAt":"2026-07-30T15:00:00Z",
                  "path":"pong from home via DERP(tor) in 12.5ms"
                }
                """,
        });
        var options = new Options
        {
            Integration = new Options.IntegrationOptions
            {
                Vpn = new Options.IntegrationOptions.VpnOptions
                {
                    PortForwarding = true,
                    SelfHostedRelay = true,
                    Gluetun = new Options.IntegrationOptions.VpnOptions.GluetunVpnOptions
                    {
                        Url = "http://10.77.0.1:8010",
                        ApiKey = "relay-status-key",
                    },
                },
            },
        };

        var status = await new Gluetun(new CapturingHttpClientFactory(handler), new TestOptionsMonitor<Options>(options)).GetStatusAsync();

        Assert.Equal(50300, status.ForwardedPort);
        Assert.NotNull(status.Relay);
        Assert.True(status.Relay.Connected);
        Assert.Equal("tailscale", status.Relay.Transport);
        Assert.Equal(12.5, status.Relay.LatencyMs);
        Assert.Equal(1048576, status.Relay.RxBytes);
        Assert.Equal(2097152, status.Relay.TxBytes);
        Assert.Equal(4, status.Relay.ActiveConnections);
        Assert.Equal(128, status.Relay.ConnectionLimit);
        Assert.Equal(100, status.Relay.BandwidthLimitMbit);
        Assert.Equal("pong from home via DERP(tor) in 12.5ms", status.Relay.Path);
    }

    [Fact]
    public async Task GetStatusAsync_Preserves_Disconnected_Relay_Diagnostics()
    {
        var handler = new StubHttpMessageHandler(new Dictionary<string, string>
        {
            ["/v1/slskdn/relay"] = """{"mode":"self-hosted-relay","connected":false,"connectionLimit":128}""",
            ["/v1/publicip/ip"] = """{"public_ip":""}""",
        });
        var options = new Options
        {
            Integration = new Options.IntegrationOptions
            {
                Vpn = new Options.IntegrationOptions.VpnOptions
                {
                    SelfHostedRelay = true,
                    Gluetun = new Options.IntegrationOptions.VpnOptions.GluetunVpnOptions
                    {
                        Url = "http://10.77.0.1:8010",
                    },
                },
            },
        };

        var status = await new Gluetun(new CapturingHttpClientFactory(handler), new TestOptionsMonitor<Options>(options)).GetStatusAsync();

        Assert.False(status.IsConnected);
        Assert.NotNull(status.Relay);
        Assert.False(status.Relay.Connected);
        Assert.Equal(128, status.Relay.ConnectionLimit);
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public CapturingHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public List<string> ClientNames { get; } = new();

        public HttpClient CreateClient(string name)
        {
            ClientNames.Add(name);
            return new HttpClient(_handler, disposeHandler: false);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly IReadOnlyDictionary<string, string>? _responses;

        public StubHttpMessageHandler(IReadOnlyDictionary<string, string>? responses = null)
        {
            _responses = responses;
        }

        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Request URI was missing."));

            var content = _responses is not null
                ? _responses[request.RequestUri.AbsolutePath]
                : """
                  {
                    "public_ip": "203.0.113.10",
                    "city": "Regina",
                    "country": "Canada"
                  }
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content),
            });
        }
    }
}
