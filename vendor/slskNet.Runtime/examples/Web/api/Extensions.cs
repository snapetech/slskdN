namespace WebAPI
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    ///     Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Converts the given path to the local format (normalizes path separators).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ToLocalOSPath(this string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string GetFullPathInsideRoot(string root, string path)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Root path is missing or invalid", nameof(root));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is missing or invalid", nameof(path));
            }

            var rootPath = NormalizeRootPath(root);
            var localPath = path.ToLocalOSPath();
            var fullPath = Path.IsPathRooted(localPath)
                ? Path.GetFullPath(localPath)
                : Path.GetFullPath(Path.Combine(rootPath, localPath));

            if (!IsPathInsideRoot(rootPath, fullPath))
            {
                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured root");
            }

            return fullPath;
        }

        public static string GetSafeOutputPath(string root, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is missing or invalid", nameof(path));
            }

            var rootPath = NormalizeRootPath(root);
            var relativePath = ToSafeRelativePath(path);
            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));

            if (!IsPathInsideRoot(rootPath, fullPath))
            {
                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured output directory");
            }

            return fullPath;
        }

        public static string GetSharedRemotePath(string root, string path)
        {
            var rootPath = NormalizeRootPath(root);
            var fullPath = GetFullPathInsideRoot(rootPath, path);
            var relativePath = Path.GetRelativePath(rootPath, fullPath);

            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
            {
                throw new ArgumentException("Path does not contain a usable shared name", nameof(path));
            }

            return relativePath.ToLocalOSPath().TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        ///     Returns the directory from the given path, regardless of separator format.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string DirectoryName(this string path)
        {
            var separator = path.Contains('\\') ? '\\' : '/';
            var parts = path.Split(separator);
            return string.Join(separator, parts.Take(parts.Length - 1));
        }

        private static bool IsPathInsideRoot(string normalizedRoot, string fullPath)
        {
            var comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var normalizedPath = Path.GetFullPath(fullPath);
            var rootWithoutSeparator = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar);

            return string.Equals(normalizedPath, rootWithoutSeparator, comparison) ||
                normalizedPath.StartsWith(normalizedRoot, comparison);
        }

        private static string NormalizeRootPath(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Root path is missing or invalid", nameof(root));
            }

            var fullPath = Path.GetFullPath(root.ToLocalOSPath());
            return fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        private static string ToSafeRelativePath(string path)
        {
            var localPath = path.ToLocalOSPath();
            var parts = localPath
                .Split(Path.DirectorySeparatorChar)
                .Where(part => !string.IsNullOrWhiteSpace(part) && part != "." && part != "..")
                .Select(SanitizePathPart)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            if (parts.Length == 0)
            {
                throw new ArgumentException("Path does not contain a usable file name", nameof(path));
            }

            return Path.Combine(parts);
        }

        private static string SanitizePathPart(string part)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                part = part.Replace(c, '_');
            }

            return part;
        }

        /// <summary>
        ///     Returns the SHA1 hash of the given string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha1(this string str)
        {
#pragma warning disable SYSLIB0021 // Type or member is obsolete

            using var sha1 = new SHA1Managed();
#pragma warning restore SYSLIB0021 // Type or member is obsolete

            return BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", "");
        }

        /// <summary>
        ///     Formats byte to nearest size (KB, MB, etc.)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static string SizeSuffix(this double value, int decimalPlaces = 1)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
