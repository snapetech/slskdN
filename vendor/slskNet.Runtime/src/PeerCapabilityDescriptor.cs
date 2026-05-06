// <copyright file="PeerCapabilityDescriptor.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     slskdN peer capability descriptor exchanged over a normal Soulseek peer-message connection.
    /// </summary>
    public sealed class PeerCapabilityDescriptor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerCapabilityDescriptor"/> class.
        /// </summary>
        /// <param name="peerId">The optional stable overlay peer id.</param>
        /// <param name="features">The supported feature names.</param>
        /// <param name="overlayPort">The optional overlay transport port.</param>
        /// <param name="maxPayloadLength">The maximum custom peer-message payload length accepted by the peer.</param>
        /// <param name="signature">The optional descriptor signature.</param>
        public PeerCapabilityDescriptor(
            string peerId = null,
            IEnumerable<string> features = null,
            int? overlayPort = null,
            int maxPayloadLength = PeerCapabilityEnvelope.DefaultMaxPayloadLength,
            PeerDescriptorSignature signature = null)
        {
            if (overlayPort.HasValue && (overlayPort.Value < 0 || overlayPort.Value > 65535))
            {
                throw new ArgumentOutOfRangeException(nameof(overlayPort), "Overlay port must be between 0 and 65535.");
            }

            if (maxPayloadLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPayloadLength), "Maximum payload length must be greater than or equal to zero.");
            }

            PeerId = peerId == null
                ? null
                : ProtocolArgumentValidator.RequireMaximumUtf8Length(peerId, nameof(peerId), "peer id", PeerCapabilityEnvelope.MaximumStringLength);

            var featureList = (features ?? Enumerable.Empty<string>())
                .Where(feature => !string.IsNullOrWhiteSpace(feature))
                .Select(feature => feature.Trim())
                .Select(feature => ProtocolArgumentValidator.RequireMaximumUtf8Length(feature, nameof(features), "feature", PeerCapabilityEnvelope.MaximumStringLength))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(feature => feature, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (featureList.Count > PeerCapabilityEnvelope.MaximumFeatureCount)
            {
                throw new ArgumentOutOfRangeException(nameof(features), $"Feature count must not exceed {PeerCapabilityEnvelope.MaximumFeatureCount}.");
            }

            Features = featureList.AsReadOnly();
            OverlayPort = overlayPort;
            MaxPayloadLength = maxPayloadLength;
            Signature = signature;
        }

        /// <summary>
        ///     Gets the supported feature names.
        /// </summary>
        public IReadOnlyCollection<string> Features { get; }

        /// <summary>
        ///     Gets the maximum custom peer-message payload length accepted by the peer.
        /// </summary>
        public int MaxPayloadLength { get; }

        /// <summary>
        ///     Gets the optional overlay transport port.
        /// </summary>
        public int? OverlayPort { get; }

        /// <summary>
        ///     Gets the optional stable overlay peer id.
        /// </summary>
        public string PeerId { get; }

        /// <summary>
        ///     Gets the optional descriptor signature.
        /// </summary>
        public PeerDescriptorSignature Signature { get; }

        /// <summary>
        ///     Creates a copy with new signature material.
        /// </summary>
        /// <param name="signature">The signature material.</param>
        /// <returns>The signed descriptor.</returns>
        public PeerCapabilityDescriptor WithSignature(PeerDescriptorSignature signature)
        {
            return new PeerCapabilityDescriptor(PeerId, Features, OverlayPort, MaxPayloadLength, signature);
        }
    }
}
