// <copyright file="ApplicationPrivateMessageAutoResponseTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core;

using Xunit;

public sealed class ApplicationPrivateMessageAutoResponseTests
{
    [Theory]
    [InlineData("prove you are human")]
    [InlineData("please verify you are not a bot before downloading")]
    [InlineData("human check: reply first")]
    [InlineData("are you real?")]
    [InlineData("bot?")]
    [InlineData("u human?")]
    [InlineData("say anything so I know you are not a bot")]
    [InlineData("reply if you're a real person")]
    [InlineData("type hello to pass the bot check")]
    [InlineData("captcha check - answer before I unban")]
    [InlineData("anti bot verification: respond please")]
    [InlineData("are you using an automated client?")]
    [InlineData("write back to prove this is not automated")]
    public void IsHumanChallengePrivateMessage_WithHumanCheckPrompt_ReturnsTrue(string message)
    {
        Assert.True(Application.IsHumanChallengePrivateMessage(message));
    }

    [Theory]
    [InlineData("thanks for sharing")]
    [InlineData("do you have this album in flac?")]
    [InlineData("your queue is full")]
    [InlineData("human after all is a great album")]
    [InlineData("that bot in the room is annoying")]
    [InlineData("I use an automated folder sorter")]
    [InlineData("can you reply when the queue opens?")]
    [InlineData("are you sharing the deluxe version?")]
    [InlineData("captcha samples from the soundtrack")]
    [InlineData("not a botched rip, sounds clean")]
    public void IsHumanChallengePrivateMessage_WithNormalMessage_ReturnsFalse(string message)
    {
        Assert.False(Application.IsHumanChallengePrivateMessage(message));
    }

    [Theory]
    [InlineData("It looks like you're not sharing any files. Please share something before downloading anything from my own shares. Thanks.")]
    [InlineData("Please consider sharing more files if you would like to download from me again. Thanks :)")]
    [InlineData("[AUTO-BAN] No leechers. Only sharing with 1k+ file users.")]
    [InlineData(" ;) Empty shares?")]
    [InlineData("PLEASE SHARE MORE FILES!!")]
    public void IsShareGatePrivateMessage_WithShareGatePrompt_ReturnsTrue(string message)
    {
        Assert.True(Application.IsShareGatePrivateMessage(message));
        Assert.True(Application.IsPrivateMessageAutoResponseCandidate(message));
    }

    [Theory]
    [InlineData("thanks for sharing")]
    [InlineData("can you share the deluxe version?")]
    [InlineData("empty rooms on that album sound great")]
    [InlineData("the files are tagged correctly")]
    [InlineData("I like the Leechers album")]
    public void IsShareGatePrivateMessage_WithNormalMessage_ReturnsFalse(string message)
    {
        Assert.False(Application.IsShareGatePrivateMessage(message));
        Assert.False(Application.IsPrivateMessageAutoResponseCandidate(message));
    }
}
