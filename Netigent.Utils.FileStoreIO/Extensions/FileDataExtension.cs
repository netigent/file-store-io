using System.Linq;
using System.Text;

namespace Netigent.Utils.FileStoreIO.Extensions
{
    public static class FileDataExtension
    {
        internal static string _fileFlag = @":\";
        internal static string _fileFlagReplaced = @":/";
        internal static string _internetFlag = @"://";
        internal static string _networkFlag = @"\\";

        internal static string DropFirstChar(this string value, char[] dropStartChars)
        {
            char firstChar = value.FirstOrDefault();
            if (dropStartChars.Any(x => x == value.FirstOrDefault()))
            {
                return string.Join(string.Empty, value.Skip(1));
            }

            return value;
        }

        /// <summary>
        /// Handle filename and foldernames safely to allow writing files to windows etc.
        /// </summary>
        /// <param name="filenameToParse">The string filename / path to parse to parse</param>
        /// <param name="replaceForbiddenFilenameChar">Replace \ / : ? * " < | > charaters with? These are not allowed by Windows, with some also exluded by Mac and Linux.</param>
        /// <param name="allowExtendedAscii">Allow Unicode or UTF-16 chars, if false char will be dropped.</param>
        /// <param name="ignorePathSeperators">If you are testing a full filepath e.g. \\Server\folder\myfile.txt, you might want to exclude \ hence add new [] { '\\' }.</param>
        /// <returns>Safe string.</returns>
        internal static string SafeFilename(this string filenameToParse, char replaceForbiddenFilenameChar = '_', bool allowExtendedAscii = true, char[] ignorePathSeperators = null)
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
