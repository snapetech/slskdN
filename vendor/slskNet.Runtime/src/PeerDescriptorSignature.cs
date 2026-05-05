// <copyright file="PeerDescriptorSignature.cs" company="JP Dillingham">
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
    using System.Linq;

    /// <summary>
    ///     Signature material for a peer capability descriptor.
    /// </summary>
    public sealed class PeerDescriptorSignature
    {
        private readonly byte[] publicKey;
        private readonly byte[] signature;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerDescriptorSignature"/> class.
        /// </summary>
        /// <param name="publicKey">The raw public key bytes.</param>
        /// <param name="signature">The raw signature bytes.</param>
        /// <param name="algorithm">The signature algorithm name.</param>
        public PeerDescriptorSignature(byte[] publicKey, byte[] signature, string algorithm = "Ed25519")
        {
            this.publicKey = publicKey?.ToArray() ?? throw new ArgumentNullException(nameof(publicKey));
            this.signature = signature?.ToArray() ?? throw new ArgumentNullException(nameof(signature));
            Algorithm = string.IsNullOrWhiteSpace(algorithm) ? throw new ArgumentException("Algorithm must not be empty", nameof(algorithm)) : algorithm;
        }

        /// <summary>
        ///     Gets the signature algorithm.
        /// </summary>
        public string Algorithm { get; }

        /// <summary>
        ///     Gets the raw public key.
        /// </summary>
        public byte[] PublicKey => publicKey.ToArray();

        /// <summary>
        ///     Gets the raw signature.
        /// </summary>
        public byte[] Signature => signature.ToArray();
    }
}
