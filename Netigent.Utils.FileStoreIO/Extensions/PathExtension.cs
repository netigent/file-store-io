using Netigent.Utils.FileStoreIO.Constants;
using Netigent.Utils.FileStoreIO.Helpers;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Netigent.Utils.FileStoreIO.Extensions
{
    public class PathInfo
    {
        public string PathTags { get; set; } = string.Empty;

        public char PathSeperator { get; set; } = Path.DirectorySeparatorChar;

        public string FilenameNoExtension { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public string Filename { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public string RelativeFilePath { get; set; } = string.Empty;
    }

    public static class PathExtension
    {
        public static PathInfo GetPathInfo(this InternalFileModel fileModel, char? usePathSeparator = null) =>
            (Path.Combine(fileModel.PathTags, fileModel.Name + fileModel.Extension).SetPathSeparator(SystemConstants.InternalDirectorySeparator)).GetPathInfo();

        public static PathInfo GetPathInfo(this string filePath, string addRootFolderPrefix = "", string removeRootFolderPrefix = "")
        {
            var relativeFileLocation = Path.Combine(addRootFolderPrefix, filePath.DropFirstChar(new char[] { '\\', '|', '/' }));

            var fileName = Path.GetFileNameWithoutExtension(relativeFileLocation);
            var extension = Path.GetExtension(relativeFileLocation);

            var pathOnly = relativeFileLocation.Split(
                separator: new[] { fileName + extension },
                options: System.StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            var cleanRoot = removeRootFolderPrefix.TrimEnd(new char[] { '\\', '|', '/' });

            string PathTags = (cleanRoot?.Length > 0 && relativeFileLocation.StartsWith(cleanRoot)
                ? pathOnly.Replace(cleanRoot, "").DropFirstChar(new char[] { '\\', '|', '/' })
                : pathOnly);

            string relativePath = cleanRoot?.Length > 0 && relativeFileLocation.StartsWith(cleanRoot)
                ? relativeFileLocation.Replace(cleanRoot, "").DropFirstChar(new char[] { '\\', '|', '/' })
                : relativeFileLocation;

            return new PathInfo
            {
                PathTags = PathTags + SystemConstants.InternalDirectorySeparator,
                PathSeperator = SystemConstants.InternalDirectorySeparator,
                FileExtension = extension,
                MimeType = MimeHelper.GetMimeType(extension),
                Filename = fileName + extension,
                FilenameNoExtension = fileName,
                RelativeFilePath = relativePath,
            };
        }

        public static string DropFirstChar(this string value, char[] dropStartChars)
        {
            char firstChar = value.FirstOrDefault();
            if (dropStartChars.Any(x => x == value.FirstOrDefault()))
            {
                return string.Join(string.Empty, value.Skip(1));
            }

            return value;
        }

        public static string SetPathSeparator(this string filePath, char pathSeparator)
        {
            // Ignoring :// \\ :\, replace everything with incoming / \ or | etc
            return string.Join(pathSeparator.ToString(), filePath.EncodeFileSystemMarkers().SplitToTags()).UnencodeFileSystemMarkers();

        }

        public static string _fileFlag = @":\";
        public static string _internetFlag = @"://";
        public static string _networkFlag = @"\\";

        private static string EncodeFileSystemMarkers(this string filePath)
        {
            return filePath.Replace(_fileFlag, "[FILE_FLAG]")
                    .Replace(_internetFlag, "[URI_FLAG]")
                    .Replace(_networkFlag, "[UNC_FLAG]");
        }

        private static string UnencodeFileSystemMarkers(this string filePath)
        {
            return filePath.Replace("[FILE_FLAG]", _fileFlag)
                    .Replace("[URI_FLAG]", _internetFlag)
                    .Replace("[UNC_FLAG]", _networkFlag);
        }

        public static string[] SplitToTags(this string filePath)
        {
            return filePath.Split(new char[] { '\\', '|', '/' }, StringSplitOptions.RemoveEmptyEntries);
        }


        public static bool IsRelativePath(this string filePath)
        {


            if (string.IsNullOrEmpty(filePath))
            {
                return true;
            }

            // Is the file absolute location??
            if (filePath.Contains(_fileFlag)
                || filePath.StartsWith(_networkFlag, StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains(_internetFlag))
            {
                return false;
            }

            // Treat as relative
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Handle filename and foldernames safely to allow writing files to windows etc.
        /// </summary>
        /// <param name="filenameToParse">The string filename / path to parse to parse</param>
        /// <param name="replaceForbiddenFilenameChar">Replace \ / : ? * " < | > charaters with? These are not allowed by Windows, with some also exluded by Mac and Linux.</param>
        /// <param name="allowExtendedAscii">Allow Unicode or UTF-16 chars, if false char will be dropped.</param>
        /// <param name="ignorePathSeperators">If you are testing a full filepath e.g. \\Server\folder\myfile.txt, you might want to exclude \ hence add new [] { '\\' }.</param>
        /// <returns>Safe string.</returns>
        public static string SafeFilename(this string filenameToParse, char replaceForbiddenFilenameChar = '_', bool allowExtendedAscii = true, char[] ignorePathSeperators = null)
        {
            //
            // Inspired by AWS guidance on Keys for S3
            // https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-keys.html
            // Windows has most forbidden chars, this is excellent for general Mac, Linux, Windows
            // https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
            //

            StringBuilder output = new StringBuilder();
            char[] charsToCheck = filenameToParse.ToCharArray();

            char[] unsupportedOsFilenameChars = new char[]
            {
                '/', // Linux, Mac + Windows Unsupported
                '\\', // Windows
                ':', // Mac + Windows Unsupported
                '?', // Windows
                '*', // Windows
                '"', // Windows
                '>', // Windows
                '|', // Windows
                '<'  // Windows
            };

            foreach (var charItem in charsToCheck)
            {
                // Drop control characters
                if (((int)charItem >= 0 && (int)charItem < 32) || // unprintable control codes
                    (int)charItem == 127) // delete character
                {
                    continue;
                }

                // Replace unsupported windows chars
                else if (unsupportedOsFilenameChars.Any(x => x == charItem) && ignorePathSeperators?.Any(x => x == charItem) != true)
                {
                    output.Append(replaceForbiddenFilenameChar);
                }

                // Allow extendedAscii
                else if (!allowExtendedAscii &&
                 ((int)charItem >= 128 && (int)charItem <= 255)) // extended ASCII characters
                {
                    continue;
                }

                // Take As-Is
                else
                {
                    output.Append(charItem);
                }
            }

            return output.ToString();
        }
    }
}
