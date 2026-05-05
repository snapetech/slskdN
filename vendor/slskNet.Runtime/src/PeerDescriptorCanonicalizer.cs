// <copyright file="PeerDescriptorCanonicalizer.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    ///     Canonical byte serialization for peer capability descriptors.
    /// </summary>
    public static class PeerDescriptorCanonicalizer
    {
        /// <summary>
        ///     Gets canonical signing bytes for a descriptor. Signature bytes are deliberately excluded.
        /// </summary>
        /// <param name="descriptor">The descriptor to canonicalize.</param>
        /// <returns>The canonical bytes.</returns>
        public static byte[] GetSigningBytes(PeerCapabilityDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                WriteString(writer, descriptor.PeerId);
                writer.Write(descriptor.OverlayPort ?? -1);
                writer.Write(descriptor.MaxPayloadLength);
                writer.Write(descriptor.Features.Count);

                foreach (var feature in descriptor.Features)
                {
                    WriteString(writer, feature);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
