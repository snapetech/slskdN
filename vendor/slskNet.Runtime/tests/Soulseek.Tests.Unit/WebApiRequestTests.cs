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
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using WebAPI;
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

        [Theory(DisplayName = "Connect endpoint rejects invalid port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task Connect_Endpoint_Rejects_Invalid_Port(int port)
        {
            var controller = new ServerController(Mock.Of<ISoulseekClient>());

            var response = await controller.Connect(new ConnectRequest
            {
                Address = "127.0.0.1",
                Port = port,
                Username = "user",
                Password = "pass",
            });

            Assert.IsType<BadRequestObjectResult>(response);
        }

        [Theory(DisplayName = "Search endpoint rejects invalid option values")]
        [InlineData(0, null, null, null, null, null)]
        [InlineData(null, 0, null, null, null, null)]
        [InlineData(null, null, 0, null, null, null)]
        [InlineData(null, null, null, -1, null, null)]
        [InlineData(null, null, null, null, -1, null)]
        [InlineData(null, null, null, null, null, -1)]
        public async Task Search_Endpoint_Rejects_Invalid_Option_Values(
            int? searchTimeout,
            int? responseLimit,
            int? fileLimit,
            int? minimumResponseFileCount,
            int? maximumPeerQueueLength,
            int? minimumPeerUploadSpeed)
        {
            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());

            var response = await controller.Post(new SearchRequest
            {
                SearchText = "music",
                SearchTimeout = searchTimeout,
                ResponseLimit = responseLimit,
                FileLimit = fileLimit,
                MinimumResponseFileCount = minimumResponseFileCount,
                MaximumPeerQueueLength = maximumPeerQueueLength,
                MinimumPeerUploadSpeed = minimumPeerUploadSpeed,
            });

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

        [Fact(DisplayName = "User info resolver tolerates missing sample picture")]
        public async Task User_Info_Resolver_Tolerates_Missing_Sample_Picture()
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);

            try
            {
                Directory.SetCurrentDirectory(temp);
                var startup = new Startup(BuildConfiguration(temp));
                var method = typeof(Startup).GetMethod("UserInfoResponseResolver", BindingFlags.Instance | BindingFlags.NonPublic);

                var task = (Task<UserInfo>)method.Invoke(startup, new object[] { "user", new IPEndPoint(IPAddress.Loopback, 1) });
                var response = await task;

                Assert.False(response.HasPicture);
                Assert.Null(response.Picture);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                Directory.Delete(temp, recursive: true);
            }
        }

        [Fact(DisplayName = "Browse response resolver advertises relative directory names")]
        public async Task Browse_Response_Resolver_Advertises_Relative_Directory_Names()
        {
            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
            var album = Path.Combine(temp, "album");
            Directory.CreateDirectory(album);
            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");

            try
            {
                var startup = new Startup(BuildConfiguration(temp));
                var method = typeof(Startup).GetMethod("BrowseResponseResolver", BindingFlags.Instance | BindingFlags.NonPublic);

                var task = (Task<BrowseResponse>)method.Invoke(startup, new object[] { "user", new IPEndPoint(IPAddress.Loopback, 1) });
                var response = await task;

                var directory = Assert.Single(response.Directories);
                Assert.Equal("album", directory.Name);
                Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(temp, recursive: true);
            }
        }

        [Fact(DisplayName = "Directory contents resolver advertises relative directory names")]
        public async Task Directory_Contents_Resolver_Advertises_Relative_Directory_Names()
        {
            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
            var album = Path.Combine(temp, "album");
            var disc = Path.Combine(album, "disc1");
            Directory.CreateDirectory(disc);
            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");

            try
            {
                var startup = new Startup(BuildConfiguration(temp));
                var method = typeof(Startup).GetMethod("DirectoryContentsResponseResolver", BindingFlags.Instance | BindingFlags.NonPublic);

                var task = (Task<IEnumerable<Soulseek.Directory>>)method.Invoke(startup, new object[] { "user", new IPEndPoint(IPAddress.Loopback, 1), 1, "album" });
                var response = (await task).ToList();

                Assert.Contains(response, directory => directory.Name == "album");
                Assert.Contains(response, directory => directory.Name == Path.Combine("album", "disc1"));
                Assert.All(response, directory => Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal));
            }
            finally
            {
                Directory.Delete(temp, recursive: true);
            }
        }

        private static IConfiguration BuildConfiguration(string directory)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("USERNAME", "user"),
                    new KeyValuePair<string, string>("PASSWORD", "password"),
                    new KeyValuePair<string, string>("SHARED_DIR", directory),
                    new KeyValuePair<string, string>("OUTPUT_DIR", directory),
                })
                .Build();
        }
    }
}
