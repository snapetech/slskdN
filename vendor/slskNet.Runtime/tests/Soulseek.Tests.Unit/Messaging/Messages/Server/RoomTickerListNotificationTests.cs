// <copyright file="RoomTickerListNotificationTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class RoomTickerListNotificationTests
    {
        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => RoomTickerListNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => RoomTickerListNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string roomName, List<RoomTicker> tickers)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString(roomName)
                .WriteInteger(tickers.Count);

            tickers.ForEach(ticker =>
            {
                builder
                    .WriteString(ticker.Username)
                    .WriteString(ticker.Message);
            });

            var response = RoomTickerListNotification.FromByteArray(builder.Build());

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(tickers.Count, response.TickerCount);

            foreach (var ticker in tickers)
            {
                Assert.Contains(response.Tickers, t => ticker.Username == t.Username && ticker.Message == t.Message);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiation snapshots tickers"), AutoData]
        public void Instantiation_Snapshots_Tickers(string roomName, List<RoomTicker> tickers)
        {
            var response = new RoomTickerListNotification(roomName, tickers.Count, tickers);
            tickers.Add(new RoomTicker("late", "late"));

            Assert.Equal(response.TickerCount, response.Tickers.Count);
            Assert.NotEqual(tickers.Count, response.Tickers.Count);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiation rejects invalid ticker metadata"), AutoData]
        public void Instantiation_Rejects_Invalid_Ticker_Metadata(string roomName, List<RoomTicker> tickers)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomTickerListNotification(roomName, -1, tickers));
            Assert.Throws<ArgumentException>(() => new RoomTickerListNotification(roomName, tickers.Count + 1, tickers));
            Assert.Throws<ArgumentException>(() => new RoomTickerListNotification(roomName, 1, new RoomTicker[] { null }));
        }
    }
}
