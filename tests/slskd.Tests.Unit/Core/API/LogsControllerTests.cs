// <copyright file="LogsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core.API;

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using slskd.Core.API;
using Xunit;

public class LogsControllerTests
{
    [Theory]
    [InlineData(typeof(LogsController), nameof(LogsController.Logs))]
    [InlineData(typeof(LogsHub), null)]
    public void LogSurfaces_RequireAdministratorRole(Type type, string? actionName)
    {
        var attributes = actionName == null
            ? type.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            : type.GetMethod(actionName)!.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        var authorize = Assert.Single(attributes);

        Assert.Equal(AuthPolicy.Any, authorize.Policy);
        Assert.Equal(AuthRole.AdministratorOnly, authorize.Roles);
    }

    [Fact]
    public void Logs_ReturnsSnapshotArray()
    {
        Program.LogBuffer.Enqueue(new LogRecord
        {
            Context = "test",
            Message = "message",
            Timestamp = DateTime.UtcNow,
        });

        var controller = new LogsController();

        var result = Assert.IsType<OkObjectResult>(controller.Logs());

        Assert.IsAssignableFrom<LogRecord[]>(result.Value);
    }
}
