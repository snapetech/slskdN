// <copyright file="PeerProbeBudget.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using slskd.Common.IO;

internal enum PeerProbeBudgetDecision
{
    Allowed,
    Exhausted,
    Unavailable,
}

/// <summary>
///     Owns the durable daily limit for probes sent to Soulseek peers.
/// </summary>
internal sealed class PeerProbeBudget
{
    private readonly object _syncRoot = new();
    private readonly string _path;
    private readonly int _maxProbesPerPeerPerDay;
    private readonly Dictionary<string, PeerProbeBudgetEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;
    private Exception? _loadFailure;

    public PeerProbeBudget(string path, int maxProbesPerPeerPerDay)
    {
        _path = path;
        _maxProbesPerPeerPerDay = maxProbesPerPeerPerDay;
    }

    public PeerProbeBudgetDecision TryConsume(string username, out Exception? failure)
    {
        lock (_syncRoot)
        {
            if (!_loaded)
            {
                Load();
            }

            if (_loadFailure != null)
            {
                failure = _loadFailure;
                return PeerProbeBudgetDecision.Unavailable;
            }

            var today = DateTime.UtcNow.Date;
            if (!_entries.TryGetValue(username, out var current) || current.Day.Date != today)
            {
                current = new PeerProbeBudgetEntry { Day = today, Count = 0 };
            }

            if (current.Count >= _maxProbesPerPeerPerDay)
            {
                failure = null;
                return PeerProbeBudgetDecision.Exhausted;
            }

            _entries[username] = new PeerProbeBudgetEntry { Day = today, Count = current.Count + 1 };

            try
            {
                AtomicFileWriter.WriteAllText(_path, JsonSerializer.Serialize(_entries));
            }
            catch (Exception ex)
            {
                // Keep the in-memory increment so another attempt in this process cannot lower the limit.
                failure = ex;
                return PeerProbeBudgetDecision.Unavailable;
            }

            failure = null;
            return PeerProbeBudgetDecision.Allowed;
        }
    }

    private void Load()
    {
        _loadFailure = null;

        try
        {
            using var stream = File.OpenRead(_path);
            var persistedEntries = JsonSerializer.Deserialize<Dictionary<string, PeerProbeBudgetEntry>>(stream)
                ?? throw new InvalidDataException("The persisted peer probe budget is empty.");
            var today = DateTime.UtcNow.Date;
            var loadedEntries = new Dictionary<string, PeerProbeBudgetEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var (username, entry) in persistedEntries)
            {
                if (string.IsNullOrWhiteSpace(username) || entry == null || entry.Count < 0 || entry.Day == DateTime.MinValue || entry.Day.Date > today)
                {
                    throw new InvalidDataException("The persisted peer probe budget contains an invalid entry.");
                }

                if (entry.Day.Date != today)
                {
                    continue;
                }

                if (!loadedEntries.TryGetValue(username, out var existing) || entry.Count > existing.Count)
                {
                    loadedEntries[username] = new PeerProbeBudgetEntry { Day = today, Count = entry.Count };
                }
            }

            foreach (var (username, entry) in loadedEntries)
            {
                _entries[username] = entry;
            }

            _loaded = true;
        }
        catch (FileNotFoundException)
        {
            // A missing file is the expected first-run state.
            _loaded = true;
        }
        catch (DirectoryNotFoundException)
        {
            // The application data directory may not exist until the first state write.
            _loaded = true;
        }
        catch (Exception ex)
        {
            _loadFailure = ex;
        }
    }

    public sealed class PeerProbeBudgetEntry
    {
        public PeerProbeBudgetEntry()
        {
        }

        public DateTime Day { get; set; }

        public int Count { get; set; }
    }
}
