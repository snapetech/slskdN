// <copyright file="Ed25519PeerDescriptorSigner.cs" company="slskdN Team">
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
    using System.Linq;
    using System.Security.Cryptography;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Signers;
    using Org.BouncyCastle.Security;

    /// <summary>
    ///     Ed25519 descriptor signer and verifier.
    /// </summary>
    public sealed class Ed25519PeerDescriptorSigner : IPeerDescriptorSigner, IPeerDescriptorVerifier, IDisposable
    {
        /// <summary>
        ///     Derives a self-certifying peer id from a raw Ed25519 public key.
        /// </summary>
        /// <param name="publicKey">The 32-byte raw Ed25519 public key.</param>
        /// <returns>A lower-case base32 peer id.</returns>
        public static string DerivePeerId(byte[] publicKey)
        {
            if (publicKey == null || publicKey.Length != 32)
            {
                throw new ArgumentException("Ed25519 public key must be 32 bytes.", nameof(publicKey));
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(publicKey).Take(20).ToArray();
                return Base32Encode(hash).ToLowerInvariant();
            }
        }

        /// <summary>
        ///     Generates a raw Ed25519 key pair.
        /// </summary>
        /// <returns>The raw private and public key bytes.</returns>
        public (byte[] PrivateKey, byte[] PublicKey) GenerateKeyPair()
        {
            var privateKey = new Ed25519PrivateKeyParameters(new SecureRandom());
            return (privateKey.GetEncoded(), privateKey.GeneratePublicKey().GetEncoded());
        }

        /// <summary>
        ///     Signs a descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor to sign.</param>
        /// <param name="privateKey">The 32-byte raw Ed25519 private key.</param>
        /// <param name="publicKey">The 32-byte raw Ed25519 public key.</param>
        /// <returns>The signed descriptor.</returns>
        public PeerCapabilityDescriptor Sign(PeerCapabilityDescriptor descriptor, byte[] privateKey, byte[] publicKey)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (privateKey == null || privateKey.Length != 32 || privateKey.All(b => b == 0))
            {
                throw new ArgumentException("Ed25519 private key must be 32 non-zero bytes.", nameof(privateKey));
            }

            if (publicKey == null || publicKey.Length != 32)
            {
                throw new ArgumentException("Ed25519 public key must be 32 bytes.", nameof(publicKey));
            }

            var derivedPeerId = DerivePeerId(publicKey);

            if (!string.IsNullOrWhiteSpace(descriptor.PeerId) &&
                !string.Equals(descriptor.PeerId, derivedPeerId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Descriptor peer id must match the supplied Ed25519 public key.", nameof(descriptor));
            }

            var unsigned = new PeerCapabilityDescriptor(
                derivedPeerId,
                descriptor.Features,
                descriptor.OverlayPort,
                descriptor.MaxPayloadLength);

            var signer = new Ed25519Signer();
            signer.Init(forSigning: true, new Ed25519PrivateKeyParameters(privateKey, 0));

            var signingBytes = PeerDescriptorCanonicalizer.GetSigningBytes(unsigned);
            signer.BlockUpdate(signingBytes, 0, signingBytes.Length);

            return unsigned.WithSignature(new PeerDescriptorSignature(publicKey, signer.GenerateSignature()));
        }

        /// <summary>
        ///     Verifies a descriptor signature.
        /// </summary>
        /// <param name="descriptor">The descriptor to verify.</param>
        /// <returns>A value indicating whether the signature is valid.</returns>
        public bool Verify(PeerCapabilityDescriptor descriptor)
        {
            if (descriptor?.Signature == null ||
                !string.Equals(descriptor.Signature.Algorithm, "Ed25519", StringComparison.OrdinalIgnoreCase) ||
                descriptor.Signature.PublicKey.Length != 32 ||
                descriptor.Signature.Signature.Length != 64)
            {
                return false;
            }

            if (!string.Equals(descriptor.PeerId, DerivePeerId(descriptor.Signature.PublicKey), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var unsigned = new PeerCapabilityDescriptor(
                descriptor.PeerId,
                descriptor.Features,
                descriptor.OverlayPort,
                descriptor.MaxPayloadLength);

            try
            {
                var verifier = new Ed25519Signer();
                verifier.Init(forSigning: false, new Ed25519PublicKeyParameters(descriptor.Signature.PublicKey, 0));

                var signingBytes = PeerDescriptorCanonicalizer.GetSigningBytes(unsigned);
                verifier.BlockUpdate(signingBytes, 0, signingBytes.Length);

                return verifier.VerifySignature(descriptor.Signature.Signature);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Disposes this signer.
        /// </summary>
        public void Dispose()
        {
        }

        private static string Base32Encode(byte[] data)
        {
            const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new System.Text.StringBuilder();
            var bits = 0;
            var bitCount = 0;

            foreach (var value in data)
            {
                bits = (bits << 8) | value;
                bitCount += 8;

                while (bitCount >= 5)
                {
                    bitCount -= 5;
                    result.Append(Alphabet[(bits >> bitCount) & 31]);
                }
            }

            if (bitCount > 0)
            {
                result.Append(Alphabet[(bits << (5 - bitCount)) & 31]);
            }

            return result.ToString();
        }
    }
}
