// <copyright file="DiagnosticFileNameTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Client
{
    using Xunit;

    public class DiagnosticFileNameTests
    {
        [Trait("Category", "Diagnostics")]
        [Theory(DisplayName = "GetDiagnosticLogValue preserves operator-visible text")]
        [InlineData(@"C:\Users\alice\Music\secret.mp3", @"C:\\Users\\alice\\Music\\secret.mp3")]
        [InlineData("/home/alice/Music/secret.mp3", "/home/alice/Music/secret.mp3")]
        [InlineData(@"@@alias\folder\secret.mp3", @"@@alias\\folder\\secret.mp3")]
        [InlineData("folder/secret.mp3", "folder/secret.mp3")]
        [InlineData("secret.mp3", "secret.mp3")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void GetDiagnosticLogValue_Preserves_Operator_Visible_Text(string filename, string expected)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var actual = s.InvokeMethod<string>("GetDiagnosticLogValue", filename);

                Assert.Equal(expected, actual);
            }
        }

        [Trait("Category", "Diagnostics")]
        [Fact(DisplayName = "GetDiagnosticLogValue escapes log-breaking control characters")]
        public void GetDiagnosticLogValue_Escapes_Log_Breaking_Control_Characters()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var actual = s.InvokeMethod<string>("GetDiagnosticLogValue", "first\r\nsecond\tthird");

                Assert.Equal("first\\r\\nsecond\\tthird", actual);
            }
        }

        [Trait("Category", "Diagnostics")]
        [Fact(DisplayName = "GetDiagnosticSearchDescription includes search text")]
        public void GetDiagnosticSearchDescription_Includes_Search_Text()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var query = new SearchQuery(new[] { "private", "phrase" }, new[] { "excluded" });
                var actual = s.InvokeMethod<string>("GetDiagnosticSearchDescription", query, 42);

                Assert.Equal("token 42, query \"private phrase -excluded\"", actual);
                Assert.Contains("private", actual);
                Assert.Contains("phrase", actual);
                Assert.Contains("excluded", actual);
            }
        }
    }
}
