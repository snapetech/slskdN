// <copyright file="ActivityPubFriendsOnlyAuthorizationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SocialFederation;

using slskd.SocialFederation.API;
using Xunit;

public sealed class ActivityPubFriendsOnlyAuthorizationTests
{
    [Fact]
    public void IsFriendsOnlyIdentityAuthorized_ApprovedHostWithoutVerifiedSignature_IsDenied()
    {
        var authorized = ActivityPubController.IsFriendsOnlyIdentityAuthorized(
            signatureVerified: false,
            "https://friend.example/actors/music",
            new[] { "friend.example" });

        Assert.False(authorized);
    }

    [Fact]
    public void IsFriendsOnlyIdentityAuthorized_VerifiedApprovedActor_IsAllowed()
    {
        var authorized = ActivityPubController.IsFriendsOnlyIdentityAuthorized(
            signatureVerified: true,
            "https://friend.example/actors/music",
            new[] { "FRIEND.EXAMPLE" });

        Assert.True(authorized);
    }

    [Fact]
    public void ResolveActorIdentity_RemovesKeyFragment()
    {
        var actor = ActivityPubController.ResolveActorIdentity("https://friend.example/actors/music#main-key");

        Assert.Equal("https://friend.example/actors/music", actor);
    }

    [Theory]
    [InlineData("(request-target) host date", false, true)]
    [InlineData("(request-target) host (created)", false, true)]
    [InlineData("(request-target) host date digest", true, true)]
    [InlineData("host date digest", true, false)]
    [InlineData("(request-target) date digest", true, false)]
    [InlineData("(request-target) host digest", true, false)]
    [InlineData("(request-target) host date", true, false)]
    [InlineData("(request-target) host date date digest", true, false)]
    public void HasRequiredSignedHeaders_EnforcesSecurityCriticalSet(
        string headers,
        bool bodyBearing,
        bool expected)
    {
        Assert.Equal(expected, ActivityPubController.HasRequiredSignedHeaders(headers, bodyBearing));
    }

    [Fact]
    public void IsFreshCreatedTimestamp_RejectsMissingStaleAndOutOfRangeValues()
    {
        Assert.False(ActivityPubController.IsFreshCreatedTimestamp(null));
        Assert.False(ActivityPubController.IsFreshCreatedTimestamp(DateTimeOffset.UtcNow.AddMinutes(-6).ToUnixTimeSeconds()));
        Assert.False(ActivityPubController.IsFreshCreatedTimestamp(long.MaxValue));
        Assert.True(ActivityPubController.IsFreshCreatedTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }
}
