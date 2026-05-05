// <copyright file="Ed25519PeerDescriptorSignerTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using Xunit;

    public class Ed25519PeerDescriptorSignerTests
    {
        [Fact(DisplayName = "Ed25519 descriptor signer signs and verifies descriptor")]
        public void Ed25519_Descriptor_Signer_Signs_And_Verifies_Descriptor()
        {
            using (var signer = new Ed25519PeerDescriptorSigner())
            {
                var keys = signer.GenerateKeyPair();
                var descriptor = new PeerCapabilityDescriptor(features: new[] { "mesh", "wishlist" });

                var signed = signer.Sign(descriptor, keys.PrivateKey, keys.PublicKey);

                Assert.NotNull(signed.PeerId);
                Assert.Equal(32, signed.Signature.PublicKey.Length);
                Assert.Equal(64, signed.Signature.Signature.Length);
                Assert.True(signer.Verify(signed));
                Assert.False(signer.Verify(new PeerCapabilityDescriptor(
                    signed.PeerId,
                    new[] { "mesh" },
                    signed.OverlayPort,
                    signed.MaxPayloadLength,
                    signed.Signature)));
            }
        }
    }
}
