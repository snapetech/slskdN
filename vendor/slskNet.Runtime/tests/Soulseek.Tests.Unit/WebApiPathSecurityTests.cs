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
    using System.IO;
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

        private static string CreateTemporaryDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
