// <copyright file="MeshRendezvousServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    public class MeshRendezvousServiceTests
    {
        [Fact(DisplayName = "Rendezvous service registers configured interest")]
        public async Task Rendezvous_Service_Registers_Configured_Interest()
        {
            var client = new Mock<ISoulseekClient>();
            var service = new MeshRendezvousService(client.Object, new MeshRendezvousOptions("tag"));

            await service.RegisterAsync();

            client.Verify(m => m.AddInterestAsync("tag", It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Fact(DisplayName = "Rendezvous service probes similar users when descriptor is configured")]
        public async Task Rendezvous_Service_Probes_Similar_Users_When_Descriptor_Is_Configured()
        {
            var client = new Mock<ISoulseekClient>();
            client.SetupGet(m => m.Username).Returns("self");
            client.SetupGet(m => m.PeerCapabilityDescriptor).Returns(new PeerCapabilityDescriptor());
            client.SetupGet(m => m.PeerCapabilities).Returns(new PeerCapabilityRegistry());
            client.Setup(m => m.GetSimilarUsersAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult<IReadOnlyCollection<SimilarUser>>(new[]
                {
                    new SimilarUser("self", 0),
                    new SimilarUser("alice", 1),
                }));
            client.Setup(m => m.SendPeerCapabilityAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            var service = new MeshRendezvousService(client.Object, new MeshRendezvousOptions(probePeerCapabilities: true));

            var result = await service.DiscoverAsync();

            Assert.Equal(SoulseekClient.MeshRendezvousInterestTag, result.InterestTag);
            client.Verify(m => m.SendPeerCapabilityAsync("alice", null, It.IsAny<CancellationToken?>()), Times.Once);
            client.Verify(m => m.SendPeerCapabilityAsync("self", null, It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Fact(DisplayName = "Rendezvous service uses ordinal username identity")]
        public async Task Rendezvous_Service_Uses_Ordinal_Username_Identity()
        {
            var client = new Mock<ISoulseekClient>();
            client.SetupGet(m => m.Username).Returns("self");
            client.SetupGet(m => m.PeerCapabilityDescriptor).Returns(new PeerCapabilityDescriptor());
            client.SetupGet(m => m.PeerCapabilities).Returns(new PeerCapabilityRegistry());
            client.Setup(m => m.GetSimilarUsersAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult<IReadOnlyCollection<SimilarUser>>(new[]
                {
                    new SimilarUser("s\0elf", 1),
                }));
            client.Setup(m => m.SendPeerCapabilityAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            var service = new MeshRendezvousService(client.Object, new MeshRendezvousOptions(probePeerCapabilities: true));

            await service.DiscoverAsync();

            client.Verify(m => m.SendPeerCapabilityAsync("s\0elf", null, It.IsAny<CancellationToken?>()), Times.Once);
        }
    }
}
