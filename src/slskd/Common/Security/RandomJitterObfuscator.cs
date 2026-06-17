// <copyright file="RandomJitterObfuscator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security;

/// <summary>
/// Random jitter timing obfuscator that adds random delays to prevent timing correlation attacks.
/// </summary>
public class RandomJitterObfuscator : ITimingObfuscator
{
    private readonly TimingObfuscationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomJitterObfuscator"/> class.
    /// </summary>
    /// <param name="options">The timing obfuscation options.</param>
    public RandomJitterObfuscator(TimingObfuscationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the delay to apply before sending the next message.
    /// Returns a random delay between 0 and JitterMs milliseconds.
    /// </summary>
    /// <returns>The delay in milliseconds.</returns>
    public Task<int> GetNextDelayAsync()
    {
        // Clamp JitterMs to avoid Next(0, n) when n <= 0; invalid config is handled gracefully
        int jitter = _options.JitterMs < 0 ? 0 : _options.JitterMs;
        int delay = Random.Shared.Next(0, jitter + 1);
        return Task.FromResult(delay);
    }

    /// <summary>
    /// Gets the configured maximum jitter value.
    /// </summary>
    public int MaxJitterMs => _options.JitterMs < 0 ? 0 : _options.JitterMs;
}
