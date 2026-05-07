// <copyright file="LoggingSanitizer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security
{
    using System;
    using System.Net;
    /// <summary>
    ///     Provides logging utilities that preserve operator-visible activity while keeping actual secrets out of logs.
    /// </summary>
    /// <remarks>
    ///     H-GLOBAL01: Logging and Telemetry Hygiene Audit. Runtime and operator logs should show usernames, paths,
    ///     search terms, peer IDs, endpoints, and hashes because those values are needed to troubleshoot a running node.
    ///     Only credentials, API keys, private keys, passwords, and equivalent secret material should be redacted.
    /// </remarks>
    public static class LoggingSanitizer
    {
        /// <summary>
        ///     Formats a file path for logging without hiding operator-visible path context.
        /// </summary>
        /// <param name="path">The full file path.</param>
        /// <returns>The path with log-breaking control characters escaped.</returns>
        /// <example>
        ///     "/home/user/documents/file.pdf" → "/home/user/documents/file.pdf"
        ///     "C:\Users\user\Desktop\file.docx" → "C:\\Users\\user\\Desktop\\file.docx"
        /// </example>
        public static string SanitizeFilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "[empty]";
            }

            try
            {
                return EscapeForLog(path);
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        ///     Formats an IP address for logging without hiding endpoint context.
        /// </summary>
        /// <param name="ipAddress">The IP address to sanitize.</param>
        /// <returns>The IP address with log-breaking control characters escaped.</returns>
        /// <example>
        ///     "192.168.1.100" → "192.168.1.100"
        /// </example>
        public static string SanitizeIpAddress(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return "[empty]";
            }

            try
            {
                return EscapeForLog(ipAddress);
            }
            catch
            {
                return ipAddress;
            }
        }

        /// <summary>
        ///     Formats an IP address for logging without hiding endpoint context.
        /// </summary>
        /// <param name="ipAddress">The IP address to sanitize.</param>
        /// <returns>The IP address with log-breaking control characters escaped.</returns>
        public static string SanitizeIpAddress(IPAddress? ipAddress)
        {
            if (ipAddress == null)
            {
                return "[null]";
            }

            return SanitizeIpAddress(ipAddress.ToString());
        }

        /// <summary>
        ///     Formats a username or external identifier for logging without hiding it.
        /// </summary>
        /// <param name="identifier">The username or external identifier.</param>
        /// <returns>The identifier with log-breaking control characters escaped.</returns>
        /// <example>
        ///     "john_doe_12345" → "john_doe_12345"
        /// </example>
        public static string SanitizeExternalIdentifier(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return "[empty]";
            }

            return EscapeForLog(identifier);
        }

        /// <summary>
        ///     Formats a content hash for logging without truncating it.
        /// </summary>
        /// <param name="hash">The full hash string.</param>
        /// <returns>The hash with log-breaking control characters escaped.</returns>
        /// <example>
        ///     "a1b2c3d4e5f678901234567890abcdef1234567890abcdef" → "a1b2c3d4e5f678901234567890abcdef1234567890abcdef"
        /// </example>
        public static string SanitizeHash(string? hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return "[empty]";
            }

            return EscapeForLog(hash);
        }

        /// <summary>
        ///     Formats user-supplied search text or metadata values for logging.
        /// </summary>
        /// <param name="value">The search text or metadata value.</param>
        /// <returns>The original value with log-breaking control characters escaped.</returns>
        public static string SanitizeQueryText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "[empty]";
            }

            var normalized = value.Trim();

            return EscapeForLog(normalized);
        }

        /// <summary>
        ///     Sanitizes a URL for safe logging by removing sensitive components.
        /// </summary>
        /// <param name="url">The full URL.</param>
        /// <returns>A sanitized URL showing only scheme and hostname.</returns>
        /// <example>
        ///     "https://api.example.com/users/12345/profile?token=secret" → "https://api.example.com"
        /// </example>
        public static string SanitizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "[empty]";
            }

            try
            {
                var uri = new Uri(url);
                var host = uri.HostNameType == UriHostNameType.IPv6 && !uri.Host.StartsWith("[", StringComparison.Ordinal)
                    ? $"[{uri.Host}]"
                    : uri.Host;
                return $"{uri.Scheme}://{host}";
            }
            catch
            {
                return "[invalid-url]";
            }
        }

        /// <summary>
        ///     Sanitizes arbitrary sensitive data by replacing it with a placeholder.
        /// </summary>
        /// <param name="data">The sensitive data.</param>
        /// <returns>A placeholder indicating sensitive data was present.</returns>
        public static string SanitizeSensitiveData(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return "[empty]";
            }

            return $"[redacted-{data.Length}-chars]";
        }

        private static string EscapeForLog(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        /// <summary>
        ///     Creates a safe logging context that can be used with structured logging.
        /// </summary>
        /// <param name="contextName">Name of the context (e.g., "user", "file", "peer").</param>
        /// <param name="identifier">The identifier to sanitize.</param>
        /// <returns>A safe context object for logging.</returns>
        public static LoggingSafeContext SafeContext(string contextName, string identifier)
        {
            return new LoggingSafeContext(contextName, SanitizeExternalIdentifier(identifier));
        }
    }

    /// <summary>
    ///     Safe context for structured logging (avoids anonymous type for testability).
    /// </summary>
    public sealed record LoggingSafeContext(string Context, string Id);
}
