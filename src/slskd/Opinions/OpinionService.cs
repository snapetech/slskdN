// <copyright file="OpinionService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Opinions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Common.IO;
using Soulseek;

public sealed class OpinionService : IOpinionService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly ILogger<OpinionService> logger;
    private readonly string storagePath;
    private readonly SemaphoreSlim syncRoot = new(1, 1);
    private readonly ConcurrentDictionary<string, OpinionRecord> opinions = new(StringComparer.OrdinalIgnoreCase);

    public OpinionService(ILogger<OpinionService> logger, string storagePath)
    {
        this.logger = logger;
        this.storagePath = storagePath;
        Load();
    }

    public async Task<OpinionRecord> SubmitAsync(OpinionRecord opinion, CancellationToken cancellationToken = default)
    {
        opinion = Normalize(opinion);
        var validation = Validate(opinion);
        if (!validation.IsValid)
        {
            throw new ArgumentException(string.Join("; ", validation.Errors), nameof(opinion));
        }

        await syncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            opinions[opinion.Id] = Clone(opinion);
            Persist();
            return Clone(opinion);
        }
        finally
        {
            syncRoot.Release();
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        id = id?.Trim() ?? string.Empty;
        if (id.Length == 0)
        {
            return false;
        }

        await syncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var removed = opinions.TryRemove(id, out _);
            if (removed)
            {
                Persist();
            }

            return removed;
        }
        finally
        {
            syncRoot.Release();
        }
    }

    public Task<IReadOnlyList<OpinionRecord>> ListAsync(OpinionQuery query, CancellationToken cancellationToken = default)
    {
        query ??= new OpinionQuery();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Task.FromResult<IReadOnlyList<OpinionRecord>>(BuildOpinionList(opinions.Values, query, now));
    }

    internal static List<OpinionRecord> BuildOpinionList(
        IEnumerable<OpinionRecord> opinions,
        OpinionQuery query,
        long now)
    {
        var limit = Math.Clamp(query.Limit, 1, 1000);
        var issuer = string.IsNullOrWhiteSpace(query.Issuer) ? null : query.Issuer.Trim();
        var subjectId = string.IsNullOrWhiteSpace(query.SubjectId) ? null : NormalizeSubjectId(query.SubjectId);
        var scope = string.IsNullOrWhiteSpace(query.Scope) ? null : NormalizeScope(query.Scope);
        var source = string.IsNullOrWhiteSpace(query.Source) ? null : query.Source.Trim();
        PriorityQueue<OpinionCandidate, OpinionCandidate>? newest = null;
        var sequence = 0;

        foreach (var opinion in opinions)
        {
            var currentSequence = sequence++;
            if ((!query.IncludeExpired && opinion.ExpiresUnixMs.HasValue && opinion.ExpiresUnixMs.Value <= now) ||
                (issuer != null && !string.Equals(opinion.Issuer, issuer, StringComparison.OrdinalIgnoreCase)) ||
                (query.SubjectType.HasValue && opinion.SubjectType != query.SubjectType.Value) ||
                (subjectId != null && !string.Equals(opinion.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase)) ||
                (query.Kind.HasValue && opinion.Kind != query.Kind.Value) ||
                (scope != null && !string.Equals(opinion.Scope, scope, StringComparison.OrdinalIgnoreCase)) ||
                (source != null && !string.Equals(opinion.Source, source, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            newest ??= new PriorityQueue<OpinionCandidate, OpinionCandidate>(OpinionCandidateWorstFirstComparer.Instance);
            var candidate = new OpinionCandidate(opinion, currentSequence);
            if (newest.Count < limit)
            {
                newest.Enqueue(candidate, candidate);
            }
            else if (OpinionCandidateWorstFirstComparer.Instance.Compare(candidate, newest.Peek()) > 0)
            {
                newest.Dequeue();
                newest.Enqueue(candidate, candidate);
            }
        }

        if (newest == null)
        {
            return new List<OpinionRecord>();
        }

        var result = new List<OpinionRecord>(newest.Count);
        while (newest.Count > 0)
        {
            result.Add(Clone(newest.Dequeue().Opinion));
        }

        result.Reverse();
        return result;
    }

    public async Task<OpinionSummary> SummarizeAsync(
        OpinionSubjectType subjectType,
        string subjectId,
        string scope = "global",
        CancellationToken cancellationToken = default)
    {
        var records = await ListAsync(new OpinionQuery
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            Scope = scope,
            Limit = 1000,
        }, cancellationToken).ConfigureAwait(false);

        var weighted = records.Sum(opinion => Polarity(opinion.Kind) * Math.Abs(opinion.Strength) * opinion.Confidence);
        var confidence = records.Count == 0 ? 0 : records.Average(opinion => opinion.Confidence);

        return new OpinionSummary
        {
            SubjectType = subjectType,
            SubjectId = NormalizeSubjectId(subjectId),
            Scope = NormalizeScope(scope),
            Total = records.Count,
            Positive = records.Count(opinion => Polarity(opinion.Kind) > 0),
            Negative = records.Count(opinion => Polarity(opinion.Kind) < 0),
            WeightedScore = Math.Clamp(weighted, -records.Count, records.Count),
            Confidence = confidence,
            Opinions = records,
        };
    }

    public async Task<IReadOnlyList<OpinionRecord>> ImportSoulseekInterestsAsync(
        string username,
        UserInterests interests,
        CancellationToken cancellationToken = default)
    {
        username = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("username is required", nameof(username));
        }

        var issuer = $"soulseek:{username}";
        var imported = new List<OpinionRecord>();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await syncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var staleIds = opinions.Values
                .Where(opinion => string.Equals(opinion.Issuer, issuer, StringComparison.OrdinalIgnoreCase))
                .Where(opinion => string.Equals(opinion.Source, "soulseek-interest", StringComparison.OrdinalIgnoreCase))
                .Select(opinion => opinion.Id)
                .ToList();

            foreach (var id in staleIds)
            {
                opinions.TryRemove(id, out _);
            }

            foreach (var item in interests.Liked ?? Array.Empty<string>())
            {
                AddImported(username, issuer, item, OpinionKind.Like, now, imported);
            }

            foreach (var item in interests.Hated ?? Array.Empty<string>())
            {
                AddImported(username, issuer, item, OpinionKind.Hate, now, imported);
            }

            Persist();
        }
        finally
        {
            syncRoot.Release();
        }

        return imported.Select(Clone).ToList();
    }

    public OpinionValidationResult Validate(OpinionRecord opinion)
    {
        var result = new OpinionValidationResult();
        if (opinion == null)
        {
            result.Errors.Add("opinion is required");
            return result;
        }

        if (string.IsNullOrWhiteSpace(opinion.Issuer))
        {
            result.Errors.Add("issuer is required");
        }

        if (!Enum.IsDefined(opinion.SubjectType) || opinion.SubjectType == OpinionSubjectType.Unknown)
        {
            result.Errors.Add("subject type is required");
        }

        if (string.IsNullOrWhiteSpace(opinion.SubjectId))
        {
            result.Errors.Add("subject id is required");
        }

        if (!Enum.IsDefined(opinion.Kind) || opinion.Kind == OpinionKind.Unknown)
        {
            result.Errors.Add("opinion kind is required");
        }

        if (opinion.Strength < -1.0 || opinion.Strength > 1.0)
        {
            result.Errors.Add("strength must be between -1.0 and 1.0");
        }

        if (opinion.Confidence < 0.0 || opinion.Confidence > 1.0)
        {
            result.Errors.Add("confidence must be between 0.0 and 1.0");
        }

        return result;
    }

    private void AddImported(string username, string issuer, string item, OpinionKind kind, long now, List<OpinionRecord> imported)
    {
        item = item?.Trim() ?? string.Empty;
        if (item.Length == 0)
        {
            return;
        }

        var subject = OpinionSubject.FromInterestItem(item);
        var record = Normalize(new OpinionRecord
        {
            Issuer = issuer,
            SubjectType = subject.Type,
            SubjectId = subject.Id,
            Kind = kind,
            Strength = kind == OpinionKind.Like ? 0.25 : -0.25,
            Confidence = 0.25,
            Scope = "soulseek-public",
            Source = "soulseek-interest",
            Reason = $"Imported from {username}'s native Soulseek interest list.",
            Evidence =
            {
                new OpinionEvidence
                {
                    Type = "soulseek-interest",
                    Value = item,
                },
            },
            CreatedUnixMs = now,
            UpdatedUnixMs = now,
        });

        opinions[record.Id] = Clone(record);
        imported.Add(record);
    }

    private OpinionRecord Normalize(OpinionRecord opinion)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        opinion.Issuer = opinion.Issuer?.Trim() ?? string.Empty;
        opinion.SubjectId = NormalizeSubjectId(opinion.SubjectId);
        opinion.Scope = NormalizeScope(opinion.Scope);
        opinion.Source = string.IsNullOrWhiteSpace(opinion.Source) ? "local" : opinion.Source.Trim();
        opinion.Reason = opinion.Reason?.Trim() ?? string.Empty;
        opinion.Strength = opinion.Strength == 0 ? DefaultStrength(opinion.Kind) : Math.Clamp(opinion.Strength, -1.0, 1.0);
        opinion.Confidence = Math.Clamp(opinion.Confidence, 0.0, 1.0);
        opinion.CreatedUnixMs = opinion.CreatedUnixMs <= 0 ? now : opinion.CreatedUnixMs;
        opinion.UpdatedUnixMs = now;
        opinion.Evidence ??= new List<OpinionEvidence>();
        opinion.PayloadHash = ComputePayloadHash(opinion);
        opinion.Id = string.IsNullOrWhiteSpace(opinion.Id) ? ComputeOpinionId(opinion) : opinion.Id.Trim();
        return opinion;
    }

    private static string NormalizeSubjectId(string? value)
        => (value ?? string.Empty).Trim();

    private static string NormalizeScope(string? scope)
        => string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();

    private static double DefaultStrength(OpinionKind kind)
        => Polarity(kind) switch
        {
            > 0 => 1.0,
            < 0 => -1.0,
            _ => 0.0,
        };

    private static int Polarity(OpinionKind kind)
        => kind switch
        {
            OpinionKind.Like or OpinionKind.Trust or OpinionKind.Recommend or OpinionKind.VerifiedGood => 1,
            OpinionKind.Hate or OpinionKind.Distrust or OpinionKind.Block or OpinionKind.Quarantine or OpinionKind.VerifiedBad => -1,
            _ => 0,
        };

    private static string ComputeOpinionId(OpinionRecord opinion)
        => "opinion:" + HashText(string.Join("|", opinion.Issuer, opinion.SubjectType, opinion.SubjectId, opinion.Kind, opinion.Scope, opinion.Source));

    private static string ComputePayloadHash(OpinionRecord opinion)
        => HashText(JsonSerializer.Serialize(new
        {
            opinion.Issuer,
            opinion.SubjectType,
            opinion.SubjectId,
            opinion.Kind,
            Strength = opinion.Strength.ToString("R", CultureInfo.InvariantCulture),
            Confidence = opinion.Confidence.ToString("R", CultureInfo.InvariantCulture),
            opinion.Scope,
            opinion.Source,
            opinion.Reason,
            opinion.Evidence,
            opinion.CreatedUnixMs,
            opinion.ExpiresUnixMs,
        }, JsonOptions));

    private static string HashText(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private void Load()
    {
        if (!System.IO.File.Exists(storagePath))
        {
            return;
        }

        try
        {
            var state = JsonSerializer.Deserialize<OpinionStoreState>(System.IO.File.ReadAllText(storagePath), JsonOptions);
            foreach (var opinion in state?.Opinions ?? new List<OpinionRecord>())
            {
                var normalized = Normalize(opinion);
                opinions[normalized.Id] = normalized;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load opinion store from {Path}", storagePath);
        }
    }

    private void Persist()
    {
        var state = new OpinionStoreState
        {
            Opinions = opinions.Values.OrderBy(opinion => opinion.Id, StringComparer.OrdinalIgnoreCase).Select(Clone).ToList(),
        };

        AtomicFileWriter.WriteAllText(storagePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    private readonly record struct OpinionCandidate(OpinionRecord Opinion, int Sequence);

    private sealed class OpinionCandidateWorstFirstComparer : IComparer<OpinionCandidate>
    {
        public static OpinionCandidateWorstFirstComparer Instance { get; } = new();

        public int Compare(OpinionCandidate left, OpinionCandidate right)
        {
            var timestampComparison = left.Opinion.UpdatedUnixMs.CompareTo(right.Opinion.UpdatedUnixMs);
            return timestampComparison != 0
                ? timestampComparison
                : right.Sequence.CompareTo(left.Sequence);
        }
    }

    private static OpinionRecord Clone(OpinionRecord opinion)
        => JsonSerializer.Deserialize<OpinionRecord>(JsonSerializer.Serialize(opinion, JsonOptions), JsonOptions)!;

    private sealed class OpinionStoreState
    {
        public List<OpinionRecord> Opinions { get; set; } = new();
    }

    public void Dispose()
    {
        syncRoot.Dispose();
    }
}
