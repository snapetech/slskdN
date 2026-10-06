// <copyright file="PublicPolicyCollectionTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Collections.Generic;
using slskd.Capabilities;
using slskd.Common.CodeQuality;
using slskd.Common.Security;
using slskd.Transfers.MultiSource;
using Xunit;
using DhtPathGuard = slskd.DhtRendezvous.Security.PathGuard;
using MeshBucketPadder = slskd.Mesh.Privacy.BucketPadder;

public class PublicPolicyCollectionTests
{
    [Fact]
    public void SharedPolicyCollectionsRejectMutation()
    {
        AssertReadOnly(PathGuard.ForbiddenChars);
        AssertReadOnly(PathGuard.DangerousExtensions);
        AssertReadOnly(PathGuard.SafeAudioExtensions);
        AssertReadOnly(DhtPathGuard.SafeAudioExtensions);
        AssertReadOnly(Honeypot.DecoyFiles);
        AssertReadOnly(CapabilityFileService.AlternativePaths);
        AssertReadOnly(MeshBucketPadder.StandardBucketSizes);
        AssertReadOnly(FlacStreamInfoParser.FlacMagic);
        AssertReadOnly(TestCoverage.CriticalSubsystems);
        AssertReadOnly(slskd.SoulseekObfuscationSupport.SupportedConnectionTypes);
    }

    [Fact]
    public void DatabaseListReturnsAnIsolatedArraySnapshot()
    {
        var firstRead = global::Database.List;
        firstRead[0] = new global::Database { Name = "changed" };

        Assert.Equal("search", global::Database.List[0].Name);
    }

    [Fact]
    public void PathAndFilePoliciesRemainEnforcedAfterRejectedMutation()
    {
        var forbiddenCharacters = Assert.IsAssignableFrom<IList<char>>(PathGuard.ForbiddenChars);
        Assert.Throws<System.NotSupportedException>(() => forbiddenCharacters[0] = 'x');

        var dangerousExtensions = Assert.IsAssignableFrom<IList<string>>(PathGuard.DangerousExtensions);
        Assert.Throws<System.NotSupportedException>(() => dangerousExtensions[0] = ".txt");

        var safeAudioExtensions = Assert.IsAssignableFrom<IList<string>>(DhtPathGuard.SafeAudioExtensions);
        Assert.Throws<System.NotSupportedException>(() => safeAudioExtensions[0] = ".exe");

        Assert.Equal("song_.flac", PathGuard.SanitizeFilename("song?.flac"));
        Assert.True(PathGuard.HasDangerousExtension("payload.exe"));
        Assert.True(DhtPathGuard.HasSafeAudioExtension("track.flac"));
        Assert.True(FlacStreamInfoParser.IsFlac([0x66, 0x4C, 0x61, 0x43]));
        Assert.Contains(512, MeshBucketPadder.StandardBucketSizes);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var mutableView = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.NotEmpty(values);
        Assert.Throws<System.NotSupportedException>(() => mutableView[0] = values[0]);
    }
}
