// <copyright file="StartupShutdownTelemetry.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using Serilog;

public static class StartupShutdownTelemetry
{
    public static void Install(
        Func<bool> isExpectedShutdown,
        Func<Exception, bool> isBenignUnobservedTaskException,
        Func<Exception, bool> isExpectedSoulseekNetworkException,
        Func<ILogger?> getLog)
    {
        // Install hard telemetry to catch silent exits and unhandled exceptions.
        // This ensures we always know why the process terminated.
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            var expectedShutdown = isExpectedShutdown();
            var msg = expectedShutdown
                ? "ProcessExit event fired during expected shutdown"
                : "[FATAL] ProcessExit event fired - process terminating";
            if (!expectedShutdown)
            {
                Console.Error.WriteLine(msg);
            }

            try
            {
                if (expectedShutdown)
                {
                    getLog()?.Information(msg);
                }
                else
                {
                    getLog()?.Fatal(msg);
                }
            }
            catch
            {
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            var msg = $"[FATAL] Unhandled exception: {ex?.Message ?? e.ExceptionObject?.ToString() ?? "unknown"}";
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine(ex?.StackTrace ?? "no stack trace");
            try
            {
                getLog()?.Fatal(ex, msg);
            }
            catch
            {
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            if (isBenignUnobservedTaskException(e.Exception))
            {
                var msg = $"[WARN] Ignoring benign unobserved task exception: {e.Exception.Message}";
                Console.Error.WriteLine(msg);
                try
                {
                    getLog()?.Warning(e.Exception, msg);
                }
                catch
                {
                }

                e.SetObserved();
                return;
            }

            var baseException = e.Exception.GetBaseException();

            if (isExpectedSoulseekNetworkException(e.Exception))
            {
                var warningMessage = $"Ignoring expected Soulseek peer/distributed network exception: {baseException.Message}";
                try
                {
                    getLog()?.Debug(baseException, warningMessage);
                }
                catch
                {
                }

                e.SetObserved();
                return;
            }

            var fatalMessage = $"[FATAL] Unobserved task exception: {e.Exception.Message}";
            Console.Error.WriteLine(fatalMessage);
            Console.Error.WriteLine(e.Exception.StackTrace);
            try
            {
                getLog()?.Fatal(e.Exception, fatalMessage);
            }
            catch
            {
            }

            e.SetObserved(); // Prevent process termination
        };
    }
}
