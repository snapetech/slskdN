// <copyright file="StartupSqlite.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using Serilog;

public static class StartupSqlite
{
    public static void InitOrFailFast(ILogger log)
    {
        // Initialize SQLitePCL provider before any SQLite connection is opened.
        SQLitePCL.Batteries.Init();

        // Check the threading mode set at compile time. If it is 0 it is unsafe to use
        // in a multithreaded application, which slskd is.
        // https://www.sqlite.org/compile.html#threadsafe
        var threadSafe = SQLitePCL.raw.sqlite3_threadsafe();

        if (threadSafe == 0)
        {
            throw new InvalidOperationException($"SQLite binary was not compiled with THREADSAFE={threadSafe}, which is not compatible with this application. Please create a GitHub issue to report this and include details about your environment.");
        }

        log.Debug("SQLite was compiled with THREADSAFE={Mode}", threadSafe);

        if (SQLitePCL.raw.sqlite3_config(SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED) != SQLitePCL.raw.SQLITE_OK)
        {
            throw new InvalidOperationException($"SQLite threading mode could not be set to SERIALIZED ({SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED}). Please create a GitHub issue to report this and include details about your environment.");
        }

        log.Debug("SQLite threading mode set to {Mode} ({Number})", "SERIALIZED", SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED);
    }
}
