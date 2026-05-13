// <copyright file="GroupsOptionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Core;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

public class GroupsOptionsTests
{
    [Fact]
    public void Validate_WhenUserAppearsInTwoUserDefinedGroups_ReturnsValidationError()
    {
        var options = new slskd.Options.GroupsOptions
        {
            UserDefined = new Dictionary<string, slskd.Options.GroupsOptions.UserDefinedOptions>
            {
                ["lossless"] = new() { Members = ["alice"] },
                ["trusted"] = new() { Members = ["ALICE"] },
            },
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.Contains(results, result => result.ErrorMessage?.Contains("alice", System.StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Validate_WhenUserAppearsInUserDefinedAndBlacklist_ReturnsValidationError()
    {
        var options = new slskd.Options.GroupsOptions
        {
            Blacklisted = new slskd.Options.GroupsOptions.BlacklistedOptions { Members = ["bob"] },
            UserDefined = new Dictionary<string, slskd.Options.GroupsOptions.UserDefinedOptions>
            {
                ["trusted"] = new() { Members = [" bob "] },
            },
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.Contains(results, result => result.ErrorMessage?.Contains("bob", System.StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Validate_WhenMembershipsAreUnique_ReturnsNoValidationError()
    {
        var options = new slskd.Options.GroupsOptions
        {
            Blacklisted = new slskd.Options.GroupsOptions.BlacklistedOptions { Members = ["mallory"] },
            UserDefined = new Dictionary<string, slskd.Options.GroupsOptions.UserDefinedOptions>
            {
                ["lossless"] = new() { Members = ["alice"] },
                ["trusted"] = new() { Members = ["bob"] },
            },
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.DoesNotContain(results, result => result.ErrorMessage?.Contains("multiple groups", System.StringComparison.OrdinalIgnoreCase) == true);
    }
}
