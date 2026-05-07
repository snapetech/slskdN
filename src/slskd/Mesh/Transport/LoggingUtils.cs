// <copyright file="LoggingUtils.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace slskd.Mesh.Transport;

/// <summary>
/// Utilities for log-safe formatting. Operator logs preserve peer IDs and endpoints; only real secrets are redacted.
/// </summary>
public static class LoggingUtils
{
    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "credential", "privatekey", "private_key", "secret", "password", "token", "apikey", "api_key"
    };

    private static readonly Regex SecretAssignmentPattern = new(
        @"(?i)\b(authorization|credential|private[_-]?key|secret|password|token|api[_-]?key)\s*[:=]\s*\S+",
        RegexOptions.Compiled);

    /// <summary>
    /// Logs a message with secret redaction.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="level">The log level.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogSafe<T>(this ILogger<T> logger, LogLevel level, string? message, params object?[] args)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        var safeArgs = RedactSensitiveData(args);
        logger.Log(level, message, safeArgs);
    }

    /// <summary>
    /// Logs a debug message with secret redaction and debug gating.
    /// Only logs in debug builds or when debug logging is explicitly enabled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogDebugSafe<T>(this ILogger<T> logger, string? message, params object?[] args)
    {
        // Only log debug messages if explicitly enabled (not just because logger.IsEnabled(LogLevel.Debug))
        // This prevents accidental leakage of secrets in debug information
#if DEBUG
        var safeArgs = RedactSensitiveData(args);
        logger.LogDebug(message, safeArgs);
#endif
    }

    /// <summary>
    /// Logs a trace message with secret redaction and trace gating.
    /// Only logs in trace builds or when trace logging is explicitly enabled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">The message arguments.</param>
    public static void LogTraceSafe<T>(this ILogger<T> logger, string? message, params object?[] args)
    {
        // Only log trace messages if explicitly enabled
        // Trace often contains the most sensitive debugging information
#if DEBUG
        var safeArgs = RedactSensitiveData(args);
        logger.LogTrace(message, safeArgs);
#endif
    }

    /// <summary>
    /// Formats a peer ID for logging without hiding it.
    /// </summary>
    /// <param name="peerId">The peer ID to log.</param>
    /// <returns>The original peer ID.</returns>
    public static string SafePeerId(string? peerId)
    {
        if (string.IsNullOrEmpty(peerId))
        {
            return "[null]";
        }

        return EscapeForLog(peerId);
    }

    /// <summary>
    /// Formats an IP address or hostname for logging without hiding it.
    /// </summary>
    /// <param name="endpoint">The endpoint to log safely.</param>
    /// <returns>The original endpoint.</returns>
    public static string SafeEndpoint(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            return "[null]";
        }

        return EscapeForLog(endpoint);
    }

    /// <summary>
    /// Formats certificate information without hiding public certificate details.
    /// </summary>
    /// <param name="certificate">The certificate to log safely.</param>
    /// <returns>The certificate subject and thumbprint.</returns>
    public static string SafeCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2? certificate)
    {
        if (certificate == null)
        {
            return "[null]";
        }

        var thumbprint = certificate.Thumbprint;

        if (string.IsNullOrEmpty(thumbprint))
        {
            return $"[cert:{certificate.Subject}]";
        }

        return $"[cert:{certificate.Subject}; thumbprint:{thumbprint}]";
    }

    /// <summary>
    /// Formats transport endpoint information.
    /// </summary>
    /// <param name="endpoint">The transport endpoint.</param>
    /// <returns>The original endpoint details.</returns>
    public static string SafeTransportEndpoint(TransportEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return "[null]";
        }

        var safeHost = SafeEndpoint(endpoint.Host);
        return $"{endpoint.TransportType}:{safeHost}:{endpoint.Port}";
    }

    /// <summary>
    /// Redacts secret data from logging arguments.
    /// </summary>
    /// <param name="args">The arguments to redact.</param>
    /// <returns>The arguments with secrets redacted.</returns>
    private static object?[] RedactSensitiveData(object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Array.Empty<object?>();
        }

        var redacted = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            redacted[i] = RedactValue(args[i]);
        }

        return redacted;
    }

    /// <summary>
    /// Redacts a single value if it contains secret material.
    /// </summary>
    /// <param name="value">The value to redact.</param>
    /// <returns>The redacted value.</returns>
    private static object? RedactValue(object? value)
    {
        if (value == null)
        {
            return value;
        }

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
        {
            return value;
        }

        // Check if this contains explicit secret material. Do not redact ordinary hashes,
        // peer IDs, endpoints, usernames, paths, or search text.
        var lowerValue = stringValue.ToLowerInvariant();

        foreach (var keyword in SensitiveKeywords)
        {
            if (lowerValue == keyword || lowerValue.StartsWith($"{keyword}=", StringComparison.Ordinal) || lowerValue.StartsWith($"{keyword}:", StringComparison.Ordinal))
            {
                return "[redacted]";
            }
        }

        if (SecretAssignmentPattern.IsMatch(stringValue))
        {
            return SecretAssignmentPattern.Replace(stringValue, match =>
            {
                var separator = match.Value.Contains('=') ? "=" : ":";
                var key = match.Value.Split(separator[0], 2)[0].Trim();
                return $"{key}{separator}[redacted]";
            });
        }

        return EscapeForLog(stringValue);
    }

    /// <summary>
    /// Creates a log-safe exception message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The exception type and message after secret redaction.</returns>
    public static string SafeException(Exception? exception)
    {
        if (exception == null)
        {
            return "[null]";
        }

        // Redact sensitive information from exception messages
        var message = RedactValue(exception.Message)?.ToString() ?? "Unknown error";

        // Include exception type but not full stack trace (too verbose)
        return $"{exception.GetType().Name}: {message}";
    }

    /// <summary>
    /// Logs connection establishment with operator-visible information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="transportType">The transport type.</param>
    public static void LogConnectionEstablished<T>(this ILogger<T> logger, string peerId, string endpoint, TransportType transportType)
    {
        logger.LogInformation("Connection established to peer {PeerId} via {Transport} at {Endpoint}",
            SafePeerId(peerId), transportType, SafeEndpoint(endpoint));
    }

    /// <summary>
    /// Logs connection failure with operator-visible information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="error">The error message.</param>
    public static void LogConnectionFailed<T>(this ILogger<T> logger, string peerId, string endpoint, string error)
    {
        logger.LogWarning("Connection failed to peer {PeerId} at {Endpoint}: {Error}",
            SafePeerId(peerId), SafeEndpoint(endpoint), SafeException(new Exception(error)));
    }

    /// <summary>
    /// Logs certificate validation with operator-visible information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="peerId">The peer ID.</param>
    /// <param name="certificate">The certificate.</param>
    /// <param name="isValid">Whether the certificate is valid.</param>
    public static void LogCertificateValidation<T>(this ILogger<T> logger, string peerId, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, bool isValid)
    {
        var level = isValid ? LogLevel.Debug : LogLevel.Warning;
        logger.Log(level, "Certificate validation for peer {PeerId}: {Certificate} - {Result}",
            SafePeerId(peerId), SafeCertificate(certificate), isValid ? "valid" : "invalid");
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
