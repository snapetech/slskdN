// <copyright file="WebHostConfigurationExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using slskd.Cryptography;

public static class WebHostConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureSlskdWebHost(
        this WebApplicationBuilder builder,
        OptionsAtStartup optionsAtStartup,
        string appName)
    {
        var webPortSection = builder.Configuration.GetSection($"{appName}:Web:Port");
        var webPort = webPortSection.Exists() && int.TryParse(webPortSection.Value, out var port)
            ? port
            : optionsAtStartup.Web.Port;

        var webAddressSection = builder.Configuration.GetSection($"{appName}:Web:Address");
        var webAddress = webAddressSection.Exists() && !string.IsNullOrEmpty(webAddressSection.Value)
            ? webAddressSection.Value
            : optionsAtStartup.Web.Address;

        var configuredAddress = webAddress == "*" ? IPAddress.Any.ToString() : webAddress;
        if (!IPAddress.TryParse(configuredAddress, out var listenAddress))
        {
            Log.Warning("Invalid web bind address '{Address}', defaulting to 0.0.0.0", configuredAddress);
            listenAddress = IPAddress.Any;
        }

        var listenAddressUrl = listenAddress.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{listenAddress}]"
            : listenAddress.ToString();

        builder.WebHost
            .UseUrls($"http://{listenAddressUrl}:{webPort}")
            .UseKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = optionsAtStartup.Web.MaxRequestBodySize;

                Log.Debug(
                    "[ConfigProbe] slskd:web:port={A} slskd:slskd:web:port={B} using={C}",
                    builder.Configuration.GetValue<string>($"{appName}:Web:Port") ?? "null",
                    builder.Configuration.GetValue<string>($"{appName}:{appName}:Web:Port") ?? "null",
                    webPort);

                Log.Information(
                    "[Kestrel] Configuring HTTP listener at http://{ListenAddressUrl}:{WebPort}/ (from config: port={PortConfigured}, address={AddressConfigured})",
                    listenAddressUrl,
                    webPort,
                    webPortSection.Exists(),
                    webAddressSection.Exists());
                options.Listen(listenAddress, webPort);
                Log.Debug("[Kestrel] HTTP listener configured");

                if (!string.IsNullOrWhiteSpace(optionsAtStartup.Web.Socket))
                {
                    Log.Information("Configuring HTTP listener on unix domain socket (UDS) {Socket}", optionsAtStartup.Web.Socket);
                    options.ListenUnixSocket(optionsAtStartup.Web.Socket);
                }

                if (!optionsAtStartup.Web.Https.Disabled)
                {
                    Log.Information(
                        "Configuring HTTPS listener at https://{ListenAddress}:{HttpsPort}/",
                        IPAddress.Any,
                        optionsAtStartup.Web.Https.Port);
                    options.Listen(IPAddress.Any, optionsAtStartup.Web.Https.Port, listenOptions =>
                    {
                        var cert = optionsAtStartup.Web.Https.Certificate;

                        if (!string.IsNullOrEmpty(cert.Pfx))
                        {
                            Log.Information("Using certificate from {CertificatePath}", cert.Pfx);
                            listenOptions.UseHttps(cert.Pfx, cert.Password);
                        }
                        else
                        {
                            Log.Information("Using randomly generated self-signed certificate");
                            listenOptions.UseHttps(X509.Generate(subject: appName));
                        }
                    });
                }
            });

        return builder;
    }
}
