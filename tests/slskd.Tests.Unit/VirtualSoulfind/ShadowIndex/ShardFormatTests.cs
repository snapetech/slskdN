// <copyright file="ShardFormatTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.ShadowIndex;

using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

public class ShardFormatTests
{
    [Fact]
    public void Serialize_HasFrozenCrossRuntimeMessagePackShape()
    {
        var shard = new ShadowIndexShard
        {
            ShardVersion = "1.0",
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123),
            TTLSeconds = 3600,
            PeerIdHints = [Enumerable.Range(0, 8).Select(value => (byte)value).ToArray()],
            CanonicalVariants =
            [
                new VariantHint
                {
                    Codec = "FLAC",
                    BitrateKbps = 900,
                    SizeBytes = 42,
                    HashPrefix = Enumerable.Range(16, 16).Select(value => (byte)value).ToArray(),
                    QualityScore = 0.75,
                },
            ],
            ApproximatePeerCount = 1,
        };

        Assert.Equal(
            "lqMxLjCS1/8dU1MAZVPxAADNDhCRxAgAAQIDBAUGB5GVpEZMQUPNA4QqxBAQERITFBUWFxgZGhscHR4fyz/oAAAAAAAAAQ==",
            Convert.ToBase64String(ShardSerializer.Serialize(shard)));
    }
}
