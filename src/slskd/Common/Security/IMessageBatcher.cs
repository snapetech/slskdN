// <copyright file="IMessageBatcher.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Collections;
using System.Collections.Generic;

namespace slskd.Common.Security;

/// <summary>
/// Interface for message batching implementations.
/// </summary>
public interface IMessageBatcher
{
    /// <summary>
    /// Adds a message to the current batch.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="metadata">Optional metadata about the message.</param>
    /// <returns>A task that completes when the message is queued.</returns>
    Task AddMessageAsync(byte[] message, IReadOnlyDictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets the next batch of messages, waiting if necessary for the batch window.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch of messages.</returns>
    Task<IReadOnlyList<BatchedMessage>> GetNextBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces immediate sending of the current batch.
    /// </summary>
    /// <returns>The current batch of messages.</returns>
    Task<IReadOnlyList<BatchedMessage>> FlushAsync();
}

/// <summary>
/// Represents a message in a batch.
/// </summary>
public class BatchedMessage
{
    /// <summary>
    /// Gets the message data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the metadata associated with the message.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the timestamp when the message was added to the batch.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchedMessage"/> class.
    /// </summary>
    /// <param name="data">The message data.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="timestamp">The timestamp.</param>
    public BatchedMessage(byte[] data, IReadOnlyDictionary<string, object>? metadata, DateTimeOffset timestamp)
    {
        Data = data?.ToArray() ?? throw new ArgumentNullException(nameof(data));
        Metadata = metadata is null
            ? new Dictionary<string, object>()
            : metadata.ToDictionary(kvp => kvp.Key, kvp => CopyMetadataValue(kvp.Value)!);
        Timestamp = timestamp;
    }

    private static object? CopyMetadataValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => bytes.ToArray(),
            IDictionary<string, object> dictionary => dictionary.ToDictionary(kvp => kvp.Key, kvp => CopyMetadataValue(kvp.Value)),
            IDictionary dictionary => CopyMetadataDictionary(dictionary),
            IDictionary<string, string> dictionary => dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as object),
            Array array => CopyMetadataArray(array),
            IList list => CopyMetadataList(list),
            _ => value,
        };
    }

    private static Dictionary<string, object> CopyMetadataDictionary(IDictionary metadata)
    {
        var copied = new Dictionary<string, object>(metadata.Count);

        foreach (DictionaryEntry kvp in metadata)
        {
            if (kvp.Key is not string key)
            {
                continue;
            }

            copied[key] = CopyMetadataValue(kvp.Value)!;
        }

        return copied;
    }

    private static List<object> CopyMetadataList(IList metadata)
    {
        var copied = new List<object>(metadata.Count);

        foreach (var value in metadata)
        {
            copied.Add(CopyMetadataValue(value)!);
        }

        return copied;
    }

    private static Array CopyMetadataArray(Array value)
    {
        if (value is byte[] bytes)
        {
            return bytes.ToArray();
        }

        if (value.Length == 0)
        {
            return Array.CreateInstance(value.GetType().GetElementType() ?? typeof(object), 0);
        }

        var copied = Array.CreateInstance(value.GetType().GetElementType() ?? typeof(object), value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            copied.SetValue(CopyMetadataValue(value.GetValue(i)), i);
        }

        return copied;
    }
}
