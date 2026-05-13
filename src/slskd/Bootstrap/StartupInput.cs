// <copyright file="StartupInput.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using Serilog;
using Utility.CommandLine;
using Utility.EnvironmentVariables;

public static class StartupInput
{
    public static bool TryPopulate(string environmentVariablePrefix, ILogger log)
    {
        EnvironmentVariables.Populate(prefix: environmentVariablePrefix);

        try
        {
            Arguments.Populate(clearExistingValues: false);
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Invalid command line input: {ex.Message.Replace(".  See inner exception for details.", string.Empty)}");
            return false;
        }
    }
}
