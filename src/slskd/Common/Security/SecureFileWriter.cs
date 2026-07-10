// <copyright file="SecureFileWriter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security;

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Opens output files beneath a trusted root without following filesystem links.
/// </summary>
public static class SecureFileWriter
{
    private const int OpenReadOnly = 0;
    private const int OpenWriteOnly = 1;
    private const int OpenCreate = 0x40;
    private const int OpenTruncate = 0x200;
    private const int OpenCloseOnExec = 0x80000;
    private const int OpenNoFollow = 0x20000;
    private const int OpenDirectory = 0x10000;
    private const int ErrorNoEntry = 2;
    private const int DirectoryMode = 0x1c0; // 0700
    private const int FileMode = 0x180; // 0600

    public static FileStream Open(string path, string trustedRoot)
    {
        var safePath = PathGuard.NormalizeAbsolutePathWithinRoots(path, new[] { trustedRoot })
            ?? throw new UnauthorizedAccessException("Output path is outside the trusted root.");
        var safeRoot = PathGuard.ResolveExistingPathComponents(trustedRoot);

        return OperatingSystem.IsLinux()
            ? OpenLinux(safePath, safeRoot)
            : OpenWithReparsePointChecks(safePath, safeRoot);
    }

    private static FileStream OpenLinux(string safePath, string safeRoot)
    {
        var relative = Path.GetRelativePath(safeRoot, safePath);
        var components = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (components.Length == 0 || components.Any(component => component is "." or ".."))
        {
            throw new UnauthorizedAccessException("Invalid output path.");
        }

        using var rootHandle = OpenHandle(
            safeRoot,
            OpenReadOnly | OpenDirectory | OpenNoFollow | OpenCloseOnExec);
        SafeFileHandle currentHandle = rootHandle;
        SafeFileHandle? ownedDirectoryHandle = null;
        try
        {
            foreach (var component in components[..^1])
            {
                var nextFd = NativeMethods.OpenAt(
                    currentHandle.DangerousGetHandle().ToInt32(),
                    component,
                    OpenReadOnly | OpenDirectory | OpenNoFollow | OpenCloseOnExec,
                    0);
                if (nextFd < 0 && Marshal.GetLastPInvokeError() == ErrorNoEntry)
                {
                    if (NativeMethods.MakeDirectoryAt(currentHandle.DangerousGetHandle().ToInt32(), component, DirectoryMode) != 0)
                    {
                        throw CreateNativeException("Failed to create output directory");
                    }

                    nextFd = NativeMethods.OpenAt(
                        currentHandle.DangerousGetHandle().ToInt32(),
                        component,
                        OpenReadOnly | OpenDirectory | OpenNoFollow | OpenCloseOnExec,
                        0);
                }

                if (nextFd < 0)
                {
                    throw CreateNativeException("Output directory is unavailable or is a symbolic link");
                }

                ownedDirectoryHandle?.Dispose();
                ownedDirectoryHandle = new SafeFileHandle((IntPtr)nextFd, ownsHandle: true);
                currentHandle = ownedDirectoryHandle;
            }

            var fileFd = NativeMethods.OpenAt(
                currentHandle.DangerousGetHandle().ToInt32(),
                components[^1],
                OpenWriteOnly | OpenCreate | OpenTruncate | OpenNoFollow | OpenCloseOnExec,
                FileMode);
            if (fileFd < 0)
            {
                throw CreateNativeException("Output file is unavailable or is a symbolic link");
            }

            SafeFileHandle? fileHandle = null;
            try
            {
                fileHandle = new SafeFileHandle((IntPtr)fileFd, ownsHandle: true);
                var stream = new FileStream(fileHandle, FileAccess.Write);
                fileHandle = null;
                return stream;
            }
            finally
            {
                fileHandle?.Dispose();
            }
        }
        finally
        {
            ownedDirectoryHandle?.Dispose();
        }
    }

    private static FileStream OpenWithReparsePointChecks(string safePath, string safeRoot)
    {
        var relative = Path.GetRelativePath(safeRoot, safePath);
        var current = safeRoot;
        foreach (var component in relative.Split(
                     new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                     StringSplitOptions.RemoveEmptyEntries)[..^1])
        {
            current = Path.Combine(current, component);
            if (Directory.Exists(current) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new UnauthorizedAccessException("Output directory is a reparse point.");
            }

            Directory.CreateDirectory(current);
        }

        if (File.Exists(safePath) && File.GetAttributes(safePath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new UnauthorizedAccessException("Output file is a reparse point.");
        }

        return new FileStream(safePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None);
    }

    private static SafeFileHandle OpenHandle(string path, int flags)
    {
        var fd = NativeMethods.Open(path, flags, 0);
        return fd >= 0
            ? new SafeFileHandle((IntPtr)fd, ownsHandle: true)
            : throw CreateNativeException("Failed to open trusted output root");
    }

    private static Win32Exception CreateNativeException(string message) =>
        new(Marshal.GetLastPInvokeError(), message);

    private static class NativeMethods
    {
        [DllImport("libc", EntryPoint = "open", SetLastError = true)]
        internal static extern int Open(string path, int flags, int mode);

        [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
        internal static extern int OpenAt(int directoryFd, string path, int flags, int mode);

        [DllImport("libc", EntryPoint = "mkdirat", SetLastError = true)]
        internal static extern int MakeDirectoryAt(int directoryFd, string path, int mode);
    }
}
