// <copyright file="PerceptualHasher.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.MediaCore;

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using MathNet.Numerics.IntegralTransforms;
using slskd;

/// <summary>
/// Supported perceptual hash algorithms.
/// </summary>
public enum PerceptualHashAlgorithm
{
    /// <summary>
    /// Chromaprint algorithm for audio fingerprinting.
    /// </summary>
    Chromaprint,

    /// <summary>
    /// pHash algorithm for image/video perceptual hashing.
    /// </summary>
    PHash,

    /// <summary>
    /// Simple spectral hash (fallback/default).
    /// </summary>
    Spectral
}

/// <summary>
/// Perceptual hashing for content similarity detection.
/// Generates compact fingerprints that remain similar for perceptually similar content,
/// enabling cross-codec/bitrate deduplication and content matching.
/// </summary>
public interface IPerceptualHasher
{
    /// <summary>
    /// Generates perceptual hash from audio PCM samples.
    /// </summary>
    /// <param name="samples">Audio PCM samples (mono, normalized -1.0 to 1.0)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="algorithm">Hash algorithm to use</param>
    /// <returns>Perceptual hash result</returns>
    PerceptualHash ComputeAudioHash(float[] samples, int sampleRate, PerceptualHashAlgorithm algorithm = PerceptualHashAlgorithm.Chromaprint);

    /// <summary>
    /// Generates perceptual hash from image pixels.
    /// </summary>
    /// <param name="pixels">Image pixel data (RGBA byte array)</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="algorithm">Hash algorithm to use</param>
    /// <returns>Perceptual hash result</returns>
    PerceptualHash ComputeImageHash(byte[] pixels, int width, int height, PerceptualHashAlgorithm algorithm = PerceptualHashAlgorithm.PHash);

    /// <summary>
    /// Computes Hamming distance between two hashes (0-64).
    /// Lower distance indicates more similar content.
    /// </summary>
    int HammingDistance(ulong hashA, ulong hashB);

    /// <summary>
    /// Computes similarity score between two hashes (0.0 to 1.0).
    /// 1.0 = identical, 0.0 = completely different.
    /// </summary>
    double Similarity(ulong hashA, ulong hashB);

    /// <summary>
    /// Determines if two hashes are similar within a threshold.
    /// </summary>
    /// <param name="hashA">First hash</param>
    /// <param name="hashB">Second hash</param>
    /// <param name="threshold">Similarity threshold (0.0-1.0)</param>
    /// <returns>True if hashes are similar</returns>
    bool AreSimilar(ulong hashA, ulong hashB, double threshold = 0.8);
}

public class PerceptualHasher : IPerceptualHasher
{
    private const int ChromaBins = 24;
    private const int ChromaFftSize = 4096;
    private const double ChromaReferenceFrequency = 55.0;
    private const int ChromaTargetSampleRate = 11025;
    private const int FrameSize = 4096; // Frame size for analysis
    private const int HashBits = 64;    // 64-bit hash output
    private static readonly double[] ChromaHannWindow = CreateChromaHannWindow();
    private static readonly int[] TargetRateChromaPerFftBin = CreateChromaPerFftBin(ChromaTargetSampleRate);

    public PerceptualHash ComputeAudioHash(float[] samples, int sampleRate, PerceptualHashAlgorithm algorithm = PerceptualHashAlgorithm.Chromaprint)
    {
        var numericHash = ComputeAudioHashNumeric(samples, sampleRate, algorithm);
        var hexHash = numericHash.ToString("X16");
        return new PerceptualHash(algorithm.ToString(), hexHash, numericHash);
    }

    public PerceptualHash ComputeImageHash(byte[] pixels, int width, int height, PerceptualHashAlgorithm algorithm = PerceptualHashAlgorithm.PHash)
    {
        var numericHash = ComputeImageHashNumeric(pixels, width, height, algorithm);
        var hexHash = numericHash.ToString("X16");
        return new PerceptualHash(algorithm.ToString(), hexHash, numericHash);
    }

    public ulong ComputeHash(float[] samples, int sampleRate)
    {
        if (samples == null || samples.Length == 0)
            return 0;

        // Compute spectral features across 8 time windows
        Span<double> features = stackalloc double[8];
        const int TargetRate = 11025;
        var downsampleRatio = sampleRate > TargetRate
            ? sampleRate / (double)TargetRate
            : 1.0;
        var analyzedLength = sampleRate > TargetRate
            ? (int)(samples.Length / downsampleRatio)
            : samples.Length;
        var windowSize = analyzedLength / 8;

        for (int w = 0; w < 8; w++)
        {
            var start = w * windowSize;
            var end = Math.Min(start + windowSize, analyzedLength);

            // Compute energy in frequency bands (simplified spectral feature)
            features[w] = downsampleRatio == 1.0
                ? ComputeSpectralEnergy(samples.AsSpan(start, end - start))
                : ComputeDownsampledSpectralEnergy(samples, downsampleRatio, start, end);
        }

        // Generate hash from feature comparisons
        return GenerateHash(features);
    }

    public int HammingDistance(ulong hashA, ulong hashB)
    {
        return BitOperations.PopCount(hashA ^ hashB);
    }

    public double Similarity(ulong hashA, ulong hashB)
    {
        var distance = HammingDistance(hashA, hashB);
        return 1.0 - ((double)distance / HashBits);
    }

    public bool AreSimilar(ulong hashA, ulong hashB, double threshold = 0.8)
    {
        return Similarity(hashA, hashB) >= threshold;
    }

    /// <summary>
    /// Downsamples audio to target sample rate using simple decimation.
    /// </summary>
    private static float[] Downsample(float[] samples, int fromRate, int toRate)
    {
        var ratio = fromRate / (double)toRate;
        var newLength = (int)(samples.Length / ratio);
        var result = new float[newLength];

        for (int i = 0; i < newLength; i++)
        {
            var srcIndex = (int)(i * ratio);
            if (srcIndex < samples.Length)
                result[i] = samples[srcIndex];
        }

        return result;
    }

    /// <summary>
    /// Computes spectral energy for a window of samples.
    /// Simplified frequency analysis without full FFT.
    /// </summary>
    private static double ComputeSpectralEnergy(ReadOnlySpan<float> window)
    {
        if (window.Length == 0) return 0;

        // Compute RMS energy as simplified spectral feature
        var sum = 0.0;
        foreach (var sample in window)
        {
            sum += sample * sample;
        }

        return Math.Sqrt(sum / window.Length);
    }

    private static double ComputeDownsampledSpectralEnergy(
        ReadOnlySpan<float> samples,
        double ratio,
        int start,
        int end)
    {
        if (start == end)
        {
            return 0;
        }

        var sum = 0.0;
        for (var index = start; index < end; index++)
        {
            var sample = samples[(int)(index * ratio)];
            sum += sample * sample;
        }

        return Math.Sqrt(sum / (end - start));
    }

    /// <summary>
    /// Generates 64-bit hash from feature vector.
    /// Uses median comparison (similar to pHash algorithm).
    /// </summary>
    private static ulong GenerateHash(ReadOnlySpan<double> features)
    {
        if (features.Length == 0) return 0;

        // Compute median feature value
        Span<double> sorted = stackalloc double[HashBits];
        features.CopyTo(sorted);
        sorted[..features.Length].Sort();
        var median = sorted[features.Length / 2];

        // Generate hash bits: 1 if feature > median, 0 otherwise
        ulong hash = 0;
        for (int i = 0; i < Math.Min(features.Length, HashBits); i++)
        {
            if (features[i] > median)
            {
                hash |= 1UL << i;
            }
        }

        return hash;
    }

    /// <summary>
    /// Computes audio hash using specified algorithm.
    /// </summary>
    private ulong ComputeAudioHashNumeric(float[] samples, int sampleRate, PerceptualHashAlgorithm algorithm)
    {
        return algorithm switch
        {
            PerceptualHashAlgorithm.Chromaprint => ComputeChromaPrint(samples, sampleRate),
            PerceptualHashAlgorithm.Spectral => ComputeHash(samples, sampleRate), // Use existing implementation
            _ => ComputeHash(samples, sampleRate) // Default to spectral
        };
    }

    /// <summary>
    /// Computes image hash using specified algorithm.
    /// </summary>
    private ulong ComputeImageHashNumeric(byte[] pixels, int width, int height, PerceptualHashAlgorithm algorithm)
    {
        return algorithm switch
        {
            PerceptualHashAlgorithm.PHash => ComputePHash(pixels, width, height),
            _ => ComputeSimpleImageHash(pixels, width, height) // Fallback
        };
    }

    /// <summary>
    /// Computes Chromaprint-style audio fingerprint using FFT-based chroma features.
    /// Downsample to ~11 kHz, frame with Hann window, FFT, map to 24-bin chroma
    /// (tone-aware, distinguishes e.g. 440 vs 880 Hz), reduce to 8 super-bands,
    /// take 8 evenly spaced frames, flatten to 64 values, median-threshold to 64-bit hash.
    /// </summary>
    private ulong ComputeChromaPrint(float[] samples, int sampleRate)
    {
        if (samples == null || samples.Length == 0)
            return 0;

        const int hopSize = 2048;
        const int superBands = 8;
        const int framesForHash = 8;

        if (sampleRate > ChromaTargetSampleRate)
        {
            samples = Downsample(samples, sampleRate, ChromaTargetSampleRate);
            sampleRate = ChromaTargetSampleRate;
        }

        var numFrames = (samples.Length - ChromaFftSize) / hopSize + 1;
        if (numFrames <= 0)
            return 0;

        var chromaPerFftBin = sampleRate == ChromaTargetSampleRate
            ? TargetRateChromaPerFftBin
            : CreateChromaPerFftBin(sampleRate);

        Span<double> hashValues = stackalloc double[framesForHash * superBands];
        var complexFrame = new Complex[ChromaFftSize];
        Span<double> chromaVec = stackalloc double[ChromaBins];

        for (int i = 0; i < framesForHash; i++)
        {
            int frameIdx = numFrames >= 2 ? (i * (numFrames - 1)) / (framesForHash - 1) : 0;
            if (frameIdx >= numFrames)
                frameIdx = numFrames - 1;

            int start = frameIdx * hopSize;
            for (int j = 0; j < ChromaFftSize; j++)
            {
                var s = (start + j) < samples.Length ? (double)samples[start + j] * ChromaHannWindow[j] : 0.0;
                complexFrame[j] = new Complex(s, 0);
            }

            Fourier.Forward(complexFrame);

            chromaVec.Clear();
            for (int k = 1; k < ChromaFftSize / 2; k++)
            {
                var c = chromaPerFftBin[k];
                if (c >= 0)
                    chromaVec[c] += complexFrame[k].Magnitude;
            }

            for (int sb = 0; sb < superBands; sb++)
                hashValues[(i * superBands) + sb] = chromaVec[sb * 3] + chromaVec[(sb * 3) + 1] + chromaVec[(sb * 3) + 2];
        }

        return GenerateHash(hashValues);
    }

    private static double[] CreateChromaHannWindow()
    {
        var hann = new double[ChromaFftSize];
        for (var index = 0; index < ChromaFftSize; index++)
        {
            hann[index] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * index / (ChromaFftSize - 1)));
        }

        return hann;
    }

    private static int[] CreateChromaPerFftBin(int sampleRate)
    {
        var binFrequency = (double)sampleRate / ChromaFftSize;
        var chromaPerFftBin = new int[(ChromaFftSize / 2) + 1];

        for (var index = 0; index <= ChromaFftSize / 2; index++)
        {
            var frequency = index * binFrequency;
            if (frequency < ChromaReferenceFrequency)
            {
                chromaPerFftBin[index] = -1;
                continue;
            }

            var chroma = (int)Math.Round(12.0 * Math.Log(frequency / ChromaReferenceFrequency, 2)) % ChromaBins;
            chromaPerFftBin[index] = (chroma + ChromaBins) % ChromaBins;
        }

        return chromaPerFftBin;
    }

    /// <summary>
    /// Computes pHash-style perceptual hash for images.
    /// Simplified implementation of the pHash algorithm.
    /// </summary>
    private ulong ComputePHash(byte[] pixels, int width, int height)
    {
        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
            return 0;

        if ((long)width * height > pixels.Length / 4)
        {
            return ComputePHashWithIntermediateBuffers(pixels, width, height);
        }

        const int downsampledWidth = 8;
        const int lowFrequencyCount = 32;
        Span<double> lowFrequency = stackalloc double[lowFrequencyCount];

        for (var index = 0; index < lowFrequency.Length; index++)
        {
            var x = index % downsampledWidth;
            var y = index / downsampledWidth;
            var sourceX = (int)(x * (double)width / downsampledWidth);
            var sourceY = (int)(y * (double)height / downsampledWidth);
            var pixelIndex = ((sourceY * width) + sourceX) * 4;
            var luminance = ((0.299 * pixels[pixelIndex])
                + (0.587 * pixels[pixelIndex + 1])
                + (0.114 * pixels[pixelIndex + 2])) / 255.0;

            lowFrequency[index] = luminance * (index % 2 == 0 ? 1.0 : -1.0);
        }

        lowFrequency.Sort();
        var median = lowFrequency[lowFrequency.Length / 2];
        ulong hash = 0;

        for (var index = 0; index < lowFrequency.Length; index++)
        {
            if (lowFrequency[index] > median)
            {
                hash |= 1UL << index;
            }
        }

        return hash;
    }

    private static ulong ComputePHashWithIntermediateBuffers(byte[] pixels, int width, int height)
    {
        var grayPixels = ConvertToGrayscale(pixels, width, height);
        var smallImage = DownsampleImage(grayPixels, width, height, 8, 8);
        var dct = ComputeDCT(smallImage);
        return GenerateHashFromDCT(dct);
    }

    /// <summary>
    /// Simple fallback image hash.
    /// </summary>
    private ulong ComputeSimpleImageHash(byte[] pixels, int width, int height)
    {
        if (pixels == null || pixels.Length == 0)
            return 0;

        // Simple hash based on pixel averages
        ulong hash = 0;
        var step = Math.Max(1, pixels.Length / HashBits);

        for (int i = 0; i < HashBits && i * step < pixels.Length; i++)
        {
            // Simple threshold.
            if (pixels[i * step] > 128)
            {
                hash |= 1UL << i;
            }
        }

        return hash;
    }

    /// <summary>
    /// Converts RGBA pixels to grayscale.
    /// </summary>
    private static double[] ConvertToGrayscale(byte[] pixels, int width, int height)
    {
        var grayscale = new double[width * height];
        for (int i = 0; i < width * height; i++)
        {
            var r = pixels[i * 4];
            var g = pixels[i * 4 + 1];
            var b = pixels[i * 4 + 2];

            // Standard luminance formula
            grayscale[i] = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
        }

        return grayscale;
    }

    /// <summary>
    /// Downsamples image to target dimensions.
    /// </summary>
    private static double[] DownsampleImage(double[] pixels, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        var result = new double[dstWidth * dstHeight];

        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                var srcX = (int)(x * (double)srcWidth / dstWidth);
                var srcY = (int)(y * (double)srcHeight / dstHeight);
                var srcIndex = srcY * srcWidth + srcX;
                result[y * dstWidth + x] = pixels[Math.Min(srcIndex, pixels.Length - 1)];
            }
        }

        return result;
    }

    /// <summary>
    /// Computes simplified 2D DCT.
    /// </summary>
    private static double[] ComputeDCT(double[] pixels)
    {
        // Very simplified DCT computation (real pHash uses proper 2D DCT)
        var size = (int)Math.Sqrt(pixels.Length);
        var dct = new double[pixels.Length];

        // Simple frequency analysis
        for (int i = 0; i < pixels.Length; i++)
        {
            dct[i] = pixels[i] * (i % 2 == 0 ? 1.0 : -1.0); // Alternating pattern
        }

        return dct;
    }

    /// <summary>
    /// Generates hash from DCT coefficients.
    /// </summary>
    private static ulong GenerateHashFromDCT(double[] dct)
    {
        if (dct.Length == 0) return 0;

        // Compute median of low-frequency components
        var lowFreq = dct.Take(32).ToArray(); // Use first 32 coefficients
        Array.Sort(lowFreq);
        var median = lowFreq[lowFreq.Length / 2];

        ulong hash = 0;
        for (int i = 0; i < Math.Min(lowFreq.Length, HashBits); i++)
        {
            if (lowFreq[i] > median)
            {
                hash |= 1UL << i;
            }
        }

        return hash;
    }

}

/// <summary>
/// Audio utility for extracting PCM samples from various formats via ffmpeg.
/// </summary>
public static class AudioUtilities
{
    /// <summary>
    /// Extracts PCM samples from an audio file using ffmpeg. Supports MP3, FLAC, OGG, WAV, M4A, etc.
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file.</param>
    /// <param name="ffmpegPath">Path to ffmpeg executable; if null or empty, uses "ffmpeg" from PATH.</param>
    /// <param name="sampleRate">Output sample rate in Hz (default 22050).</param>
    /// <param name="channels">Output channels, 1=mono (default), 2=stereo.</param>
    /// <param name="maxDurationSeconds">Maximum duration to decode in seconds (default 300).</param>
    /// <returns>PCM samples (normalized -1.0 to 1.0) and sample rate.</returns>
    public static (float[] Samples, int SampleRate) ExtractPcmSamples(
        string audioFilePath,
        string? ffmpegPath = null,
        int sampleRate = 22050,
        int channels = 1,
        int maxDurationSeconds = 300)
    {
        return ExtractPcmSamplesAsync(audioFilePath, default, ffmpegPath, sampleRate, channels, maxDurationSeconds)
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Extracts PCM samples from an audio file using ffmpeg asynchronously.
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="ffmpegPath">Path to ffmpeg; if null or empty, uses "ffmpeg".</param>
    /// <param name="sampleRate">Output sample rate in Hz (default 22050).</param>
    /// <param name="channels">Output channels, 1=mono (default).</param>
    /// <param name="maxDurationSeconds">Max duration to decode (default 300).</param>
    /// <returns>PCM samples (normalized -1.0 to 1.0) and sample rate.</returns>
    public static async Task<(float[] Samples, int SampleRate)> ExtractPcmSamplesAsync(
        string audioFilePath,
        CancellationToken cancellationToken = default,
        string? ffmpegPath = null,
        int sampleRate = 22050,
        int channels = 1,
        int maxDurationSeconds = 300)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath))
            throw new ArgumentException("Audio file path must be supplied.", nameof(audioFilePath));
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found.", audioFilePath);
        var exe = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-nostdin");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-t");
        psi.ArgumentList.Add(maxDurationSeconds.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(audioFilePath);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("s16le");
        psi.ArgumentList.Add("-ar");
        psi.ArgumentList.Add(sampleRate.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-ac");
        psi.ArgumentList.Add(channels.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("pipe:1");

        using var process = new Process { StartInfo = psi };
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        process.Start();
        await using var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"ffmpeg exited with code {process.ExitCode} while decoding {audioFilePath}. stderr: {stderr.Trim()}");
        }

        var bytes = ms.ToArray();
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"ffmpeg produced no PCM output for {audioFilePath}. ffmpeg stderr: {stderr}");
        }

        if (bytes.Length % sizeof(short) != 0)
        {
            var truncated = bytes.Length - (bytes.Length % sizeof(short));
            Array.Resize(ref bytes, truncated);
        }

        var sampleCount = bytes.Length / sizeof(short);
        var shorts = new short[sampleCount];
        Buffer.BlockCopy(bytes, 0, shorts, 0, bytes.Length);

        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = shorts[i] / 32768f;

        return (samples, sampleRate);
    }

    /// <summary>
    /// Converts stereo to mono by averaging channels.
    /// </summary>
    public static float[] StereoToMono(float[] stereoSamples)
    {
        var monoSamples = new float[stereoSamples.Length / 2];
        for (int i = 0; i < monoSamples.Length; i++)
        {
            monoSamples[i] = (stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2.0f;
        }

        return monoSamples;
    }
}
