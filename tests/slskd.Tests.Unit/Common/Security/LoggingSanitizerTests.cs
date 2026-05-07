// <copyright file="LoggingSanitizerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security
{
    using System.Net;
    using slskd.Common.Security;
    using Xunit;

    /// <summary>
    ///     Tests for H-GLOBAL01: LoggingSanitizer implementation.
    /// </summary>
    public class LoggingSanitizerTests
    {
        [Fact]
        public void SanitizeFilePath_WithFullPath_PreservesPath()
        {
            // Arrange
            var fullPath = "/home/user/documents/secret.pdf";

            // Act
            var result = LoggingSanitizer.SanitizeFilePath(fullPath);

            // Assert
            Assert.Equal(fullPath, result);
        }

        [Fact]
        public void SanitizeFilePath_WithWindowsPath_PreservesPathAndEscapesBackslashes()
        {
            // Arrange
            var fullPath = @"C:\Users\user\Desktop\confidential.docx";

            // Act
            var result = LoggingSanitizer.SanitizeFilePath(fullPath);

            // Assert
            Assert.Equal(@"C:\\Users\\user\\Desktop\\confidential.docx", result);
        }

        [Fact]
        public void SanitizeFilePath_WithEmptyPath_ReturnsPlaceholder()
        {
            // Act
            var result = LoggingSanitizer.SanitizeFilePath(string.Empty);

            // Assert
            Assert.Equal("[empty]", result);
        }

        [Fact]
        public void SanitizeIpAddress_WithValidIp_PreservesIp()
        {
            // Arrange
            var ip = "192.168.1.100";

            // Act
            var result = LoggingSanitizer.SanitizeIpAddress(ip);

            // Assert
            Assert.Equal(ip, result);
        }

        [Fact]
        public void SanitizeIpAddress_WithIpAddressObject_PreservesIp()
        {
            // Arrange
            var ip = IPAddress.Parse("10.0.0.1");

            // Act
            var result = LoggingSanitizer.SanitizeIpAddress(ip);

            // Assert
            Assert.Equal("10.0.0.1", result);
        }

        [Fact]
        public void SanitizeExternalIdentifier_WithLongIdentifier_PreservesIdentifier()
        {
            // Arrange
            var identifier = "john_doe_12345"; // 14 chars

            // Act
            var result = LoggingSanitizer.SanitizeExternalIdentifier(identifier);

            // Assert
            Assert.Equal(identifier, result);
        }

        [Fact]
        public void SanitizeExternalIdentifier_WithShortIdentifier_PreservesIdentifier()
        {
            // Arrange
            var identifier = "ab";

            // Act
            var result = LoggingSanitizer.SanitizeExternalIdentifier(identifier);

            // Assert
            Assert.Equal(identifier, result);
        }

        [Fact]
        public void SanitizeHash_WithLongHash_PreservesHash()
        {
            // Arrange: 48 chars, first 8 and last 8
            var hash = "a1b2c3d4e5f678901234567890abcdef1234567890abcdef";

            // Act
            var result = LoggingSanitizer.SanitizeHash(hash);

            Assert.Equal(hash, result);
        }

        [Fact]
        public void SanitizeHash_WithShortHash_ReturnsUnchanged()
        {
            // Arrange
            var hash = "abc123";

            // Act
            var result = LoggingSanitizer.SanitizeHash(hash);

            // Assert
            Assert.Equal("abc123", result);
        }

        [Fact]
        public void SanitizeUrl_WithFullUrl_ReturnsSchemeAndHostOnly()
        {
            // Arrange
            var url = "https://api.example.com/users/12345/profile?token=secret";

            // Act
            var result = LoggingSanitizer.SanitizeUrl(url);

            // Assert
            Assert.Equal("https://api.example.com", result);
        }

        [Fact]
        public void SanitizeUrl_WithIpv6Url_PreservesBracketedHost()
        {
            // Arrange
            var url = "https://[::1]:8443/users/12345/profile?token=secret";

            // Act
            var result = LoggingSanitizer.SanitizeUrl(url);

            // Assert
            Assert.Equal("https://[::1]", result);
        }

        [Fact]
        public void SanitizeSensitiveData_WithData_ReturnsRedactedPlaceholder()
        {
            // Arrange
            var data = "super-secret-token-12345"; // 24 chars

            // Act
            var result = LoggingSanitizer.SanitizeSensitiveData(data);

            // Assert
            Assert.Equal("[redacted-24-chars]", result);
        }

        [Fact]
        public void SanitizeQueryText_WithSearchText_PreservesTrimmedSearchText()
        {
            // Arrange
            var query = "private artist unreleased track";

            // Act
            var result1 = LoggingSanitizer.SanitizeQueryText(query);
            var result2 = LoggingSanitizer.SanitizeQueryText($"  {query}  ");

            // Assert
            Assert.Equal(query, result1);
            Assert.Equal(result1, result2);
            Assert.Contains("private", result1, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("artist", result1, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeQueryText_WithEmptyText_ReturnsPlaceholder()
        {
            // Act
            var result = LoggingSanitizer.SanitizeQueryText("   ");

            // Assert
            Assert.Equal("[empty]", result);
        }

        [Fact]
        public void SafeContext_CreatesSafeLoggingObject()
        {
            // Arrange
            var identifier = "sensitive-user-id-123";

            // Act
            var result = LoggingSanitizer.SafeContext("user", identifier);

            // Assert
            Assert.Equal("user", result.Context);
            Assert.Equal(identifier, result.Id);
        }

        [Fact]
        public void Sanitizers_EscapeLogBreakingControlCharacters()
        {
            var result = LoggingSanitizer.SanitizeQueryText("first\r\nsecond\tthird");

            Assert.Equal("first\\r\\nsecond\\tthird", result);
        }
    }
}
