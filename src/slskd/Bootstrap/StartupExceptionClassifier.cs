// <copyright file="StartupExceptionClassifier.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

public static class StartupExceptionClassifier
{
    public static bool IsBenignUnobservedTaskException(Exception exception)
    {
        var aggregate = exception as AggregateException;
        var exceptions = aggregate != null
            ? aggregate.Flatten().InnerExceptions.ToArray()
            : new[] { exception };

        return exceptions.Length > 0 && exceptions.All(IsBenignUnobservedTaskInnerException);
    }

    private static bool IsBenignUnobservedTaskInnerException(Exception exception)
    {
        return exception is slskd.VPNClientException;
    }
}
