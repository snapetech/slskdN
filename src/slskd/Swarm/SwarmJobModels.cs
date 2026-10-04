// <copyright file="SwarmJobModels.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Swarm;

/// <summary>
/// Swarm job model for orchestrated transfers.
/// </summary>
public record SwarmJob(string JobId, SwarmFile File, IReadOnlyList<SwarmSource> Sources, string? VariantId = null)
{
    public bool HasVariant(string variantId) =>
        !string.IsNullOrWhiteSpace(variantId) &&
        string.Equals(VariantId, variantId, StringComparison.Ordinal);
}

public record SwarmFile(string ContentId, string Hash, long SizeBytes, string? Codec = null, string? Filename = null);

public record SwarmSource(string PeerId, string Transport, string? Address = null, int? Port = null);
