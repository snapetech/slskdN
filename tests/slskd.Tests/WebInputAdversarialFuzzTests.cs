// <copyright file="WebInputAdversarialFuzzTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class WebInputAdversarialFuzzTests
{
    [Fact]
    public async Task MalformedJsonLoginBodies_ReturnClientErrorsWithoutUnhandledExceptions()
    {
        using var factory = new ModelStateTestHostFactory();
        using var client = factory.CreateClient();

        foreach (var body in MalformedJsonBodies())
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/api/v0/session", content);

            Assert.True(
                response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType,
                $"Unexpected status {(int)response.StatusCode} for payload {body}");
        }
    }

    [Fact]
    public async Task RandomByteLoginBodies_ReturnClientErrorsWithoutUnhandledExceptions()
    {
        using var factory = new ModelStateTestHostFactory();
        using var client = factory.CreateClient();
        var random = new Random(0x51_5A_4B_44);

        for (var index = 0; index < 32; index++)
        {
            var bytes = new byte[random.Next(1, 96)];
            random.NextBytes(bytes);
            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await client.PostAsync("/api/v0/session", content);

            Assert.True(
                response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnsupportedMediaType,
                $"Unexpected status {(int)response.StatusCode} for byte corpus item {index}");
        }
    }

    [Fact]
    public async Task HostileQueryAndPathInputs_ReturnDocumentedHttpResponses()
    {
        using var factory = new NoAuthTestHostFactory();
        using var client = factory.CreateClient();

        foreach (var value in HostileStrings())
        {
            var encoded = Uri.EscapeDataString(value);

            using var queryResponse = await client.GetAsync($"/api/v0/session/enabled?next={encoded}");
            Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

            if (value.Contains('\0', StringComparison.Ordinal))
            {
                continue;
            }

            using var pathResponse = await client.GetAsync($"/api/v0/session/{encoded}");
            Assert.True(
                pathResponse.StatusCode is HttpStatusCode.BadRequest
                    or HttpStatusCode.Unauthorized
                    or HttpStatusCode.NotFound
                    or HttpStatusCode.MethodNotAllowed,
                $"Unexpected status {(int)pathResponse.StatusCode} for path corpus item {value}");
        }
    }

    private static IEnumerable<string> MalformedJsonBodies()
    {
        yield return string.Empty;
        yield return "{";
        yield return "[";
        yield return "\"unterminated";
        yield return "{\"username\":";
        yield return "{\"username\":\"alice\",\"password\":";
        yield return "{\0}";
        yield return "{\"username\":[\"alice\"],\"password\":{}}";
        yield return "{\"username\":\"alice\",\"password\":\"secret\",\"extra\":";
    }

    private static IEnumerable<string> HostileStrings()
    {
        yield return string.Empty;
        yield return "../";
        yield return "..%2f..%2f";
        yield return "\0";
        yield return "\uD800";
        yield return "' OR 1=1 --";
        yield return "<script>alert(1)</script>";
        yield return new string('A', 512);
    }
}
