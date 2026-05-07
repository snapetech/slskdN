// <copyright file="EventBridgeTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Net;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Network;
    using Xunit;

    public class EventBridgeTests
    {
        [Fact(DisplayName = "Search responder bridge isolates throwing public subscribers")]
        public void Search_Responder_Bridge_Isolates_Throwing_Public_Subscribers()
        {
            var searchResponder = new Mock<ISearchResponder>();

            using (var client = new SoulseekClient(minorVersion: 9999, searchResponder: searchResponder.Object))
            {
                client.SearchRequestReceived += (sender, e) => throw new InvalidOperationException("subscriber failed");

                var ex = Record.Exception(() => searchResponder.Raise(
                    m => m.RequestReceived += null,
                    searchResponder.Object,
                    new SearchRequestEventArgs("username", 1, "query")));

                Assert.Null(ex);
            }
        }

        [Fact(DisplayName = "Distributed manager bridge isolates throwing public subscribers")]
        public void Distributed_Manager_Bridge_Isolates_Throwing_Public_Subscribers()
        {
            var distributedConnectionManager = new Mock<IDistributedConnectionManager>();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1);

            using (var client = new SoulseekClient(minorVersion: 9999, distributedConnectionManager: distributedConnectionManager.Object))
            {
                client.DistributedParentAdopted += (sender, e) => throw new InvalidOperationException("subscriber failed");

                var ex = Record.Exception(() => distributedConnectionManager.Raise(
                    m => m.ParentAdopted += null,
                    distributedConnectionManager.Object,
                    new DistributedParentEventArgs("username", endpoint, 1, "root")));

                Assert.Null(ex);
            }
        }

        [Fact(DisplayName = "Server message bridge isolates throwing public subscribers")]
        public void Server_Message_Bridge_Isolates_Throwing_Public_Subscribers()
        {
            var serverMessageHandler = new Mock<IServerMessageHandler>();

            using (var client = new SoulseekClient(minorVersion: 9999, serverMessageHandler: serverMessageHandler.Object))
            {
                client.UserCannotConnect += (sender, e) => throw new InvalidOperationException("subscriber failed");

                var ex = Record.Exception(() => serverMessageHandler.Raise(
                    m => m.UserCannotConnect += null,
                    serverMessageHandler.Object,
                    new UserCannotConnectEventArgs(1, "username")));

                Assert.Null(ex);
            }
        }

        [Fact(DisplayName = "Diagnostic bridge isolates throwing public subscribers")]
        public void Diagnostic_Bridge_Isolates_Throwing_Public_Subscribers()
        {
            var listenerHandler = new Mock<IListenerHandler>();

            using (var client = new SoulseekClient(minorVersion: 9999, listenerHandler: listenerHandler.Object))
            {
                client.DiagnosticGenerated += (sender, e) => throw new InvalidOperationException("subscriber failed");

                var ex = Record.Exception(() => listenerHandler.Raise(
                    m => m.DiagnosticGenerated += null,
                    listenerHandler.Object,
                    new DiagnosticEventArgs(DiagnosticLevel.Info, "message")));

                Assert.Null(ex);
            }
        }
    }
}
