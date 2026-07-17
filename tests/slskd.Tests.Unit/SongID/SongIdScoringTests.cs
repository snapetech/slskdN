// <copyright file="SongIdScoringTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SongID;

using slskd.Audio;
using slskd.SongID;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public sealed class SongIdScoringTests
{
    [Fact]
    public void ApplyCanonicalTrackSignals_BoostsTrackAndFlagsLosslessSupport()
    {
        var track = new SongIdTrackCandidate
        {
            RecordingId = "rec-1",
            Title = "Signal Song",
            Artist = "Signal Artist",
            IdentityScore = 0.62,
            ByzantineScore = 0.55,
            ActionScore = 0.58,
        };

        var variants = new List<AudioVariant>
        {
            new()
            {
                Codec = "FLAC",
                QualityScore = 0.94,
                SeenCount = 6,
                TranscodeSuspect = false,
            },
            new()
            {
                Codec = "MP3",
                QualityScore = 0.73,
                SeenCount = 2,
                TranscodeSuspect = false,
            },
        };

        SongIdScoring.ApplyCanonicalTrackSignals(track, variants);

        Assert.True(track.CanonicalScore > 0.6);
        Assert.Equal(2, track.CanonicalVariantCount);
        Assert.True(track.HasLosslessCanonical);
        Assert.True(track.IdentityScore > 0.62);
        Assert.True(track.ByzantineScore > 0.55);
        Assert.True(track.ActionScore > 0.58);
    }

    [Fact]
    public void ApplyRunQualityConsensus_BoostsAlbumAndArtistFromSupportedTracks()
    {
        var run = new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new()
                {
                    Artist = "Consensus Artist",
                    Title = "Track One",
                    CanonicalScore = 0.88,
                    ActionScore = 0.64,
                    IdentityScore = 0.65,
                    ByzantineScore = 0.60,
                },
                new()
                {
                    Artist = "Consensus Artist",
                    Title = "Track Two",
                    CanonicalScore = 0.74,
                    ActionScore = 0.62,
                    IdentityScore = 0.63,
                    ByzantineScore = 0.59,
                },
            },
            Albums = new List<SongIdAlbumCandidate>
            {
                new()
                {
                    Artist = "Consensus Artist",
                    Title = "Consensus Album",
                    ActionScore = 0.61,
                    IdentityScore = 0.66,
                    ByzantineScore = 0.58,
                },
            },
            Artists = new List<SongIdArtistCandidate>
            {
                new()
                {
                    Name = "Consensus Artist",
                    ActionScore = 0.57,
                    IdentityScore = 0.60,
                    ByzantineScore = 0.56,
                },
            },
        };

        SongIdScoring.ApplyRunQualityConsensus(run);

        Assert.Equal(2, run.Albums[0].CanonicalSupportCount);
        Assert.True(run.Albums[0].CanonicalScore >= 0.88);
        Assert.True(run.Albums[0].ActionScore > 0.61);
        Assert.Equal(2, run.Artists[0].CanonicalSupportCount);
        Assert.True(run.Artists[0].CanonicalScore >= 0.88);
        Assert.True(run.Artists[0].ActionScore > 0.57);
    }

    [Fact]
    public void ApplyRunQualityConsensus_PreservesFuzzySupportAndUnsupportedCandidateState()
    {
        var unsupportedAlbum = new SongIdAlbumCandidate
        {
            Artist = "Unrelated Artist",
            CanonicalSupportCount = 7,
            CanonicalScore = 0.55,
            IdentityScore = 0.40,
            ByzantineScore = 0.30,
            ActionScore = 0.20,
        };
        var run = new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new() { Artist = "Consensus Artist feat. Guest", CanonicalScore = 0.80 },
                new() { Artist = "Consensus Artist featuring Guest", CanonicalScore = 0.60 },
                new() { Artist = "Consensus Artist featuring Guest", CanonicalScore = 0.0 },
            },
            Albums = new List<SongIdAlbumCandidate>
            {
                new() { Artist = "consensus artist featuring guest" },
                unsupportedAlbum,
            },
            Artists = new List<SongIdArtistCandidate>
            {
                new() { Name = "Consensus Artist ft. Guest" },
            },
        };

        SongIdScoring.ApplyRunQualityConsensus(run);

        Assert.Equal(2, run.Albums[0].CanonicalSupportCount);
        Assert.Equal(0.80, run.Albums[0].CanonicalScore);
        Assert.Equal(7, unsupportedAlbum.CanonicalSupportCount);
        Assert.Equal(0.55, unsupportedAlbum.CanonicalScore);
        Assert.Equal(0.40, unsupportedAlbum.IdentityScore);
        Assert.Equal(0.30, unsupportedAlbum.ByzantineScore);
        Assert.Equal(0.20, unsupportedAlbum.ActionScore);
        Assert.Equal(2, run.Artists[0].CanonicalSupportCount);
        Assert.Equal(0.80, run.Artists[0].CanonicalScore);
    }

    [Fact]
    public void ApplyRunQualityConsensus_RepeatedArtistLabelsHaveBoundedAllocation()
    {
        const int trackCount = 1_000;
        const int candidateCount = 6;
        var run = new SongIdRun
        {
            Tracks = Enumerable.Range(0, trackCount)
                .Select(_ => new SongIdTrackCandidate
                {
                    Artist = "Consensus Artist feat. Guest",
                    CanonicalScore = 0.80,
                })
                .ToList(),
            Albums = Enumerable.Range(0, candidateCount)
                .Select(_ => new SongIdAlbumCandidate { Artist = "Consensus Artist featuring Guest" })
                .ToList(),
            Artists = Enumerable.Range(0, candidateCount)
                .Select(_ => new SongIdArtistCandidate { Name = "Consensus Artist featuring Guest" })
                .ToList(),
        };
        SongIdScoring.ApplyRunQualityConsensus(new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new() { Artist = "Warm Artist", CanonicalScore = 0.5 },
            },
            Albums = new List<SongIdAlbumCandidate>
            {
                new() { Artist = "Warm Artist" },
            },
            Artists = new List<SongIdArtistCandidate>
            {
                new() { Name = "Warm Artist" },
            },
        });

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        SongIdScoring.ApplyRunQualityConsensus(run);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.All(run.Albums, album =>
        {
            Assert.Equal(trackCount, album.CanonicalSupportCount);
            Assert.Equal(0.80, album.CanonicalScore);
        });
        Assert.All(run.Artists, artist =>
        {
            Assert.Equal(trackCount, artist.CanonicalSupportCount);
            Assert.Equal(0.80, artist.CanonicalScore);
        });
        Assert.True(
            allocatedBytes < 384_000,
            $"Expected quality-consensus allocation below 384 KB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ApplyCorpusReranking_ReordersTrackCandidatesByCorpusEvidence()
    {
        var run = new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new()
                {
                    CandidateId = "b",
                    RecordingId = "rec-b",
                    Artist = "Artist B",
                    Title = "Song B",
                    IdentityScore = 0.70,
                    ByzantineScore = 0.66,
                    ActionScore = 0.72,
                },
                new()
                {
                    CandidateId = "a",
                    RecordingId = "rec-a",
                    Artist = "Artist A",
                    Title = "Song A",
                    IdentityScore = 0.68,
                    ByzantineScore = 0.63,
                    ActionScore = 0.69,
                },
            },
            CorpusMatches = new List<SongIdCorpusMatch>
            {
                new()
                {
                    RecordingId = "rec-a",
                    Artist = "Artist A",
                    Title = "Song A",
                    SimilarityScore = 0.91,
                },
            },
        };

        SongIdScoring.ApplyCorpusReranking(run);

        Assert.Equal("rec-a", run.Tracks[0].RecordingId);
        Assert.True(run.Tracks[0].ActionScore > 0.69);
    }

    [Fact]
    public void ApplyCorpusReranking_PreservesDirectMatchPrecedenceAndFuzzyBoosts()
    {
        var run = new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new()
                {
                    RecordingId = "rec-direct",
                    Artist = "Exact Artist",
                    Title = "Exact Title",
                },
            },
            Albums = new List<SongIdAlbumCandidate>
            {
                new()
                {
                    Artist = "Exact Artist",
                    Title = "Different Album",
                },
            },
            Artists = new List<SongIdArtistCandidate>
            {
                new() { Name = "Exact Artist" },
            },
            CorpusMatches = new List<SongIdCorpusMatch>
            {
                new()
                {
                    RecordingId = "rec-direct",
                    Artist = "Different Artist",
                    Title = "Different Title",
                    SimilarityScore = 0.40,
                },
                new()
                {
                    RecordingId = "rec-fuzzy",
                    Artist = "Exact Artist",
                    Title = "Exact Title",
                    SimilarityScore = 0.99,
                },
            },
        };

        SongIdScoring.ApplyCorpusReranking(run);

        Assert.Equal(0.40 * 0.18, run.Tracks[0].ActionScore, precision: 10);
        Assert.Equal(0.75 * 0.14, run.Albums[0].ActionScore, precision: 10);
        Assert.Equal(0.12, run.Artists[0].ActionScore, precision: 10);
    }

    [Fact]
    public void ApplyCorpusReranking_RepeatedLabelsHaveBoundedAllocation()
    {
        const int candidateCount = 1_000;
        var run = new SongIdRun
        {
            Tracks = Enumerable.Range(0, candidateCount)
                .Select(index => new SongIdTrackCandidate
                {
                    RecordingId = $"candidate-{index}",
                    Artist = "Artist feat. Guest",
                    Title = "The Example & Song",
                })
                .ToList(),
            CorpusMatches = Enumerable.Range(0, 5)
                .Select(index => new SongIdCorpusMatch
                {
                    RecordingId = $"corpus-{index}",
                    Artist = "Artist featuring Guest",
                    Title = "The Example and Song",
                    SimilarityScore = 0.90,
                })
                .ToList(),
        };
        SongIdScoring.ApplyCorpusReranking(new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new() { RecordingId = "warm", Artist = "Warm Artist", Title = "Warm Title" },
            },
            CorpusMatches = new List<SongIdCorpusMatch>
            {
                new() { RecordingId = "other", Artist = "Warm Artist", Title = "Warm Title" },
            },
        });

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        SongIdScoring.ApplyCorpusReranking(run);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.All(run.Tracks, track => Assert.Equal(0.18, track.ActionScore, precision: 10));
        Assert.True(
            allocatedBytes < 640_000,
            $"Expected corpus reranking allocation below 640 KB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ComputeTrackSearchQualityScore_ReflectsCanonicalBoost()
    {
        var track = new SongIdTrackCandidate
        {
            CanonicalScore = 0.80,
            HasLosslessCanonical = true,
        };

        var quality = SongIdScoring.ComputeTrackSearchQualityScore(track, 0.74);

        Assert.True(quality > 0.88);
        Assert.True(quality <= 1.0);
    }

    [Fact]
    public void BuildForensicMatrix_OneStrongSyntheticLaneCapsConfidence()
    {
        var run = new SongIdRun
        {
            Scorecard = new SongIdScorecard
            {
                ClipCount = 3,
                AiArtifactClipCount = 3,
                HighAiArtifactClipCount = 3,
            },
            AiHeuristics = new SongIdAiHeuristicFinding
            {
                ArtifactScore = 0.82,
                ArtifactLabel = "high",
                PeakCount = 18,
                PeakDensity = 0.12,
                PeriodicityStrength = 0.71,
                ResidualRatio = 0.34,
            },
            Clips = new List<SongIdClipFinding>
            {
                new()
                {
                    ClipId = "clip-1",
                    AiHeuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.82,
                        ArtifactLabel = "high",
                    },
                },
            },
        };

        var matrix = SongIdScoring.BuildForensicMatrix(run);
        var synthetic = SongIdScoring.BuildSyntheticAssessment(run, matrix);

        Assert.True(matrix.ConfidenceScore <= 44);
        Assert.True(matrix.SyntheticScore <= 44);
        Assert.Contains("one_strong_synthetic_lane_is_not_enough", matrix.Notes);
        Assert.Equal("low_signal", synthetic.Verdict);
    }

    [Fact]
    public void BuildForensicMatrix_StrongIdentitySuppressesSyntheticOverclaim()
    {
        var run = new SongIdRun
        {
            Tracks = new List<SongIdTrackCandidate>
            {
                new()
                {
                    IsExact = true,
                    IdentityScore = 0.96,
                    Artist = "Known Artist",
                    Title = "Known Song",
                },
            },
            Scorecard = new SongIdScorecard
            {
                ClipCount = 4,
                AiArtifactClipCount = 4,
                HighAiArtifactClipCount = 4,
                ProvenanceSignalCount = 2,
            },
            Provenance = new SongIdProvenanceFinding
            {
                SignalCount = 2,
                Signals = new List<string> { "c2pa", "content credentials" },
                ManifestHint = true,
                Verified = true,
                ValidationState = "valid",
            },
            AiHeuristics = new SongIdAiHeuristicFinding
            {
                ArtifactScore = 0.90,
                ArtifactLabel = "high",
                PeakCount = 24,
                PeakDensity = 0.18,
                PeriodicityStrength = 0.83,
                ResidualRatio = 0.41,
            },
            Clips = new List<SongIdClipFinding>
            {
                new()
                {
                    ClipId = "clip-1",
                    SongRec = new SongIdRecognizerFinding
                    {
                        Artist = "Known Artist",
                        Title = "Known Song",
                    },
                    AiHeuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.90,
                        ArtifactLabel = "high",
                    },
                },
                new()
                {
                    ClipId = "clip-2",
                    SongRec = new SongIdRecognizerFinding
                    {
                        Artist = "Known Artist",
                        Title = "Known Song",
                    },
                    AiHeuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.89,
                        ArtifactLabel = "high",
                    },
                },
            },
        };

        var identity = SongIdScoring.BuildIdentityAssessment(run);
        var matrix = SongIdScoring.BuildForensicMatrix(run);
        var synthetic = SongIdScoring.BuildSyntheticAssessment(run, matrix);

        Assert.Equal("recognized_cataloged_track", identity.Verdict);
        Assert.True(matrix.IdentityScore >= 75);
        Assert.True(matrix.SyntheticScore <= 34);
        Assert.Contains("strong_identity_suppresses_synthetic_overclaim", matrix.Notes);
        Assert.Equal("mixed_or_inconclusive", synthetic.Verdict);
        Assert.Equal("medium", synthetic.Confidence);
    }

    [Fact]
    public void BuildForensicMatrix_UsesPerturbationProbeStabilityWhenAvailable()
    {
        var run = new SongIdRun
        {
            SourceType = "local_file",
            Scorecard = new SongIdScorecard
            {
                ClipCount = 4,
                AiArtifactClipCount = 4,
                HighAiArtifactClipCount = 2,
            },
            AiHeuristics = new SongIdAiHeuristicFinding
            {
                ArtifactScore = 0.61,
                ArtifactLabel = "medium",
                SpectralCentroid = 3560,
                SpectralFlux = 0.12,
                PitchSalience = 0.49,
                DurationSuspicion = 0.11,
            },
            Perturbations = new List<SongIdPerturbationFinding>
            {
                new()
                {
                    PerturbationId = "lowpass",
                    BaselineDelta = 0.08,
                    Heuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.58,
                        ArtifactLabel = "medium",
                    },
                },
                new()
                {
                    PerturbationId = "resample",
                    BaselineDelta = 0.09,
                    Heuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.56,
                        ArtifactLabel = "medium",
                    },
                },
                new()
                {
                    PerturbationId = "pitch_shift",
                    BaselineDelta = 0.07,
                    Heuristics = new SongIdAiHeuristicFinding
                    {
                        ArtifactScore = 0.55,
                        ArtifactLabel = "medium",
                    },
                },
            },
        };

        var matrix = SongIdScoring.BuildForensicMatrix(run);

        Assert.True(matrix.PerturbationStability > 0.6);
        Assert.Equal("clean_full_track", matrix.QualityClass);
        Assert.True(matrix.LaneScores.ContainsKey("descriptor_priors"));
    }

    [Fact]
    public void ApplyCorpusFamilyHints_ReusesFamilyLabelFromCorpusMatch()
    {
        var run = new SongIdRun
        {
            ForensicMatrix = new SongIdForensicMatrix
            {
                FamilyLabel = "none",
                KnownFamilyScore = 0,
            },
            CorpusMatches = new List<SongIdCorpusMatch>
            {
                new()
                {
                    FamilyLabel = "suno_like",
                    KnownFamilyScore = 84,
                    SimilarityScore = 0.88,
                },
            },
        };

        SongIdScoring.ApplyCorpusFamilyHints(run);

        Assert.Equal("suno_like", run.ForensicMatrix.FamilyLabel);
        Assert.Equal(84, run.ForensicMatrix.KnownFamilyScore);
        Assert.Contains("corpus_family_hint_reused", run.ForensicMatrix.Notes);
    }

    [Fact]
    public void ComputeIdentityFirstOverallScore_PrefersHigherIdentityAtComparableQuality()
    {
        var lowerIdentity = SongIdScoring.ComputeIdentityFirstOverallScore(
            identityScore: 0.52,
            qualityScore: 0.88,
            byzantineScore: 0.76,
            readinessScore: 0.79);
        var higherIdentity = SongIdScoring.ComputeIdentityFirstOverallScore(
            identityScore: 0.91,
            qualityScore: 0.82,
            byzantineScore: 0.74,
            readinessScore: 0.77);

        Assert.True(higherIdentity > lowerIdentity);
    }

    [Fact]
    public void CompareLooseText_TreatsFeatAndAmpersandVariantsAsEquivalent()
    {
        var featuringScore = SongIdScoring.CompareLooseText("Artist feat. Guest", "Artist featuring Guest");
        var andScore = SongIdScoring.CompareLooseText("Artist & Guest", "Artist and Guest");

        Assert.Equal(1, featuringScore);
        Assert.Equal(1, andScore);
    }

    [Theory]
    [InlineData("alpha alpha beta", "alpha gamma gamma", 1.0 / 3.0)]
    [InlineData("Alpha---Beta", "alpha beta", 1.0)]
    [InlineData("ÄPFEL", "pfel", 1.0)]
    [InlineData("alpha\tbeta", "alpha beta", 1.0)]
    [InlineData("Artist&feat Guest", "Artist featuring Guest", 0.75)]
    [InlineData("Artist feat&Guest", "Artist featuring Guest", 0.75)]
    [InlineData("Artist-feat Guest", "Artist feat", 2.0 / 3.0)]
    [InlineData("Artist\tfeat Guest", "Artist feat", 2.0 / 3.0)]
    [InlineData("feat. Artist", "feat Artist", 1.0)]
    [InlineData("alpha", "alpha beta", 0.5)]
    [InlineData("alpha alpha", "alpha", 1.0)]
    [InlineData("alpha", null, 0.0)]
    [InlineData("", "", 0.0)]
    public void CompareLooseText_PreservesNormalizedTokenSetSemantics(string left, string? right, double expected)
    {
        Assert.Equal(expected, SongIdScoring.CompareLooseText(left, right), precision: 10);
    }

    [Fact]
    public void NormalizeLooseText_MatchesLegacyPipelineAcrossAdversarialInputs()
    {
        var inputs = new List<string?>
        {
            null,
            string.Empty,
            "   \t\r\n",
            "Artist&feat Guest",
            "Artist feat&Guest",
            "Artist-feat Guest",
            "Artist\tfeat Guest",
            " feat. ",
            " ft. ",
            "x-feat-y",
            "ÄPFEL Kelvin İSTANBUL ſong",
            "alpha😀beta",
            "&& feat. && ft &&",
        };
        const string alphabet = "abcxyzFEATft01239 &.-_\t\r\nÄÖKſİ";
        var random = new Random(0x5eed);
        for (var inputIndex = 0; inputIndex < 2_000; inputIndex++)
        {
            var length = random.Next(0, 80);
            var characters = new char[length];
            for (var characterIndex = 0; characterIndex < length; characterIndex++)
            {
                characters[characterIndex] = alphabet[random.Next(alphabet.Length)];
            }

            inputs.Add(new string(characters));
        }

        foreach (var input in inputs)
        {
            Assert.Equal(NormalizeLooseTextLegacy(input), SongIdScoring.NormalizeLooseText(input));
        }
    }

    [Fact]
    public void CompareLooseText_LargeTokenSetsHaveBoundedAllocation()
    {
        const int tokenCount = 5_000;
        var left = string.Join(' ', Enumerable.Range(0, tokenCount).Select(index => $"token{index}"));
        var right = string.Join(' ', Enumerable.Range(tokenCount / 2, tokenCount).Select(index => $"token{index}"));
        _ = SongIdScoring.CompareLooseText("warm shared tokens", "warm different tokens");

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var score = SongIdScoring.CompareLooseText(left, right);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(1.0 / 3.0, score, precision: 10);
        Assert.True(
            allocatedBytes < 700_000,
            $"Expected loose-text token comparison below 700 KB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void CompareLooseText_RepeatedTypicalComparisonsHaveBoundedAllocation()
    {
        const int iterations = 10_000;
        const string left = "Artist feat. Guest Album Song";
        const string right = "Artist featuring Other Album Song";
        _ = SongIdScoring.CompareLooseText(left, right);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var score = 0.0;
        for (var index = 0; index < iterations; index++)
        {
            score += SongIdScoring.CompareLooseText(left, right);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(2.0 / 3.0, score / iterations, precision: 10);
        Assert.True(
            allocatedBytes < 8_500_000,
            $"Expected repeated loose-text comparisons below 8.5 MB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Theory]
    [InlineData("a b c a b", 0.0)]
    [InlineData("a b c d e f", 0.0)]
    [InlineData("a b c a b c", 0.5)]
    [InlineData("a b c a b c a b c", 1.0)]
    [InlineData("a a a a a a", 1.0)]
    [InlineData("DON'T stop now don't stop now", 0.5)]
    [InlineData("Ä A B C ä a b c", 0.5)]
    [InlineData("", 0.0)]
    public void ComputeRepeatedNgramRatio_PreservesTokenAndOccurrenceSemantics(string text, double expected)
    {
        Assert.Equal(expected, SongIdScoring.ComputeRepeatedNgramRatio(text), precision: 10);
    }

    [Fact]
    public void ComputeRepeatedNgramRatio_DuplicateHeavyTranscriptHasBoundedAllocation()
    {
        const string warmup = "warm repeated phrase warm repeated phrase";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.ComputeRepeatedNgramRatio(warmup);
        }

        const int repetitions = 10_000;
        var transcript = string.Join(' ', Enumerable.Repeat("alpha beta gamma", repetitions));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var ratio = SongIdScoring.ComputeRepeatedNgramRatio(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(1.0, ratio);
        Assert.True(
            allocatedBytes < 16 * 1024,
            $"Expected repeated-trigram analysis below 16 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ComputeRepeatedNgramRatio_UniqueTranscriptRetainsOnlyDistinctRangeKeys()
    {
        const string warmup = "warm unique phrase with enough tokens";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.ComputeRepeatedNgramRatio(warmup);
        }

        const int tokenCount = 10_000;
        var transcript = string.Join(' ', Enumerable.Range(0, tokenCount).Select(index =>
            $"token{(char)('a' + (index / 676))}{(char)('a' + ((index / 26) % 26))}{(char)('a' + (index % 26))}"));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var ratio = SongIdScoring.ComputeRepeatedNgramRatio(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0.0, ratio);
        Assert.True(
            allocatedBytes < 1_900_000,
            $"Expected unique-trigram analysis below 1.9 MB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Theory]
    [InlineData("hello world", 2)]
    [InlineData("DON'T stop", 2)]
    [InlineData("123 alpha-beta", 2)]
    [InlineData("ÄPFEL", 1)]
    [InlineData("' ''", 2)]
    [InlineData("abc123def", 2)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void CountTokens_PreservesAsciiLetterAndApostropheRuns(string? text, int expected)
    {
        Assert.Equal(expected, SongIdScoring.CountTokens(text!));
    }

    [Fact]
    public void CountTokens_LargeTranscriptHasBoundedAllocation()
    {
        const string warmup = "warm token count";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.CountTokens(warmup);
        }

        const int repetitions = 10_000;
        var transcript = string.Join(' ', Enumerable.Repeat("alpha beta gamma", repetitions));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var tokenCount = SongIdScoring.CountTokens(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(repetitions * 3, tokenCount);
        Assert.True(
            allocatedBytes < 1_024,
            $"Expected transcript token counting below 1 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Theory]
    [InlineData("AI generated by Suno and Udio", 4)]
    [InlineData("cover by ai", 1)]
    [InlineData("ai-made", 1)]
    [InlineData("chair", 0)]
    [InlineData("AI_MADE", 0)]
    [InlineData("fake artist", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void CountSyntheticMentions_PreservesCaseBoundaryAndAlternativeSemantics(string? text, int expected)
    {
        Assert.Equal(expected, SongIdScoring.CountSyntheticMentions(text!));
    }

    [Fact]
    public void CountSyntheticMentions_MatchHeavyTranscriptHasBoundedAllocation()
    {
        const string warmup = "AI generated by Suno and Udio";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.CountSyntheticMentions(warmup);
        }

        const int repetitions = 10_000;
        var transcript = string.Join(' ', Enumerable.Repeat("ai generated suno udio", repetitions));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var mentionCount = SongIdScoring.CountSyntheticMentions(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(repetitions * 4, mentionCount);
        Assert.True(
            allocatedBytes < 1_024,
            $"Expected synthetic-mention counting below 1 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Theory]
    [InlineData("", 0.0)]
    [InlineData("one\ntwo\nthree", 0.0)]
    [InlineData("one\none\ntwo", 2.0 / 3.0)]
    [InlineData("one\none\none", 1.0)]
    [InlineData("Alpha!\r\n alpha ", 1.0)]
    [InlineData("Artist & Guest\nartist and guest", 1.0)]
    [InlineData(" feat. \nfeat.", 1.0)]
    [InlineData("!!!\nalpha\nalpha", 1.0)]
    public void ComputeRepeatedLineRatio_PreservesSplitNormalizeAndOccurrenceSemantics(string text, double expected)
    {
        Assert.Equal(expected, SongIdScoring.ComputeRepeatedLineRatio(text), precision: 10);
    }

    [Fact]
    public void ComputeRepeatedLineRatio_DuplicateHeavyTranscriptHasBoundedAllocation()
    {
        const string warmup = "warm repeated line\nwarm repeated line";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.ComputeRepeatedLineRatio(warmup);
        }

        const int lineCount = 10_000;
        var transcript = string.Join('\n', Enumerable.Repeat("Artist feat. Guest", lineCount));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var ratio = SongIdScoring.ComputeRepeatedLineRatio(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(1.0, ratio);
        Assert.True(
            allocatedBytes < 16 * 1024,
            $"Expected duplicate-heavy repeated-line scoring below 16 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ComputeRepeatedLineRatio_UniqueTranscriptHasBoundedAllocation()
    {
        const string warmup = "warm unique line\nsecond distinct line";
        for (var index = 0; index < 32; index++)
        {
            _ = SongIdScoring.ComputeRepeatedLineRatio(warmup);
        }

        const int lineCount = 10_000;
        var transcript = string.Join('\n', Enumerable.Range(0, lineCount).Select(index =>
            $"line{(char)('a' + (index / 676))}{(char)('a' + ((index / 26) % 26))}{(char)('a' + (index % 26))} unique phrase"));

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var ratio = SongIdScoring.ComputeRepeatedLineRatio(transcript);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0.0, ratio);
        Assert.True(
            allocatedBytes < 2_400_000,
            $"Expected unique repeated-line scoring below 2.4 MB allocated, got {allocatedBytes:N0} bytes.");
    }

    private static string NormalizeLooseTextLegacy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.ToLowerInvariant()
            .Replace("&", " and ", StringComparison.Ordinal)
            .Replace(" feat. ", " featuring ", StringComparison.Ordinal)
            .Replace(" feat ", " featuring ", StringComparison.Ordinal)
            .Replace(" ft. ", " featuring ", StringComparison.Ordinal)
            .Replace(" ft ", " featuring ", StringComparison.Ordinal);
        return System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]+", " ").Trim();
    }
}
