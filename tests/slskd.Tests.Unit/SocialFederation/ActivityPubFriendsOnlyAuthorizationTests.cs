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
}
