// <copyright file="OptionsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd;
using slskd.Core.API;
using Xunit;

public class OptionsControllerTests
{
    private static readonly HashSet<string> CredentialPropertyNames = new(StringComparer.Ordinal)
    {
        "AccessToken",
        "ApiKey",
        "ClientSecret",
        "Password",
        "Secret",
        "Token",
        "TokenSigningKey",
    };

    [Fact]
    public void Options_AllCredentialPropertiesAreMarkedSecret()
    {
        var unprotectedCredentials = GetTypeTree(typeof(slskd.Options))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.PropertyType == typeof(string))
            .Where(property => CredentialPropertyNames.Contains(property.Name))
            .Where(property => property.GetCustomAttribute<SecretAttribute>() == null)
            .Select(property => $"{property.DeclaringType?.FullName}.{property.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unprotectedCredentials);
    }

    [Fact]
    public void Current_RedactsSharingTokenSigningKey()
    {
        const string signingKey = "do-not-return-this-signing-key";
        var options = new slskd.Options
        {
            Sharing = new slskd.Options.SharingOptions
            {
                TokenSigningKey = signingKey,
            },
        };
        var controller = CreateController(options);

        var result = Assert.IsType<OkObjectResult>(controller.Current());
        var redacted = Assert.IsType<slskd.Options>(result.Value);

        Assert.Equal("*****", redacted.Sharing.TokenSigningKey);
        Assert.Equal(signingKey, options.Sharing.TokenSigningKey);
    }

    [Fact]
    public void Current_RedactsEveryWebhookHeaderValue()
    {
        const string authorization = "EXAMPLE_AUTHORIZATION_VALUE";
        const string customValue = "EXAMPLE_CUSTOM_HEADER_VALUE";
        var options = new slskd.Options
        {
            Integration = new slskd.Options.IntegrationOptions
            {
                Webhooks = new Dictionary<string, slskd.Options.IntegrationOptions.WebhookOptions>
                {
                    ["example"] = new()
                    {
                        Call = new()
                        {
                            Url = "https://example.invalid/webhook",
                            Headers =
                            [
                                new() { Name = "Authorization", Value = authorization },
                                new() { Name = "X-Custom-Metadata", Value = customValue },
                            ],
                        },
                    },
                },
            },
        };
        var controller = CreateController(options);

        var result = Assert.IsType<OkObjectResult>(controller.Current());
        var redacted = Assert.IsType<slskd.Options>(result.Value);
        var headers = redacted.Integration.Webhooks["example"].Call.Headers;

        Assert.All(headers, header => Assert.Equal("*****", header.Value));
        Assert.Equal(authorization, options.Integration.Webhooks["example"].Call.Headers[0].Value);
        Assert.Equal(customValue, options.Integration.Webhooks["example"].Call.Headers[1].Value);
    }

    [Fact]
    public void Current_RedactsLegacyBridgePassword()
    {
        const string password = "EXAMPLE_BRIDGE_PASSWORD";
        var options = new slskd.Options
        {
            VirtualSoulfind = new slskd.Core.VirtualSoulfindOptions
            {
                Bridge = new slskd.VirtualSoulfind.Bridge.BridgeOptions
                {
                    Password = password,
                },
            },
        };
        var controller = CreateController(options);

        var result = Assert.IsType<OkObjectResult>(controller.Current());
        var redacted = Assert.IsType<slskd.Options>(result.Value);

        Assert.Equal("*****", redacted.VirtualSoulfind?.Bridge?.Password);
        Assert.Equal(password, options.VirtualSoulfind.Bridge?.Password);
    }

    [Fact]
    public void ApplyOverlay_WithNullBody_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.ApplyOverlay(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Options overlay is required", badRequest.Value);
    }

    [Fact]
    public void ValidateYamlFile_WithNullBody_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.ValidateYamlFile(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("YAML is required", badRequest.Value);
    }

    [Fact]
    public void ValidateYamlFile_WithMalformedYaml_DoesNotLeakParserExceptionMessage()
    {
        var controller = CreateController();

        var result = controller.ValidateYamlFile(":\n  : bad");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Invalid YAML configuration", ok.Value);
    }

    [Fact]
    public void ValidateYamlFile_WithInvalidOptions_DoesNotLeakValidationMessage()
    {
        var controller = CreateController();

        var result = controller.ValidateYamlFile("soulseek:\n  listenPort: 1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.DoesNotContain("1024", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Invalid YAML configuration", ok.Value);
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("100.1")]
    public void ValidateYamlFile_WithOutOfRangeAutoRetryTolerance_ReturnsInvalidConfiguration(string tolerance)
    {
        var controller = CreateController();
        var yaml = $"transfers:\n  download:\n    auto_retry:\n      alternate_source_size_tolerance_percent: {tolerance}";

        var result = controller.ValidateYamlFile(yaml);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Invalid YAML configuration", ok.Value);
    }

    [Fact]
    public void ApplyOverlay_WithInvalidOverlay_DoesNotLeakValidationMessage()
    {
        var controller = CreateController();

        var result = controller.ApplyOverlay(new OptionsOverlay
        {
            Soulseek = new OptionsOverlay.SoulseekOptionsPatch
            {
                ListenPort = 1
            }
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.DoesNotContain("1024", badRequest.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Invalid options overlay", badRequest.Value);
    }

    private static OptionsController CreateController(bool remoteConfiguration = true)
    {
        return CreateController(new slskd.Options
        {
            RemoteConfiguration = remoteConfiguration,
        });
    }

    private static OptionsController CreateController(slskd.Options options)
    {

        var optionsSnapshot = new Mock<IOptionsSnapshot<slskd.Options>>();
        optionsSnapshot.SetupGet(snapshot => snapshot.Value).Returns(options);

        var stateMutator = new Mock<IStateMutator<State>>();

        return new OptionsController(
            new OptionsAtStartup(),
            optionsSnapshot.Object,
            stateMutator.Object);
    }

    private static IEnumerable<Type> GetTypeTree(Type root) =>
        root.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTypeTree)
            .Prepend(root);
}
