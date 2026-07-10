// <copyright file="MeshPeerPublicKeyResolver.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Mesh.ServiceFabric;

using slskd.Mesh.Dht;
using slskd.Mesh.Transport;

public interface IMeshPeerPublicKeyResolver
{
    Task<IReadOnlyList<byte[]>> ResolveTrustedKeysAsync(string peerId);
}

public sealed class MeshPeerPublicKeyResolver : IMeshPeerPublicKeyResolver
{
    private readonly IMeshDhtClient _dhtClient;

    public MeshPeerPublicKeyResolver(IMeshDhtClient dhtClient)
    {
        _dhtClient = dhtClient;
    }

    public async Task<IReadOnlyList<byte[]>> ResolveTrustedKeysAsync(string peerId)
    {
        var descriptor = await _dhtClient.GetAsync<MeshPeerDescriptor>($"mesh:peer:{peerId}");
        if (descriptor is null
            || descriptor.IsExpired()
            || !string.Equals(descriptor.PeerId, peerId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(descriptor.Signature))
        {
            return Array.Empty<byte[]>();
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(descriptor.Signature);
        }
        catch (FormatException)
        {
            return Array.Empty<byte[]>();
        }

        using var signer = new Ed25519Signer();
        foreach (var encodedKey in descriptor.ControlSigningKeys.Distinct(StringComparer.Ordinal))
        {
            byte[] publicKey;
            try
            {
                publicKey = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                continue;
            }

            if (publicKey.Length == 32
                && string.Equals(Ed25519Signer.DerivePeerId(publicKey), peerId, StringComparison.Ordinal)
                && signer.Verify(descriptor.GetSignableData(), signature, publicKey))
            {
                return new[] { publicKey };
            }
        }

        return Array.Empty<byte[]>();
    }
}
