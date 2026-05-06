// <copyright file="GluetunTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.VPN;

using System;
using System.Collections.Generic;
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
        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Request URI was missing."));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "public_ip": "203.0.113.10",
                      "city": "Regina",
                      "country": "Canada"
                    }
                    """),
            });
        }
    }
}
