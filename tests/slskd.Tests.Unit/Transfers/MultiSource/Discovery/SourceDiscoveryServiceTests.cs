// <copyright file="SourceDiscoveryServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Discovery;

using Moq;
using slskd.Common.Security;
using slskd.Transfers.MultiSource;
using slskd.Transfers.MultiSource.Discovery;
using Soulseek;
using Xunit;

public class SourceDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoveryLoop_WhenSafetyLimiterRejects_DoesNotSearchSoulseek()
    {
        var appDirectory = Path.Combine(Path.GetTempPath(), $"slskdn-discovery-test-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(appDirectory);

        try
        {
            var limiterCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var safetyLimiter = new Mock<ISoulseekSafetyLimiter>();
            safetyLimiter
                .Setup(limiter => limiter.TryConsumeSearch("source-discovery"))
                .Callback(() => limiterCalled.TrySetResult())
                .Returns(false);

            var soulseekClient = new Mock<ISoulseekClient>(MockBehavior.Strict);
            var service = new SourceDiscoveryService(
                appDirectory,
                soulseekClient.Object,
                Mock.Of<IContentVerificationService>(),
                safetyLimiter.Object);

            await service.StartDiscoveryAsync("beatles", enableHashVerification: false);
            await limiterCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopDiscoveryAsync();

            soulseekClient.VerifyNoOtherCalls();
        }
        finally
        {
            System.IO.Directory.Delete(appDirectory, recursive: true);
        }
    }
}
