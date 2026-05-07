// <copyright file="OpinionSubject.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Opinions;

public static class OpinionSubject
{
    public static (OpinionSubjectType Type, string Id) FromInterestItem(string item)
    {
        item = item?.Trim() ?? string.Empty;
        if (item.Length == 0)
        {
            return (OpinionSubjectType.Other, string.Empty);
        }

        var separator = item.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == item.Length - 1)
        {
            return (OpinionSubjectType.Other, item);
        }

        var prefix = item[..separator].Trim().ToLowerInvariant();
        var value = item[(separator + 1)..].Trim();
        if (value.Length == 0)
        {
            return (OpinionSubjectType.Other, item);
        }

        return prefix switch
        {
            "user" or "username" => (OpinionSubjectType.User, value),
            "file" or "filename" or "path" => (OpinionSubjectType.File, value),
            "hash" or "sha256" or "contenthash" or "content-hash" => (OpinionSubjectType.ContentHash, value),
            "artist" => (OpinionSubjectType.Artist, value),
            "album" => (OpinionSubjectType.Album, value),
            "song" or "track" => (OpinionSubjectType.Track, value),
            "pod" => (OpinionSubjectType.Pod, value),
            "source" => (OpinionSubjectType.Source, value),
            "peer" or "meshpeer" or "mesh-peer" => (OpinionSubjectType.MeshPeer, value),
            "search" or "query" => (OpinionSubjectType.SearchTerm, value),
            _ => (OpinionSubjectType.Other, item),
        };
    }
}
