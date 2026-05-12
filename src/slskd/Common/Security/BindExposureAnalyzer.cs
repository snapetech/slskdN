// <copyright file="BindExposureAnalyzer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security;

using System;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Classifies web listener exposure for startup hardening checks.
/// </summary>
/// <remarks>
/// Port-enabled is not the same as remote-reachable. Hardening checks should use
/// the actual bind address/socket posture, not only whether HTTP/HTTPS ports are configured.
/// </remarks>
public static class BindExposureAnalyzer
{
    /// <summary>
    /// Classifies the configured web listener posture.
    /// </summary>
    /// <param name="options">Startup options containing web listener settings.</param>
    /// <returns>The most exposed listener classification.</returns>
    public static BindExposure AnalyzeWebBinding(OptionsAtStartup options)
    {
        if (options?.Web == null)
        {
            return BindExposure.None;
        }

        var httpExposure = Analyze(options.Web.Address, options.Web.Port, options.Web.Socket);
        var httpsExposure = options.Web.Https.Disabled
            ? BindExposure.None
            : Analyze(IPAddress.Any.ToString(), options.Web.Https.Port);

        return MostExposed(httpExposure, httpsExposure);
    }

    /// <summary>
    /// Classifies a single web listener binding.
    /// </summary>
    /// <param name="address">Configured bind address. Wildcard values such as <c>*</c>, <c>0.0.0.0</c>, and <c>::</c> are remote reachable.</param>
    /// <param name="port">Configured TCP port. Values less than or equal to zero mean no TCP listener.</param>
    /// <param name="unixSocket">Configured Unix socket path, if any.</param>
    /// <returns>The exposure classification.</returns>
    public static BindExposure Analyze(string? address, int port, string? unixSocket = null)
    {
        var hasTcpListener = port > 0;
        var hasUnixSocket = !string.IsNullOrWhiteSpace(unixSocket);

        if (!hasTcpListener)
        {
            return hasUnixSocket ? BindExposure.UnixSocketOnly : BindExposure.None;
        }

        if (string.IsNullOrWhiteSpace(address) || address == "*")
        {
            return BindExposure.AnyAddress;
        }

        var normalized = address.Trim();
        if (string.Equals(normalized, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return BindExposure.LoopbackOnly;
        }

        if (!IPAddress.TryParse(normalized, out var ipAddress))
        {
            return BindExposure.Unknown;
        }

        if (IPAddress.IsLoopback(ipAddress))
        {
            return BindExposure.LoopbackOnly;
        }

        if (IPAddress.Any.Equals(ipAddress) || IPAddress.IPv6Any.Equals(ipAddress))
        {
            return BindExposure.AnyAddress;
        }

        return IsPrivateAddress(ipAddress)
            ? BindExposure.NonLoopbackPrivate
            : BindExposure.NonLoopbackPublic;
    }

    /// <summary>
    /// Returns whether an exposure is reachable from outside the local machine.
    /// </summary>
    /// <param name="exposure">The exposure classification.</param>
    /// <returns>True when the bind posture is remote reachable.</returns>
    public static bool IsRemoteReachable(BindExposure exposure) => exposure is
        BindExposure.NonLoopbackPrivate or
        BindExposure.NonLoopbackPublic or
        BindExposure.AnyAddress or
        BindExposure.Unknown;

    private static bool IsPrivateAddress(IPAddress ipAddress)
    {
        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            return bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) ||
                (bytes[0] == 169 && bytes[1] == 254);
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal || IsUniqueLocalIPv6(ipAddress);
        }

        return false;
    }

    private static bool IsUniqueLocalIPv6(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        return bytes.Length > 0 && (bytes[0] & 0xFE) == 0xFC;
    }

    private static BindExposure MostExposed(BindExposure first, BindExposure second) =>
        ExposureRank(first) >= ExposureRank(second) ? first : second;

    private static int ExposureRank(BindExposure exposure) => exposure switch
    {
        BindExposure.None => 0,
        BindExposure.UnixSocketOnly => 1,
        BindExposure.LoopbackOnly => 2,
        BindExposure.NonLoopbackPrivate => 3,
        BindExposure.NonLoopbackPublic => 4,
        BindExposure.AnyAddress => 5,
        BindExposure.Unknown => 6,
        _ => 6,
    };
}

/// <summary>
/// Bind exposure classification for web listener hardening.
/// </summary>
public enum BindExposure
{
    /// <summary>No web listener is enabled.</summary>
    None,

    /// <summary>The listener is reachable only through loopback.</summary>
    LoopbackOnly,

    /// <summary>The listener is reachable only through a Unix socket.</summary>
    UnixSocketOnly,

    /// <summary>The listener is bound to a non-loopback private/link-local address.</summary>
    NonLoopbackPrivate,

    /// <summary>The listener is bound to a non-loopback public address.</summary>
    NonLoopbackPublic,

    /// <summary>The listener is bound to all addresses.</summary>
    AnyAddress,

    /// <summary>The listener is enabled but the bind address cannot be classified safely.</summary>
    Unknown,
}
