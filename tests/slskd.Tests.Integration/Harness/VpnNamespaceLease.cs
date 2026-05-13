// <copyright file="VpnNamespaceLease.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Integration.Harness;

using System.Threading;

internal sealed record VpnNamespaceLease(
    string Wrapper,
    string Config,
    int Index,
    string NamespaceName,
    string NamespaceIp,
    string NamespaceHostIp,
    string NamespaceSubnet);

internal static class VpnNamespaceLeaseAllocator
{
    private static int vpnWrapperIndex;

    public static VpnNamespaceLease? Allocate()
    {
        var wrapper = Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_WRAPPER");
        var vpnConfigs = Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_CONFIGS")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.IsNullOrWhiteSpace(wrapper) || vpnConfigs is not { Length: > 0 })
        {
            return null;
        }

        if (ShouldClaimPortForwarding())
        {
            vpnConfigs = vpnConfigs
                .Where(IsNatPmpCapableConfig)
                .ToArray();
            if (vpnConfigs.Length == 0)
            {
                throw new InvalidOperationException(
                    "SLSKDN_FULL_INSTANCE_VPN_CLAIM_PORT_FORWARDING is enabled, but no configured VPN file is marked NAT-PMP capable.");
            }
        }

        var index = Interlocked.Increment(ref vpnWrapperIndex) - 1;
        var config = vpnConfigs[index % vpnConfigs.Length];
        var subnetOctet = 230 + index;
        var namespacePrefix = Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_NAMESPACE_PREFIX") ?? "sln";
        var namespaceName = BuildVpnNamespaceName(namespacePrefix, index);

        return new VpnNamespaceLease(
            wrapper,
            config,
            index,
            namespaceName,
            $"10.{subnetOctet}.0.2",
            $"10.{subnetOctet}.0.1",
            $"10.{subnetOctet}.0.0/24");
    }

    public static bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_WRAPPER")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_CONFIGS"));
    }

    private static string BuildVpnNamespaceName(string prefix, int index)
    {
        var safePrefix = new string(prefix.Where(char.IsLetterOrDigit).Take(4).ToArray());
        if (string.IsNullOrWhiteSpace(safePrefix))
        {
            safePrefix = "sln";
        }

        return $"{safePrefix}{index:D2}";
    }

    private static bool ShouldClaimPortForwarding()
    {
        var value = Environment.GetEnvironmentVariable("SLSKDN_FULL_INSTANCE_VPN_CLAIM_PORT_FORWARDING");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNatPmpCapableConfig(string config)
    {
        if (!File.Exists(config))
        {
            return true;
        }

        var natPmpLine = File.ReadLines(config)
            .FirstOrDefault(line => line.Contains("NAT-PMP", StringComparison.OrdinalIgnoreCase));
        return natPmpLine == null ||
            !natPmpLine.Contains("off", StringComparison.OrdinalIgnoreCase);
    }
}
