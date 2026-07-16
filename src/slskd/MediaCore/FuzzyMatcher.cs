// <copyright file="FuzzyMatcher.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.MediaCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Local fuzzy matcher for advisory matching (not published to DHT).
/// Supports multiple algorithms: Jaccard, Levenshtein, Phonetic, and Perceptual hash matching.
/// </summary>
public interface IFuzzyMatcher
{
    double Score(string title, string artist, string candidateTitle, string candidateArtist);
    double ScoreLevenshtein(string a, string b);
    double ScorePhonetic(string a, string b);

    /// <summary>
    /// Computes cross-codec fuzzy match score using perceptual hashes.
    /// </summary>
    /// <param name="contentIdA">First ContentID with perceptual hash</param>
    /// <param name="contentIdB">Second ContentID with perceptual hash</param>
    /// <param name="registry">ContentID registry for lookup</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Match confidence score (0.0 to 1.0)</returns>
    Task<double> ScorePerceptualAsync(string contentIdA, string contentIdB, IContentIdRegistry registry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar content using combined fuzzy matching algorithms.
    /// </summary>
    /// <param name="targetContentId">ContentID to find matches for</param>
    /// <param name="candidates">Candidate ContentIDs to compare against</param>
    /// <param name="registry">ContentID registry</param>
    /// <param name="minConfidence">Minimum confidence threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matches with confidence scores</returns>
    Task<IReadOnlyList<FuzzyMatchResult>> FindSimilarContentAsync(
        string targetContentId,
        IEnumerable<string> candidates,
        IContentIdRegistry registry,
        double minConfidence = 0.7,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a fuzzy content match.
/// </summary>
public record FuzzyMatchResult(
    string TargetContentId,
    string CandidateContentId,
    double Confidence,
    FuzzyMatchReason Reason);

/// <summary>
/// Reasons for fuzzy match confidence.
/// </summary>
public enum FuzzyMatchReason
{
    PerceptualHash,
    TextSimilarity,
    Combined
}

public class FuzzyMatcher : IFuzzyMatcher
{
    private readonly IPerceptualHasher _perceptualHasher;
    private readonly IDescriptorRetriever _descriptorRetriever;
    private readonly ILogger<FuzzyMatcher> _logger;

    public FuzzyMatcher(
        IPerceptualHasher perceptualHasher,
        IDescriptorRetriever descriptorRetriever,
        ILogger<FuzzyMatcher> logger)
    {
        _perceptualHasher = perceptualHasher;
        _descriptorRetriever = descriptorRetriever;
        _logger = logger;
    }

    public double Score(string title, string artist, string candidateTitle, string candidateArtist)
    {
        // Jaccard similarity: simple case-insensitive token overlap
        // Fast and effective for basic matching
        var t = Tokenize($"{title} {artist}");
        var c = Tokenize($"{candidateTitle} {candidateArtist}");
        if (t.Count == 0 || c.Count == 0) return 0;

        var smaller = t.Count <= c.Count ? t : c;
        var larger = ReferenceEquals(smaller, t) ? c : t;
        var intersectionCount = 0;
        foreach (var token in smaller)
        {
            if (larger.Contains(token))
            {
                intersectionCount++;
            }
        }

        var unionCount = t.Count + c.Count - intersectionCount;
        return (double)intersectionCount / unionCount;
    }

    /// <summary>
    /// Levenshtein distance-based similarity score (0.0 to 1.0).
    /// Higher scores indicate more similar strings.
    /// Uses normalized edit distance for comparison.
    /// </summary>
    public double ScoreLevenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return 1.0;

        // Normalize to lowercase for case-insensitive comparison
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        var distance = ComputeLevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        // Convert distance to similarity score (0.0 to 1.0)
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Phonetic similarity using Soundex algorithm.
    /// Returns 1.0 for exact phonetic match, 0.0 for no match.
    /// Useful for matching artist/album names with typos or phonetic variations.
    /// </summary>
    public double ScorePhonetic(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

        var soundexA = Soundex(a);
        var soundexB = Soundex(b);

        // Exact phonetic match
        if (soundexA == soundexB) return 1.0;

        // Partial match: first letter matches (common root sound)
        if (soundexA[0] == soundexB[0]) return 0.5;

        return 0.0;
    }

    /// <summary>
    /// Computes Levenshtein edit distance between two strings.
    /// Measures minimum number of single-character edits (insertions, deletions, substitutions).
    /// </summary>
    private static int ComputeLevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var sharedPrefixLength = 0;
        var shorterLength = Math.Min(a.Length, b.Length);
        while (sharedPrefixLength < shorterLength && a[sharedPrefixLength] == b[sharedPrefixLength])
        {
            sharedPrefixLength++;
        }

        a = a[sharedPrefixLength..];
        b = b[sharedPrefixLength..];

        var sharedSuffixLength = 0;
        shorterLength = Math.Min(a.Length, b.Length);
        while (sharedSuffixLength < shorterLength &&
            a[^(sharedSuffixLength + 1)] == b[^(sharedSuffixLength + 1)])
        {
            sharedSuffixLength++;
        }

        if (sharedSuffixLength > 0)
        {
            a = a[..^sharedSuffixLength];
            b = b[..^sharedSuffixLength];
        }

        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        if (b.Length > a.Length)
        {
            var temporary = a;
            a = b;
            b = temporary;
        }

        var previousRow = new int[b.Length + 1];
        var currentRow = new int[b.Length + 1];

        for (var column = 0; column <= b.Length; column++)
        {
            previousRow[column] = column;
        }

        for (var row = 1; row <= a.Length; row++)
        {
            currentRow[0] = row;

            for (var column = 1; column <= b.Length; column++)
            {
                var substitutionCost = a[row - 1] == b[column - 1] ? 0 : 1;
                currentRow[column] = Math.Min(
                    Math.Min(
                        previousRow[column] + 1,
                        currentRow[column - 1] + 1),
                    previousRow[column - 1] + substitutionCost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[b.Length];
    }

    /// <summary>
    /// Computes Soundex phonetic code for a string (American English).
    /// Returns 4-character code representing phonetic sound.
    /// </summary>
    private static string Soundex(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "0000";

        s = s.ToUpperInvariant();

        // Remove non-alphabetic characters
        s = new string(s.Where(char.IsLetter).ToArray());
        if (s.Length == 0) return "0000";

        var result = new char[4];
        result[0] = s[0]; // Keep first letter

        // Soundex digit mapping
        var prevCode = GetSoundexCode(s[0]);
        int index = 1;

        for (int i = 1; i < s.Length && index < 4; i++)
        {
            var code = GetSoundexCode(s[i]);

            // Skip vowels and duplicates
            if (code != '0' && code != prevCode)
            {
                result[index++] = code;
            }

            prevCode = code;
        }

        // Pad with zeros
        while (index < 4)
        {
            result[index++] = '0';
        }

        return new string(result);
    }

    /// <summary>
    /// Maps a letter to its Soundex phonetic code.
    /// </summary>
    private static char GetSoundexCode(char c)
    {
        return c switch
        {
            'B' or 'F' or 'P' or 'V' => '1',
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
            'D' or 'T' => '3',
            'L' => '4',
            'M' or 'N' => '5',
            'R' => '6',
            _ => '0', // Vowels (A, E, I, O, U), H, W, Y
        };
    }

    private static HashSet<string> Tokenize(string value)
    {
        var normalized = value.ToLowerInvariant();
        var tokens = new HashSet<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            while (start < normalized.Length && normalized[start] == ' ')
            {
                start++;
            }

            var separator = start;
            while (separator < normalized.Length && normalized[separator] != ' ')
            {
                separator++;
            }

            var tokenStart = start;
            var tokenEnd = separator;
            while (tokenStart < tokenEnd && IsTokenBoundaryPunctuation(normalized[tokenStart]))
            {
                tokenStart++;
            }

            while (tokenEnd > tokenStart && IsTokenBoundaryPunctuation(normalized[tokenEnd - 1]))
            {
                tokenEnd--;
            }

            if (tokenStart < tokenEnd)
            {
                tokens.Add(normalized[tokenStart..tokenEnd]);
            }

            start = separator + 1;
        }

        return tokens;
    }

    private static bool IsTokenBoundaryPunctuation(char value)
    {
        return value is '\"' or '\'' or ',' or '.' or '(' or ')' or '[' or ']';
    }

    /// <inheritdoc/>
    public Task<double> ScorePerceptualAsync(string contentIdA, string contentIdB, IContentIdRegistry registry, CancellationToken cancellationToken = default)
    {
        return ScorePerceptualAsync(contentIdA, contentIdB, descriptorCache: null, cancellationToken);
    }

    private async Task<double> ScorePerceptualAsync(
        string contentIdA,
        string contentIdB,
        Dictionary<string, DescriptorRetrievalResult>? descriptorCache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentIdA) || string.IsNullOrWhiteSpace(contentIdB))
            return 0.0;

        try
        {
            var parsedA = ContentIdParser.Parse(contentIdA);
            var parsedB = ContentIdParser.Parse(contentIdB);

            if (parsedA == null || parsedB == null)
                return 0.0;

            if (!string.Equals(
                ContentIdParser.NormalizeDomain(parsedA.Domain, parsedA.Type),
                ContentIdParser.NormalizeDomain(parsedB.Domain, parsedB.Type),
                StringComparison.OrdinalIgnoreCase))
                return 0.0;

            var resultA = await RetrieveDescriptorAsync(contentIdA, descriptorCache, cancellationToken);
            var resultB = await RetrieveDescriptorAsync(contentIdB, descriptorCache, cancellationToken);

            if (!resultA.Found || !resultB.Found || resultA.Descriptor == null || resultB.Descriptor == null)
                return 0.0;

            var hashA = GetBestNumericHash(resultA.Descriptor);
            var hashB = GetBestNumericHash(resultB.Descriptor);

            if (hashA.HasValue && hashB.HasValue)
                return _perceptualHasher.Similarity(hashA.Value, hashB.Value);

            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FuzzyMatcher] Error computing perceptual similarity between {ContentIdA} and {ContentIdB}", contentIdA, contentIdB);
            return 0.0;
        }
    }

    private async Task<DescriptorRetrievalResult> RetrieveDescriptorAsync(
        string contentId,
        Dictionary<string, DescriptorRetrievalResult>? descriptorCache,
        CancellationToken cancellationToken)
    {
        if (descriptorCache != null && descriptorCache.TryGetValue(contentId, out var cached))
        {
            return cached;
        }

        var result = await _descriptorRetriever.RetrieveAsync(contentId, bypassCache: false, cancellationToken);
        if (descriptorCache != null && result.Found && result.Descriptor != null)
        {
            descriptorCache[contentId] = result;
        }

        return result;
    }

    /// <summary>
    /// Picks the best perceptual hash with NumericHash: Chromaprint preferred, else first available.
    /// </summary>
    private static ulong? GetBestNumericHash(ContentDescriptor? descriptor)
    {
        if (descriptor?.PerceptualHashes == null || descriptor.PerceptualHashes.Count == 0)
            return null;

        var chroma = descriptor.PerceptualHashes.FirstOrDefault(h => h.Algorithm == "Chromaprint" && h.NumericHash.HasValue);
        if (chroma?.NumericHash != null)
            return chroma.NumericHash.Value;

        var first = descriptor.PerceptualHashes.FirstOrDefault(h => h.NumericHash.HasValue);
        return first?.NumericHash;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FuzzyMatchResult>> FindSimilarContentAsync(
        string targetContentId,
        IEnumerable<string> candidates,
        IContentIdRegistry registry,
        double minConfidence = 0.7,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FuzzyMatchResult>();
        var descriptorCache = new Dictionary<string, DescriptorRetrievalResult>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Compute perceptual similarity
            var perceptualScore = await ScorePerceptualAsync(
                targetContentId,
                candidate,
                descriptorCache,
                cancellationToken);

            // Compute text similarity (if applicable)
            var textScore = ComputeTextSimilarity(targetContentId, candidate);

            // Combine scores with weights
            var combinedScore = CombineSimilarityScores(perceptualScore, textScore);

            if (combinedScore >= minConfidence)
            {
                var reason = perceptualScore > textScore ? FuzzyMatchReason.PerceptualHash :
                           textScore > perceptualScore ? FuzzyMatchReason.TextSimilarity :
                           FuzzyMatchReason.Combined;

                results.Add(new FuzzyMatchResult(
                    TargetContentId: targetContentId,
                    CandidateContentId: candidate,
                    Confidence: combinedScore,
                    Reason: reason));
            }
        }

        // Sort by confidence descending
        return results.OrderByDescending(r => r.Confidence).ToArray();
    }

    /// <summary>
    /// Computes text-based similarity between ContentIDs.
    /// </summary>
    private double ComputeTextSimilarity(string contentIdA, string contentIdB)
    {
        // Extract identifiers from ContentIDs for text comparison
        var idA = contentIdA.Split(':').LastOrDefault() ?? contentIdA;
        var idB = contentIdB.Split(':').LastOrDefault() ?? contentIdB;

        // Use Levenshtein distance for string similarity
        return ScoreLevenshtein(idA, idB);
    }

    /// <summary>
    /// Combines perceptual and text similarity scores.
    /// </summary>
    private static double CombineSimilarityScores(double perceptualScore, double textScore)
    {
        // Weight perceptual similarity higher for same-domain content
        const double perceptualWeight = 0.7;
        const double textWeight = 0.3;

        return (perceptualScore * perceptualWeight) + (textScore * textWeight);
    }
}
