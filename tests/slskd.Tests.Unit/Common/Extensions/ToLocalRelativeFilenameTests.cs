// <copyright file="ToLocalRelativeFilenameTests.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
// </copyright>

// <copyright file="ToLocalRelativeFilenameTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Extensions
{
    using System;
    using Xunit;

    public class ToLocalRelativeFilenameTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\t")]
        public void Throws_ArgumentException_Given_Bad_Remote_Filename(string filename)
        {
            var ex = Record.Exception(() => filename.ToLocalRelativeFilename());

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void Returns_Localized_Filename_And_Parent_Directory()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Equal(@"path\file.ext", "deeply/nested/path/file.ext".ToLocalRelativeFilename());
            }
            else
            {
                Assert.Equal(@"path/file.ext", "deeply/nested/path/file.ext".ToLocalRelativeFilename());
            }

        }

        [Fact]
        public void Returns_Just_File_If_Only_File_Given()
        {
            Assert.Equal("file.ext", "file.ext".ToLocalRelativeFilename());
        }

        [Theory]
        [InlineData(@"C:\Users\edvinas\Downloads\Album\Track.flac")]
        [InlineData(@"C:/Users/edvinas/Downloads/Album/Track.flac")]
        [InlineData(@"\Users\edvinas\Downloads\Album\Track.flac")]
        [InlineData(@"/Users/edvinas/Downloads/Album/Track.flac")]
        public void Returns_Localized_Filename_And_Parent_Directory_For_Rooted_Remote_Path(string filename)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Equal(@"Album\Track.flac", filename.ToLocalRelativeFilename());
            }
            else
            {
                Assert.Equal(@"Album/Track.flac", filename.ToLocalRelativeFilename());
            }
        }

        [Fact]
        public void Removes_Invalid_Characters_From_Path_And_Filename()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Assert.Equal(@"p_a_t_h\fi_le.ext", @"p?a|t<h/fi>le.ext".ToLocalRelativeFilename());
            }
            else
            {
                Assert.Equal(@"_", $"{'\0'}".ToLocalRelativeFilename());
            }
        }

        [Theory]
        [InlineData("../file.ext")]
        [InlineData("path/../file.ext")]
        [InlineData(@"C:\..\file.ext")]
        [InlineData(@"C:\path\..\file.ext")]
        public void Throws_ArgumentException_Given_Traversal_Or_Rooted_Path(string filename)
        {
            var ex = Record.Exception(() => filename.ToLocalRelativeFilename());

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }
    }
}
