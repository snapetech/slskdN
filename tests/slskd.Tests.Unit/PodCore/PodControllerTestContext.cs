// <copyright file="PodControllerTestContext.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

internal static class PodControllerTestContext
{
    public static T AsAdministrator<T>(T controller, string identity = "test-admin")
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, identity),
                        new Claim(ClaimTypes.Role, "Administrator"),
                    },
                    "test")),
            },
        };
        return controller;
    }
}
