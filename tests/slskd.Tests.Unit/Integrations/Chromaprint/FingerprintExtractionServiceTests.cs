// <copyright file="FingerprintExtractionServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.Chromaprint;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Integrations.Chromaprint;
using Xunit;
using ChromaprintOptions = slskd.Options.IntegrationOptions.ChromaprintOptions;

public class FingerprintExtractionServiceTests
{
    [Fact]
    public void GetMaximumPcmBytes_ReturnsExpectedBound()
    {
        var options = new ChromaprintOptions
        {
            SampleRate = 44100,
            Channels = 2,
            DurationSeconds = 120,
        };

        var maxBytes = FingerprintExtractionService.GetMaximumPcmBytes(options);

        Assert.Equal(21168000, maxBytes);
    }

    [Fact]
    public async Task ReadBoundedPcmAsync_ReturnsBufferWithinLimit()
    {
        var expected = new byte[4096];
        new Random(1234).NextBytes(expected);
        await using var stream = new MemoryStream(expected);

        var actual = await FingerprintExtractionService.ReadBoundedPcmAsync(stream, expected.Length, default);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ReadBoundedPcmAsync_ThrowsWhenStreamExceedsLimit()
    {
        await using var stream = new MemoryStream(new byte[FingerprintExtractionService.CopyBufferSize + 1]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FingerprintExtractionService.ReadBoundedPcmAsync(stream, FingerprintExtractionService.CopyBufferSize, default));

        Assert.Contains("more PCM output than expected", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractFingerprintAsync_StartsProcessBeforeReadingStandardError()
    {
        var options = new slskd.Options
        {
            Integration = new slskd.Options.IntegrationOptions
            {
                Chromaprint = new ChromaprintOptions
                {
                    Enabled = true,
                    FfmpegPath = GetProcessPath(),
                    SampleRate = 1,
                    Channels = 1,
                    DurationSeconds = 1,
                },
            },
        };
        var chromaprint = new Mock<IChromaprintService>();
        var service = new FingerprintExtractionService(
            chromaprint.Object,
            new TestOptionsMonitor<slskd.Options>(options),
            NullLogger<FingerprintExtractionService>.Instance);
        var filePath = Path.GetTempFileName();

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExtractFingerprintAsync(filePath));

            Assert.Contains("no PCM output", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("StandardError has not been redirected", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void FormatDiagnostic_ReturnsUsefulFallbackForEmptyOutput()
    {
        Assert.Equal("(no diagnostic output)", FingerprintExtractionService.FormatDiagnostic(" \n\t"));
        Assert.Equal("decoder failed", FingerprintExtractionService.FormatDiagnostic(" \ndecoder failed\n"));
    }

    private static string GetProcessPath()
    {
        return OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
    }
}
