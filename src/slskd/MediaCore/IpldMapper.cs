// <copyright file="IpldMapper.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace slskd.MediaCore;

/// <summary>
/// IPLD mapper for content graph traversal and link management.
/// Maps descriptors to IPLD-compatible shape (dag-cbor/json).
/// Feature-flagged; IPFS publishing is optional.
/// </summary>
public class IpldMapper : IIpldMapper
{
    private const int MaxInitialGraphCapacity = 4096;

    private readonly IContentIdRegistry _registry;
    private readonly ILogger<IpldMapper> _logger;
    private readonly Dictionary<string, List<IpldLink>> _outgoingLinks = new();
    private readonly Dictionary<string, List<IncomingLink>> _incomingLinksByTarget = new();
    private readonly Dictionary<string, int> _sourceOrder = new();
    private readonly object _linksLock = new();

    public IpldMapper(
        IContentIdRegistry registry,
        ILogger<IpldMapper> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Maps a ContentDescriptor to IPLD-compatible JSON.
    /// </summary>
    /// <param name="descriptor">The content descriptor to map.</param>
    /// <returns>IPLD-compatible JSON representation.</returns>
    public string ToJson(ContentDescriptor descriptor)
    {
        var ipld = new
        {
            contentId = descriptor.ContentId,
            hashes = descriptor.Hashes,
            phash = descriptor.PerceptualHashes,
            size = descriptor.SizeBytes,
            codec = descriptor.Codec,
            confidence = descriptor.Confidence,
            sig = descriptor.Signature,
            links = descriptor.Links?.AllLinks.Select(link => new
            {
                name = link.Name,
                target = link.Target,
                linkName = link.LinkName
            }).ToArray()
        };

        return JsonSerializer.Serialize(ipld, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <inheritdoc/>
    public async Task AddLinksAsync(string contentId, IEnumerable<IpldLink> links, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("ContentId cannot be empty", nameof(contentId));

        if (links == null)
            throw new ArgumentNullException(nameof(links));

        var linksList = links.ToList();
        if (!linksList.Any())
            return;

        // Verify the contentId exists in registry
        var exists = await _registry.IsContentIdRegisteredAsync(contentId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"ContentID '{contentId}' is not registered");
        }

        lock (_linksLock)
        {
            if (!_outgoingLinks.TryGetValue(contentId, out var list))
            {
                list = new List<IpldLink>();
                _outgoingLinks[contentId] = list;
                _sourceOrder[contentId] = _sourceOrder.Count;
            }

            list.AddRange(linksList);
            var sourceOrder = _sourceOrder[contentId];
            foreach (var link in linksList)
            {
                IndexIncomingLink(contentId, sourceOrder, link);
            }
        }

        _logger.LogInformation(
            "[IPLD] Added {LinkCount} links to ContentID {ContentId}: {LinkNames}",
            linksList.Count, contentId, string.Join(", ", linksList.Select(l => l.Name)));

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ContentGraphTraversal> TraverseAsync(string startContentId, string linkName, int maxDepth = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startContentId))
            throw new ArgumentException("Start ContentID cannot be empty", nameof(startContentId));

        if (string.IsNullOrWhiteSpace(linkName))
            throw new ArgumentException("Link name cannot be empty", nameof(linkName));

        if (maxDepth < 1 || maxDepth > 10)
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be between 1 and 10");

        var visited = new HashSet<string>();
        var nodes = new List<ContentGraphNode>();
        var paths = new List<ContentGraphPath>();

        var completed = await TraverseRecursiveAsync(
            startContentId, linkName, maxDepth, 0, visited, nodes, paths,
            new List<string> { startContentId }, new List<IpldLink>(), cancellationToken);

        return new ContentGraphTraversal(
            StartContentId: startContentId,
            LinkName: linkName,
            VisitedNodes: nodes,
            Paths: paths,
            CompletedTraversal: completed);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> FindInboundLinksAsync(string targetContentId, string? linkName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetContentId))
            throw new ArgumentException("Target ContentID cannot be empty", nameof(targetContentId));

        var inboundContentIds = new List<string>();

        lock (_linksLock)
        {
            if (_incomingLinksByTarget.TryGetValue(targetContentId, out var incomingLinks))
            {
                var index = 0;
                while (index < incomingLinks.Count)
                {
                    var sourceContentId = incomingLinks[index].SourceContentId;
                    var matches = false;
                    do
                    {
                        matches |= linkName == null || incomingLinks[index].Link.Name == linkName;
                        index++;
                    }
                    while (index < incomingLinks.Count && incomingLinks[index].SourceContentId == sourceContentId);

                    if (matches)
                    {
                        inboundContentIds.Add(sourceContentId);
                    }
                }
            }
        }

        await Task.CompletedTask;
        return inboundContentIds;
    }

    /// <inheritdoc/>
    public async Task<ContentGraph> GetGraphAsync(string contentId, int maxDepth = 2, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("ContentID cannot be empty", nameof(contentId));

        var rootNode = await CreateGraphNodeAsync(contentId, cancellationToken);
        var directCapacity = Math.Min(rootNode.OutgoingLinks.Count, MaxInitialGraphCapacity);
        var nodes = new List<ContentGraphNode>(directCapacity + 1) { rootNode };
        var paths = new List<ContentGraphPath>(directCapacity);
        var visited = new HashSet<string>(directCapacity + 1) { contentId };

        // Build the graph recursively
        await BuildGraphRecursiveAsync(rootNode, maxDepth, 0, nodes, paths, visited, cancellationToken);

        return new ContentGraph(
            RootContentId: contentId,
            Nodes: nodes,
            Paths: paths);
    }

    /// <inheritdoc/>
    public async Task<IpldValidationResult> ValidateLinksAsync(CancellationToken cancellationToken = default)
    {
        var brokenLinks = new List<string>();
        var orphanedLinks = new List<string>();
        var registrationStatusByContentId = new Dictionary<string, bool>();
        var totalValidated = 0;

        try
        {
            var registryStats = await _registry.GetStatsAsync(cancellationToken);
            var domains = registryStats.MappingsByDomain.Count > 0
                ? registryStats.MappingsByDomain.Keys
                : new[] { "audio", "video", "image" };

            foreach (var domain in domains)
            {
                var contentIds = await _registry.FindByDomainAsync(domain, cancellationToken);
                foreach (var contentId in contentIds)
                {
                    totalValidated++;
                    List<IpldLink> outgoing;
                    lock (_linksLock)
                    {
                        outgoing = _outgoingLinks.TryGetValue(contentId, out var stored)
                            ? stored.ToList()
                            : new List<IpldLink>();
                    }

                    foreach (var link in outgoing)
                    {
                        if (!await IsRegisteredAsync(link.Target))
                        {
                            brokenLinks.Add($"{contentId} -> {link.Target} ({link.Name})");
                        }
                    }
                }
            }

            List<(string SourceContentId, List<IpldLink> Links)> allOutgoingLinks;
            lock (_linksLock)
            {
                allOutgoingLinks = _outgoingLinks
                    .Select(kvp => (kvp.Key, kvp.Value.ToList()))
                    .ToList();
            }

            foreach (var (sourceContentId, links) in allOutgoingLinks)
            {
                if (!links.Any(link => !string.IsNullOrWhiteSpace(link.Target)) ||
                    await IsRegisteredAsync(sourceContentId))
                {
                    continue;
                }

                foreach (var link in links)
                {
                    if (!string.IsNullOrWhiteSpace(link.Target))
                    {
                        orphanedLinks.Add($"{sourceContentId} -> {link.Target} ({link.Name})");
                    }
                }
            }

            async Task<bool> IsRegisteredAsync(string contentId)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(contentId))
                {
                    return false;
                }

                if (registrationStatusByContentId.TryGetValue(contentId, out var isRegistered))
                {
                    return isRegistered;
                }

                isRegistered = await _registry.IsContentIdRegisteredAsync(contentId, cancellationToken);
                registrationStatusByContentId[contentId] = isRegistered;
                return isRegistered;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IPLD] Error during link validation");
        }

        var isValid = brokenLinks.Count == 0 && orphanedLinks.Count == 0;

        return new IpldValidationResult(
            IsValid: isValid,
            BrokenLinks: brokenLinks,
            OrphanedLinks: orphanedLinks,
            TotalLinksValidated: totalValidated);
    }

    private async Task<bool> TraverseRecursiveAsync(
        string currentContentId,
        string linkName,
        int maxDepth,
        int currentDepth,
        HashSet<string> visited,
        List<ContentGraphNode> nodes,
        List<ContentGraphPath> paths,
        List<string> currentPath,
        List<IpldLink> currentLinks,
        CancellationToken cancellationToken)
    {
        if (currentDepth >= maxDepth || visited.Contains(currentContentId))
        {
            return true;
        }

        visited.Add(currentContentId);

        try
        {
            var node = await CreateGraphNodeAsync(currentContentId, cancellationToken);
            nodes.Add(node);

            var links = node.OutgoingLinks.Where(l => l.Name == linkName).ToList();

            foreach (var link in links)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                currentPath.Add(link.Target);
                currentLinks.Add(link);

                var completed = await TraverseRecursiveAsync(
                    link.Target, linkName, maxDepth, currentDepth + 1,
                    visited, nodes, paths, currentPath, currentLinks, cancellationToken);

                if (!completed)
                    return false;

                paths.Add(new ContentGraphPath(
                    ContentIds: new List<string>(currentPath),
                    Links: new List<IpldLink>(currentLinks)));

                currentPath.RemoveAt(currentPath.Count - 1);
                currentLinks.RemoveAt(currentLinks.Count - 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IPLD] Error traversing from {ContentId}", currentContentId);
            return false;
        }

        return true;
    }

    private async Task BuildGraphRecursiveAsync(
        ContentGraphNode node,
        int maxDepth,
        int currentDepth,
        List<ContentGraphNode> nodes,
        List<ContentGraphPath> paths,
        HashSet<string> visited,
        CancellationToken cancellationToken)
    {
        if (currentDepth >= maxDepth)
            return;

        foreach (var link in node.OutgoingLinks)
        {
            if (!visited.Contains(link.Target))
            {
                visited.Add(link.Target);
                var childNode = await CreateGraphNodeAsync(link.Target, cancellationToken);
                nodes.Add(childNode);

                paths.Add(new ContentGraphPath(
                    ContentIds: new[] { node.ContentId, link.Target },
                    Links: new[] { link }));

                await BuildGraphRecursiveAsync(
                    childNode, maxDepth, currentDepth + 1, nodes, paths, visited, cancellationToken);
            }
        }
    }

    private async Task<ContentGraphNode> CreateGraphNodeAsync(string contentId, CancellationToken cancellationToken)
    {
        List<IpldLink>? storedCopy = null;
        lock (_linksLock)
        {
            if (_outgoingLinks.TryGetValue(contentId, out var stored) && stored.Count > 0)
                storedCopy = stored.ToList();
        }

        IReadOnlyList<IpldLink> outgoingLinks = storedCopy is null
            ? Array.Empty<IpldLink>()
            : storedCopy;
        var incomingLinks = await FindInboundLinksAsync(contentId, linkName: null, cancellationToken);

        return new ContentGraphNode(
            ContentId: contentId,
            OutgoingLinks: outgoingLinks,
            IncomingLinks: incomingLinks);
    }

    private void IndexIncomingLink(string sourceContentId, int sourceOrder, IpldLink link)
    {
        if (link.Target == null)
        {
            return;
        }

        if (!_incomingLinksByTarget.TryGetValue(link.Target, out var incomingLinks))
        {
            incomingLinks = new List<IncomingLink>();
            _incomingLinksByTarget[link.Target] = incomingLinks;
        }

        var low = 0;
        var high = incomingLinks.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (incomingLinks[middle].SourceOrder <= sourceOrder)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        incomingLinks.Insert(low, new IncomingLink(sourceContentId, sourceOrder, link));
    }

    private readonly record struct IncomingLink(string SourceContentId, int SourceOrder, IpldLink Link);

    private static bool IsInboundLink(string sourceContentId, string targetContentId, string? linkName)
    {
        var sourceParsed = ContentIdParser.Parse(sourceContentId);
        var targetParsed = ContentIdParser.Parse(targetContentId);

        if (sourceParsed == null || targetParsed == null)
            return false;

        // Basic relationship detection
        if (sourceParsed.Domain == targetParsed.Domain)
        {
            if (sourceParsed.Type == "track" && targetParsed.Type == "album" && sourceContentId.Contains(targetParsed.Id))
                return linkName == null || linkName == IpldLinkNames.Album;

            if (sourceParsed.Type == "album" && targetParsed.Type == "artist" && sourceContentId.Contains(targetParsed.Id))
                return linkName == null || linkName == IpldLinkNames.Artist;
        }

        return false;
    }
}
