// <copyright file="SecurityMiddlewareLoggingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

public class SecurityMiddlewareLoggingTests
{
    [Fact]
    public async Task InvokeAsync_DoesNotLogRawTargetOrUnescapedRequestPath()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");
        context.Request.Path = new PathString("/mesh/first\r\nforged");
        context.Features.Get<IHttpRequestFeature>()!.RawTarget = "/mesh/first\r\nforged?access_token=private-secret";
        var messages = new List<string>();
        var middleware = new SecurityMiddleware(_ => Task.CompletedTask, new CapturingLogger<SecurityMiddleware>(messages));

        await middleware.InvokeAsync(context);

        var message = string.Join("\n", messages);
        Assert.Contains("/mesh/first\\r\\nforged", message);
        Assert.DoesNotContain("access_token", message);
        Assert.DoesNotContain("private-secret", message);
        Assert.DoesNotContain("first\r\nforged", message);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly ICollection<string> _messages;

        public CapturingLogger(ICollection<string> messages)
        {
            _messages = messages;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, System.Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }
}
