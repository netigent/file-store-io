using Netigent.Utils.FileStoreIO.Constants;
using System;
using System.IO;

namespace Netigent.Utils.FileStoreIO.Extensions
{
    public static class IoExtensions
    {
        /// <summary>
        /// Determines if a path is absolute across Windows, UNC, Linux, and Mac filesystems.
        /// Paths with environment variables (like %USERPROFILE%) or tilde (~) that start from
        /// root locations are considered absolute as they resolve to absolute paths.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is absolute, false if relative</returns>
        public static bool IsAbsolutePath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            path = path.TrimStart();

            // Tilde paths (~/ or ~\) are absolute (Unix/Linux/Mac home directory)
            // But bare ~ or ~user are relative
            if (path.StartsWith("~/") || path.StartsWith(@"~\"))
                return true;

            // Windows environment variables at the start (%VAR%\)
            if (path.StartsWith("%") && path.Contains("%\\"))
                return true;
            if (path.StartsWith("%") && path.Contains("%/"))
                return true;

            // UNC paths (\\server\share or //server/share)
            if (path.StartsWith(@"\\") || path.StartsWith("//"))
                return true;

            // Windows absolute paths (C:\, D:\, etc.) - must have separator after colon
            if (path.Length >= 3 &&
                char.IsLetter(path[0]) &&
                path[1] == ':' &&
                (path[2] == '\\' || path[2] == '/'))
                return true;

            // Windows paths with drive letter and environment variable (C:\%VAR%\)
            // Must have drive letter, colon, separator, then content
            if (path.Length >= 4 &&
                char.IsLetter(path[0]) &&
                path[1] == ':' &&
                (path[2] == '\\' || path[2] == '/'))
                return true;

            // Unix/Linux/Mac absolute paths (starts with /)
            // But not if it contains $ variable syntax
            if (path.StartsWith("/") && !path.Contains("$"))
                return true;

            // Windows extended-length paths (\\?\C:\)
            if (path.StartsWith(@"\\?\"))
                return true;

            // Windows device paths (\\.\)
            if (path.StartsWith(@"\\.\"))
                return true;

            return false;
        }

        /// <summary>
        /// Takes a path and if relative, attempts to ensure appPrefix and rewrite seperators and add to root.
        /// </summary>
        /// <param name="currentPath">The path, e.g. "Training/Sales/Outreach", "Training\Sales\Outreach\A123", "" etc</param>
        /// <param name="rootFolder">The root directory e.g. "c:\temp\", "\\server\myfolder", "~/documents", "%USERPROFILE%\Documents\" etc</param>
        /// <param name="includePrefix">The appPrefix e.g. "CustomerA" or "" etc</param>
        /// <returns></returns>
        public static string ToAbsolutePath(this string currentPath, string rootFolder, string includePrefix = "")
        {
            if (IoExtensions.IsAbsolutePath(currentPath))
            {
                return currentPath;
            }

            // Remove trailing separators from root to avoid doubles
            rootFolder = rootFolder.TrimEnd('\\', '/', '|');

            // If Windows or UNC this will be \ otherwise / (i.e. Mac. Linux etc)
            char seperator = GetPathSeparator(rootFolder);

            // Ensure path has correct terminator
            currentPath = currentPath
                .ToRelativeFile(includePrefix: includePrefix, useRelativeRoot: string.Empty, useSeperator: seperator)
                .ReplaceSeperators(seperator);

            // 1. Strip prefix if it exists (case-insensitive)
            if (includePrefix.Length > 0 && currentPath.StartsWith(includePrefix, StringComparison.OrdinalIgnoreCase))
            {
                currentPath = currentPath.Substring((includePrefix).Length);
            }

            // 2. Now append prefix if specified
            string relativePath = includePrefix.Length > 0
                ? ReplaceSeperators($"{includePrefix}{seperator}{currentPath}", seperator)
                : ReplaceSeperators(currentPath, seperator);

            // Handle empty path case
            if (string.IsNullOrEmpty(relativePath))
                return rootFolder;

            return string.Join(seperator.ToString(), [rootFolder, relativePath]);
        }

        /// <summary>
        /// Takes a path and if relative, attempts to ensure appPrefix and rewrite seperators and add to root.
        /// </summary>
        /// <param name="currentPath">The path, e.g. "Training/Sales/Outreach", "Training\Sales\Outreach\A123", "" etc</param>
        /// <param name="root">The root directory e.g. "c:\temp\", "\\server\myfolder", "~/documents", "%USERPROFILE%\Documents\" etc</param>
        /// <param name="includePrefix">The appPrefix e.g. "CustomerA" or "" etc</param>
        /// <returns></returns>
        public static string ToRelativeFolder(this string currentPath, string includePrefix = "", string useRelativeRoot = "./", char useSeperator = SystemConstants.InternalDirectorySeparator)
        {
            string x = ToRelativeFile(
                currentPath: Path.GetDirectoryName(currentPath.ReplaceSeperators(useSeperator)) + "",
                includePrefix: includePrefix,
                useRelativeRoot,
                useSeperator);

            return string.IsNullOrEmpty(x)
                ? useRelativeRoot
                : $"{x.TrimEnd('\\', '|', '/')}{useSeperator}";
        }


        /// <summary>
        /// Takes a path and if relative, attempts to ensure appPrefix and rewrite seperators and add to root.
        /// </summary>
        /// <param name="currentPath">The path, e.g. "Training/Sales/Outreach", "Training\Sales\Outreach\A123", "" etc</param>
        /// <param name="root">The root directory e.g. "c:\temp\", "\\server\myfolder", "~/documents", "%USERPROFILE%\Documents\" etc</param>
        /// <param name="includePrefix">The appPrefix e.g. "CustomerA" or "" etc</param>
        /// <returns></returns>
        public static string ToRelativeFile(this string currentPath, string includePrefix = "", string useRelativeRoot = "./", char useSeperator = SystemConstants.InternalDirectorySeparator)
        {
            if (IoExtensions.IsAbsolutePath(currentPath))
            {
                return currentPath;
            }

            // Ensure path has correct terminator
            string[] parts = currentPath
                .TrimStart('.', '~')
                .Split(['\\', '|', '/'], StringSplitOptions.RemoveEmptyEntries);

            if (!string.IsNullOrEmpty(includePrefix) && parts.Length > 0 && parts[0].Equals(includePrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                parts[0] = includePrefix;
            }
            else if (!string.IsNullOrEmpty(includePrefix))
            {
                parts = [includePrefix, .. parts];
            }

            return parts.Length > 0
                ? $"{useRelativeRoot}{string.Join(useSeperator.ToString(), parts)}"
                : useRelativeRoot;
        }

        /// <summary>
        /// Nornalises seperators by replacing \ | / with selected.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="useSeperator"></param>
        /// <returns></returns>
        public static string ReplaceSeperators(this string currentPath, char useSeperator = SystemConstants.InternalDirectorySeparator)
        {
            return currentPath.Replace('\\', useSeperator).Replace('|', useSeperator).Replace('/', useSeperator);
        }

        internal static string _fileFlag = @":\";
        internal static string _fileFlagReplaced = @":/";
        internal static string _internetFlag = @"://";
        internal static string _networkFlag = @"\\";

        internal static string EncodeFileSystemMarkers(this string filePath)
        {
            return filePath.Replace(_fileFlag, "[FILE_FLAG]")
                    .Replace(_internetFlag, "[URI_FLAG]")
                    .Replace(_networkFlag, "[UNC_FLAG]");
        }

        internal static string UnencodeFileSystemMarkers(this string filePath)
        {
            return filePath.Replace("[FILE_FLAG]", _fileFlag)
                    .Replace("[URI_FLAG]", _internetFlag)
                    .Replace("[UNC_FLAG]", _networkFlag);
        }

        internal static string[] SplitToTags(this string filePath)
        {
            return filePath.Split(['\\', '|', '/'], StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Detects the native path separator for an absolute path
        /// Returns '\' for Windows/UNC paths, '/' for Unix/Linux/Mac paths
        /// </summary>
        /// <param name="rootFolder">The absolute path to analyze</param>
        /// <returns>'\' for Windows paths, '/' for Unix paths</returns>
        /// <exception cref="ArgumentException">Thrown if path is not absolute</exception>
        public static char GetPathSeparator(this string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                throw new ArgumentException("Path cannot be null or whitespace", nameof(rootFolder));

            rootFolder = rootFolder.TrimStart();

            // UNC paths (\\server\share or //server/share)
            if (rootFolder.StartsWith(@"\\"))
                return '\\';

            if (rootFolder.StartsWith("//"))
                return '/';

            // Windows absolute paths (C:\, D:\, etc.)
            if (rootFolder.Length >= 3 &&
                char.IsLetter(rootFolder[0]) &&
                rootFolder[1] == ':' &&
                (rootFolder[2] == '\\' || rootFolder[2] == '/'))
            {
                return rootFolder[2] == '\\' ? '\\' : '/';
            }

            // Windows extended-length paths (\\?\C:\)
            if (rootFolder.StartsWith(@"\\?\") || rootFolder.StartsWith(@"\\.\"))
                return '\\';

            // Unix/Linux/Mac absolute paths (starts with /)
            if (rootFolder.StartsWith("/"))
                return '/';

            // Tilde paths (~/ or ~\) - detect based on separator used
            if (rootFolder.StartsWith("~/"))
                return '/';

            if (rootFolder.StartsWith(@"~\"))
                return '\\';

            // Windows environment variables at the start (%VAR%\)
            if (rootFolder.StartsWith("%") && rootFolder.Contains("%\\"))
                return '\\';

            // Assume windows without trailing \
            if (rootFolder.StartsWith("%") && rootFolder.EndsWith("%"))
                return '\\';

            if (rootFolder.StartsWith("%") && rootFolder.Contains("%/"))
                return '/';

            // If we get here, the path is not absolute
            throw new ArgumentException($"Path '{rootFolder}' is not an absolute path", nameof(rootFolder));
        }

        /// <summary>
        /// Remove RootFolder.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        public static string RemoveRootFolder(this string currentPath, string rootFolder)
        {
            char seperator = GetPathSeparator(rootFolder);
            return currentPath.Replace($"{rootFolder.TrimEnd('\\', '|', '/')}{seperator}", string.Empty);
        }
    }
}
