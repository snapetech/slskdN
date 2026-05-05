// <copyright file="WebApiRequestTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using WebAPI.Controllers;
    using WebAPI.DTO;
    using WebAPI.Trackers;
    using Xunit;

    public class WebApiRequestTests
    {
        [Fact(DisplayName = "Search endpoint rejects null request")]
        public async Task Search_Endpoint_Rejects_Null_Request()
        {
            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());

            var response = await controller.Post(null);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Search endpoint rejects blank search text")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public async Task Search_Endpoint_Rejects_Blank_Search_Text(string searchText)
        {
            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());

            var response = await controller.Post(new SearchRequest { SearchText = searchText });

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact(DisplayName = "User search endpoint rejects blank username")]
        public async Task User_Search_Endpoint_Rejects_Blank_Username()
        {
            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());

            var response = await controller.PostUsers(new SearchRequest { SearchText = "music" }, " ");

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact(DisplayName = "Connect endpoint rejects null request")]
        public async Task Connect_Endpoint_Rejects_Null_Request()
        {
            var controller = new ServerController(Mock.Of<ISoulseekClient>());

            var response = await controller.Connect(null);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Room message endpoint rejects blank message")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Room_Message_Endpoint_Rejects_Blank_Message(string message)
        {
            var tracker = new RoomTracker();
            tracker.TryAdd("room", new WebAPI.Room());
            var controller = new RoomsController(Mock.Of<ISoulseekClient>(), tracker);

            var response = await controller.SendMessage("room", message);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Room ticker endpoint rejects blank message")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Room_Ticker_Endpoint_Rejects_Blank_Message(string message)
        {
            var tracker = new RoomTracker();
            tracker.TryAdd("room", new WebAPI.Room());
            var controller = new RoomsController(Mock.Of<ISoulseekClient>(), tracker);

            var response = await controller.SetTicker("room", message);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Room member endpoint rejects blank username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Room_Member_Endpoint_Rejects_Blank_Username(string username)
        {
            var tracker = new RoomTracker();
            tracker.TryAdd("room", new WebAPI.Room());
            var controller = new RoomsController(Mock.Of<ISoulseekClient>(), tracker);

            var response = await controller.AddRoomMember("room", username);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Conversation send endpoint rejects blank message")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Conversation_Send_Endpoint_Rejects_Blank_Message(string message)
        {
            var controller = new ConversationsController(Mock.Of<ISoulseekClient>(), new ConversationTracker());

            var response = await controller.Send("user", message);

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact(DisplayName = "Conversation send endpoint rejects blank username")]
        public async Task Conversation_Send_Endpoint_Rejects_Blank_Username()
        {
            var controller = new ConversationsController(Mock.Of<ISoulseekClient>(), new ConversationTracker());

            var response = await controller.Send(" ", "message");

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Fact(DisplayName = "Token response tolerates missing optional claims")]
        public void Token_Response_Tolerates_Missing_Optional_Claims()
        {
            var notBefore = DateTime.UtcNow.AddMinutes(-1);
            var token = new JwtSecurityToken(notBefore: notBefore, expires: notBefore.AddHours(1));
            var response = new TokenResponse(token);

            Assert.Null(response.Name);
            Assert.Equal(((DateTimeOffset)token.ValidFrom).ToUnixTimeSeconds(), response.NotBefore);
        }
    }
}
