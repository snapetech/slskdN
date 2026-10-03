// <copyright file="SharingOptionsValidationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Sharing;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using slskd;
using Xunit;

public class SharingOptionsValidationTests
{
    [Theory]
    [InlineData("https://shares.example.test/slskd")]
    [InlineData("http://192.168.1.20:5030")]
    public void Validate_AllowsAbsoluteExternalEndpoint(string endpoint)
    {
        var options = new Options.SharingOptions { ExternalEndpoint = endpoint };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("shares.example.test")]
    [InlineData("ftp://shares.example.test")]
    [InlineData("https://user:password@shares.example.test")]
    [InlineData("https://shares.example.test/path?token=value")]
    [InlineData("https://shares.example.test/path#fragment")]
    public void Validate_RejectsInvalidExternalEndpoint(string endpoint)
    {
        var options = new Options.SharingOptions { ExternalEndpoint = endpoint };

        var results = Validate(options);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Options.SharingOptions.ExternalEndpoint)));
    }

    [Theory]
    [InlineData("http://127.0.0.1:5030")]
    [InlineData("https://192.168.1.20:5031")]
    [InlineData("http://[fd00::20]:5030")]
    public void Validate_AllowsExactPrivateOwnerOrigins(string origin)
    {
        var options = new Options.SharingOptions { TrustedPrivateOwnerOrigins = new[] { origin } };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("https://owner.example.test")]
    [InlineData("http://192.168.1.20:5030/api")]
    [InlineData("http://192.168.1.20:5030?token=value")]
    [InlineData("http://169.254.169.254")]
    [InlineData("http://224.0.0.1")]
    [InlineData("http://8.8.8.8")]
    public void Validate_RejectsNonPrivateOrNonOriginOwnerEntries(string origin)
    {
        var options = new Options.SharingOptions { TrustedPrivateOwnerOrigins = new[] { origin } };

        var results = Validate(options);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Options.SharingOptions.TrustedPrivateOwnerOrigins)));
    }

    private static List<ValidationResult> Validate(Options.SharingOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
