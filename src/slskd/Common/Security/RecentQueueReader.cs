// <copyright file="RecentQueueReader.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

internal static class RecentQueueReader
{
    public static IReadOnlyList<T> ReadNewest<T>(ConcurrentQueue<T> queue, int count, int retentionLimit)
    {
        if (count <= 0)
        {
            return Array.Empty<T>();
        }

        var capacity = Math.Min(count, retentionLimit);
        T[]? buffer = null;
        var itemCount = 0;
        foreach (var item in queue)
        {
            buffer ??= new T[capacity];
            buffer[itemCount++ % capacity] = item;
        }

        if (buffer == null)
        {
            return Array.Empty<T>();
        }

        if (itemCount < buffer.Length)
        {
            Array.Resize(ref buffer, itemCount);
            Array.Reverse(buffer);
            return buffer;
        }

        var nextIndex = itemCount % buffer.Length;
        Array.Reverse(buffer, 0, nextIndex);
        Array.Reverse(buffer, nextIndex, buffer.Length - nextIndex);
        return buffer;
    }
}
