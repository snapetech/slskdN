// <copyright file="MusicDomainMapping.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.Core.Music
{
    using System;
    using System.Buffers;
    using System.Security.Cryptography;
    using System.Text;
    using slskd.VirtualSoulfind.Core;

    /// <summary>
    ///     Utilities for mapping between MusicBrainz identifiers and domain-neutral Content IDs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This provides deterministic, bidirectional mapping between:
    ///         - MusicBrainz Release IDs → ContentWorkId
    ///         - MusicBrainz Recording IDs → ContentItemId
    ///     </para>
    ///     <para>
    ///         The mapping uses a namespace-based UUID v5 approach to ensure:
    ///         - Same MBID always produces same ContentId (deterministic)
    ///         - Different MBIDs produce different ContentIds (no collisions)
    ///         - Reverse mapping is possible (store MBID separately, use as key)
    ///     </para>
    /// </remarks>
    public static class MusicDomainMapping
    {
        // Namespace UUID for MusicBrainz Release IDs (v5 UUID namespace)
        private static readonly Guid ReleaseNamespace = new Guid("a3c5f8d2-4e1b-5a7c-9f2e-3d6b8c1a4f5e");

        // Namespace UUID for MusicBrainz Recording IDs
        private static readonly Guid RecordingNamespace = new Guid("b7d9e3f1-6c2a-4b8d-a5f3-7e9c1d4b6a8f");

        /// <summary>
        ///     Converts a MusicBrainz Release ID to a <see cref="ContentWorkId"/>.
        /// </summary>
        /// <param name="releaseId">The MusicBrainz Release ID (GUID format).</param>
        /// <returns>A deterministic <see cref="ContentWorkId"/>.</returns>
        /// <exception cref="ArgumentException">If the release ID is invalid.</exception>
        public static ContentWorkId ReleaseIdToContentWorkId(string releaseId)
        {
            if (string.IsNullOrWhiteSpace(releaseId))
            {
                throw new ArgumentException("Release ID cannot be null or empty.", nameof(releaseId));
            }

            // Validate it's a valid GUID
            if (!Guid.TryParse(releaseId, out var mbid))
            {
                throw new ArgumentException($"Invalid MusicBrainz Release ID format: {releaseId}", nameof(releaseId));
            }

            // Generate deterministic UUID v5 (namespace + name)
            var deterministicGuid = GenerateUuidV5(ReleaseNamespace, releaseId);
            return new ContentWorkId(deterministicGuid);
        }

        /// <summary>
        ///     Converts a MusicBrainz Recording ID to a <see cref="ContentItemId"/>.
        /// </summary>
        /// <param name="recordingId">The MusicBrainz Recording ID (GUID format).</param>
        /// <returns>A deterministic <see cref="ContentItemId"/>.</returns>
        /// <exception cref="ArgumentException">If the recording ID is invalid.</exception>
        public static ContentItemId RecordingIdToContentItemId(string recordingId)
        {
            if (string.IsNullOrWhiteSpace(recordingId))
            {
                throw new ArgumentException("Recording ID cannot be null or empty.", nameof(recordingId));
            }

            // Validate it's a valid GUID
            if (!Guid.TryParse(recordingId, out var mbid))
            {
                throw new ArgumentException($"Invalid MusicBrainz Recording ID format: {recordingId}", nameof(recordingId));
            }

            // Generate deterministic UUID v5 (namespace + name)
            var deterministicGuid = GenerateUuidV5(RecordingNamespace, recordingId);
            return new ContentItemId(deterministicGuid);
        }

        /// <summary>
        ///     Generates a UUID v5 (namespace + name) for deterministic ID generation.
        /// </summary>
        /// <param name="namespaceId">The namespace UUID.</param>
        /// <param name="name">The name (MBID) to hash.</param>
        /// <returns>A deterministic UUID.</returns>
        /// <remarks>
        ///     This implements RFC 4122 UUID v5 (SHA-1 based).
        ///     Reference: https://tools.ietf.org/html/rfc4122#section-4.3
        /// </remarks>
        private static Guid GenerateUuidV5(Guid namespaceId, string name)
        {
            var normalizedName = name.ToLowerInvariant();
            var byteCount = 16 + Encoding.UTF8.GetByteCount(normalizedName);
            byte[]? rentedBytes = null;
            Span<byte> bytes = byteCount <= 512
                ? stackalloc byte[byteCount]
                : (rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount));

            try
            {
                _ = namespaceId.TryWriteBytes(bytes[..16], bigEndian: true, out _);
                _ = Encoding.UTF8.GetBytes(normalizedName, bytes[16..]);

                Span<byte> hash = stackalloc byte[20];
                SHA1.HashData(bytes[..byteCount], hash);

                Span<byte> uuidBytes = stackalloc byte[16];
                hash[..16].CopyTo(uuidBytes);

                // Set version (v5 = 0101) and variant (10xx) bits per RFC 4122
                uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x50); // Version 5
                uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80); // Variant 10xx

                return new Guid(uuidBytes, bigEndian: true);
            }
            finally
            {
                if (rentedBytes != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
                }
            }
        }
    }
}
