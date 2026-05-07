// <copyright file="OpinionModels.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Opinions;

using System;
using System.Collections.Generic;

public enum OpinionSubjectType
{
    Unknown = 0,
    User,
    File,
    ContentHash,
    Artist,
    Album,
    Track,
    Pod,
    Source,
    MeshPeer,
    SearchTerm,
    Other,
}

public enum OpinionKind
{
    Unknown = 0,
    Like,
    Hate,
    Trust,
    Distrust,
    Block,
    Recommend,
    Quarantine,
    VerifiedGood,
    VerifiedBad,
}

public sealed class OpinionEvidence
{
    public string Type { get; set; } = "note";

    public string Value { get; set; } = string.Empty;
}

public sealed class OpinionRecord
{
    public string Id { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public OpinionSubjectType SubjectType { get; set; } = OpinionSubjectType.Unknown;

    public string SubjectId { get; set; } = string.Empty;

    public OpinionKind Kind { get; set; } = OpinionKind.Unknown;

    public double Strength { get; set; }

    public double Confidence { get; set; } = 1.0;

    public string Scope { get; set; } = "global";

    public string Source { get; set; } = "local";

    public string Reason { get; set; } = string.Empty;

    public List<OpinionEvidence> Evidence { get; set; } = new();

    public long CreatedUnixMs { get; set; }

    public long UpdatedUnixMs { get; set; }

    public long? ExpiresUnixMs { get; set; }

    public string PayloadHash { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;
}

public sealed class OpinionQuery
{
    public string? Issuer { get; init; }

    public OpinionSubjectType? SubjectType { get; init; }

    public string? SubjectId { get; init; }

    public OpinionKind? Kind { get; init; }

    public string? Scope { get; init; }

    public string? Source { get; init; }

    public bool IncludeExpired { get; init; }

    public int Limit { get; init; } = 100;
}

public sealed class OpinionSummary
{
    public OpinionSubjectType SubjectType { get; init; }

    public string SubjectId { get; init; } = string.Empty;

    public string Scope { get; init; } = "global";

    public int Total { get; init; }

    public int Positive { get; init; }

    public int Negative { get; init; }

    public double WeightedScore { get; init; }

    public double Confidence { get; init; }

    public IReadOnlyList<OpinionRecord> Opinions { get; init; } = Array.Empty<OpinionRecord>();
}

public sealed class OpinionValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = new();
}
