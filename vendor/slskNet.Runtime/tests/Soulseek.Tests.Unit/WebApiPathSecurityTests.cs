// <copyright file="WebApiPathSecurityTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using Microsoft.Data.Sqlite;
    using WebAPI;
    using Xunit;

    public class WebApiPathSecurityTests
    {
        [Fact(DisplayName = "Web API path guard accepts paths inside the configured root")]
        public void WebApi_Path_Guard_Accepts_Paths_Inside_Root()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                var file = Path.Combine(root, "music", "track.mp3");
                var resolved = Extensions.GetFullPathInsideRoot(root, file);

                Assert.Equal(Path.GetFullPath(file), resolved);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact(DisplayName = "Web API path guard rejects sibling prefix escapes")]
        public void WebApi_Path_Guard_Rejects_Sibling_Prefix_Escapes()
        {
            var parent = CreateTemporaryDirectory();

            try
            {
                var root = Path.Combine(parent, "share");
                var sibling = Path.Combine(parent, "share-other", "secret.txt");

                Directory.CreateDirectory(root);
                Directory.CreateDirectory(Path.GetDirectoryName(sibling));

                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetFullPathInsideRoot(root, sibling));
            }
            finally
            {
                Directory.Delete(parent, recursive: true);
            }
        }

        [Fact(DisplayName = "Web API output path keeps absolute remote names under the output root")]
        public void WebApi_Output_Path_Keeps_Absolute_Remote_Names_Under_Output_Root()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                var resolved = Extensions.GetSafeOutputPath(root, Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "passwd"));

                Assert.StartsWith(Path.GetFullPath(root), resolved);
                Assert.EndsWith(Path.Combine("etc", "passwd"), resolved);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact(DisplayName = "Web API shared remote path is relative to the configured root")]
        public void WebApi_Shared_Remote_Path_Is_Relative_To_Root()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                var file = Path.Combine(root, "music", "track.mp3");
                var resolved = Extensions.GetSharedRemotePath(root, file);

                Assert.Equal(Path.Combine("music", "track.mp3"), resolved);
                Assert.DoesNotContain(Path.GetFullPath(root), resolved, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact(DisplayName = "Web API shared remote path rejects paths outside the configured root")]
        public void WebApi_Shared_Remote_Path_Rejects_Paths_Outside_Root()
        {
            var parent = CreateTemporaryDirectory();

            try
            {
                var root = Path.Combine(parent, "share");
                var sibling = Path.Combine(parent, "share-other", "secret.txt");

                Directory.CreateDirectory(root);
                Directory.CreateDirectory(Path.GetDirectoryName(sibling));

                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetSharedRemotePath(root, sibling));
            }
            finally
            {
                Directory.Delete(parent, recursive: true);
            }
        }

        [Fact(DisplayName = "Shared file cache advertises relative filenames")]
        public void Shared_File_Cache_Advertises_Relative_Filenames()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                var album = Path.Combine(root, "album");
                Directory.CreateDirectory(album);
                File.WriteAllText(Path.Combine(album, "track.mp3"), "test");

                var cache = new SharedFileCache(root, ttl: 3600000);
                var results = cache.Search(new SearchQuery("track")).ToList();

                var file = Assert.Single(results);
                Assert.Equal(Path.Combine("album", "track.mp3"), file.Filename);
                Assert.DoesNotContain(Path.GetFullPath(root), file.Filename, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact(DisplayName = "Shared file cache disposes previous SQLite connection on refresh")]
        public void Shared_File_Cache_Disposes_Previous_SQLite_Connection_On_Refresh()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                File.WriteAllText(Path.Combine(root, "track.mp3"), "test");

                var cache = new SharedFileCache(root, ttl: 3600000);
                cache.Fill();

                var firstConnection = cache.GetProperty<SqliteConnection>("SQLite");

                cache.Fill();

                Assert.Equal(ConnectionState.Closed, firstConnection.State);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static string CreateTemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
