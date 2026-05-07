// <copyright file="LoggingSanitizer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.Core
{
    /// <summary>
    /// Utility for formatting values in logs without hiding operator-visible activity.
    /// </summary>
    public static class LoggingSanitizer
    {
        /// <summary>
        /// Formats a string for logging without hiding operator-visible activity.
        /// </summary>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The string with log-breaking control characters escaped.</returns>
        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return EscapeForLog(value);
        }

        /// <summary>
        /// Formats a file path for logging without hiding directory context.
        /// </summary>
        /// <param name="filePath">The file path to sanitize.</param>
        /// <returns>The path with log-breaking control characters escaped.</returns>
        public static string? SanitizeFilePath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return filePath;
            }

            return EscapeForLog(filePath);
        }

        private static string EscapeForLog(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
