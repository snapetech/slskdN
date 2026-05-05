// <copyright file="PlaybackPriorityServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Playback;

using Microsoft.Extensions.Logging.Abstractions;
using slskd.Transfers.MultiSource.Playback;
using Xunit;

public class PlaybackPriorityServiceTests
{
    [Theory]
    [InlineData(0, PriorityZone.High)]
    [InlineData(4_999, PriorityZone.High)]
    [InlineData(5_000, PriorityZone.Mid)]
    [InlineData(29_999, PriorityZone.Mid)]
    [InlineData(30_000, PriorityZone.Low)]
    public async Task GetPriority_UsesCurrentBufferAhead(long bufferAheadMs, PriorityZone expected)
    {
        var service = new PlaybackPriorityService(NullLogger<PlaybackPriorityService>.Instance);

        await service.RecordAsync(new PlaybackFeedback
        {
            JobId = "job-1",
            BufferAheadMs = bufferAheadMs,
        });

        Assert.Equal(expected, service.GetPriority("job-1"));
    }
}
