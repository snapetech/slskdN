// <copyright file="MeshRendezvousPeerMessageRoundTripTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class MeshRendezvousPeerMessageRoundTripTests
    {
        [Trait("Category", "MeshRendezvous")]
        [Theory, AutoData]
        public async Task SendPeerMessageAsync_round_trips_between_two_clients(string username, string targetUsername, byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                payload = Encoding.UTF8.GetBytes("mesh-ping");
            }

            var messageCode = 4096;
            var peerEndpoint = new IPEndPoint(IPAddress.Loopback, 5000);
            var senderToReceiverConnection = new Mock<IMessageConnection>();

            senderToReceiverConnection.SetupGet(m => m.Username)
                .Returns(username);

            senderToReceiverConnection.SetupGet(m => m.IPEndPoint)
                .Returns(peerEndpoint);

            byte[] rawMessage = null;
            senderToReceiverConnection.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Callback<byte[], CancellationToken?>((message, _) => rawMessage = message)
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(targetUsername, peerEndpoint.Address, peerEndpoint.Port)));

            var serverConnA = new Mock<IMessageConnection>();
            var peerConnManagerA = new Mock<IPeerConnectionManager>();
            peerConnManagerA.Setup(m => m.GetOrAddMessageConnectionAsync(targetUsername, peerEndpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(senderToReceiverConnection.Object));

            using (var sender = new SoulseekClient(
                minorVersion: 9999,
                waiter: waiter.Object,
                serverConnection: serverConnA.Object,
                peerConnectionManager: peerConnManagerA.Object))
            using (var receiver = new SoulseekClient(minorVersion: 9999))
            {
                sender.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                receiver.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                byte[] receivedPayload = null;
                var receivedCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                receiver.RegisterPeerMessageHandler(messageCode, (fromUsername, endpoint, messagePayload) =>
                {
                    receivedPayload = messagePayload.ToArray();
                    receivedCompletion.TrySetResult(true);
                    return Task.CompletedTask;
                });

                await sender.SendPeerMessageAsync(targetUsername, messageCode, payload);

                var receiverHandler = receiver.GetProperty<IPeerMessageHandler>("PeerMessageHandler");
                receiverHandler.HandleMessageRead(senderToReceiverConnection.Object, rawMessage);

                var result = await Task.WhenAny(receivedCompletion.Task, Task.Delay(TimeSpan.FromMilliseconds(200)));
                Assert.Same(receivedCompletion.Task, result);
                Assert.NotNull(rawMessage);
                Assert.NotNull(receivedPayload);
                Assert.Equal(payload, receivedPayload);
            }
        }
    }
}
