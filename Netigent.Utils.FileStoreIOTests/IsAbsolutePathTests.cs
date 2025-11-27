using Netigent.Utils.FileStoreIO.Extensions;

namespace Netigent.Utils.FileStoreIOTests
{
    [TestFixture]
    public class IsAbsolutePathTests
    {
        #region Windows Absolute Paths

        [TestCase(@"C:\Windows\System32")]
        [TestCase(@"D:\Program Files")]
        [TestCase(@"Z:\")]
        [TestCase(@"c:\temp")]
        [TestCase(@"E:\folder\subfolder\file.txt")]
        [TestCase(@"C:/Windows/System32")] // Forward slashes on Windows
        public void IsAbsolutePath_WindowsAbsolutePaths_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region UNC Paths

        [TestCase(@"\\server\share")]
        [TestCase(@"\\server\share\folder")]
        [TestCase(@"\\192.168.1.1\share")]
        [TestCase(@"\\SERVERNAME\SharedFolder\file.txt")]
        [TestCase(@"//server/share")] // Unix-style UNC
        [TestCase(@"//server/share/folder/file")]
        public void IsAbsolutePath_UNCPaths_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Unix/Linux/Mac Absolute Paths

        [TestCase("/")]
        [TestCase("/home")]
        [TestCase("/home/user")]
        [TestCase("/usr/local/bin")]
        [TestCase("/var/log/system.log")]
        [TestCase("/etc/config")]
        [TestCase("/tmp/file.txt")]
        public void IsAbsolutePath_UnixAbsolutePaths_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Windows Extended and Device Paths

        [TestCase(@"\\?\C:\Windows")]
        [TestCase(@"\\?\UNC\server\share")]
        [TestCase(@"\\.\COM1")]
        [TestCase(@"\\.\PhysicalDrive0")]
        public void IsAbsolutePath_WindowsExtendedPaths_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Environment Variable and Tilde Paths - Absolute

        [TestCase("~/documents")]
        [TestCase("~/home/user")]
        [TestCase("~/.config")]
        [TestCase("~/")]
        [TestCase(@"~/folder\file.txt")] // Mixed separators
        [TestCase(@"%USERPROFILE%\Documents")]
        [TestCase(@"%APPDATA%\config")]
        [TestCase(@"%TEMP%\file.txt")]
        [TestCase(@"%ProgramFiles%\Application")]
        [TestCase(@"%SystemRoot%\System32")]
        [TestCase(@"C:\%USERNAME%\folder")] // Environment variable in middle of absolute path
        [TestCase(@"D:\%APPDATA%\test")]
        public void IsAbsolutePath_EnvironmentVariableAndTildePaths_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Environment Variable Paths - Relative

        [TestCase("$HOME/documents")] // Unix variables with $ are relative (not standard shell expansion in paths)
        [TestCase("$HOME/.bashrc")]
        [TestCase("$TEMP/file")]
        [TestCase("/home/$USER/docs")] // Environment variable in middle
        [TestCase("~")] // Just tilde without separator
        [TestCase("~user")] // Other user's home (Unix)
        [TestCase("~user/documents")]
        public void IsAbsolutePath_UnixVariablePathsAndBareeTilde_ReturnsFalse(string path)
        {
            Assert.IsFalse(IoExtensions.IsAbsolutePath(path));
        }

        [TestCase("C:")] // Drive letter without separator
        [TestCase("C:file.txt")] // Drive-relative path
        [TestCase(@"C:folder\file.txt")]
        public void IsAbsolutePath_DriveRelativePaths_ReturnsFalse(string path)
        {
            Assert.IsFalse(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Edge Cases

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void IsAbsolutePath_EmptyOrWhitespace_ReturnsFalse(string path)
        {
            Assert.IsFalse(IoExtensions.IsAbsolutePath(path));
        }



        [TestCase(@"\Windows")] // Rooted but not absolute on Windows
        [TestCase(@"\folder\file.txt")]
        public void IsAbsolutePath_RootedButNotAbsolute_ReturnsFalse(string path)
        {
            Assert.IsFalse(IoExtensions.IsAbsolutePath(path));
        }

        [TestCase("  C:\\Windows")] // Leading whitespace
        [TestCase("  /home/user")]
        [TestCase("  //server/share")]
        public void IsAbsolutePath_LeadingWhitespace_ReturnsTrue(string path)
        {
            Assert.IsTrue(IoExtensions.IsAbsolutePath(path));
        }

        #endregion

        #region Cross-Platform Mixed Tests

        [Test]
        public void IsAbsolutePath_MixedScenarios_ReturnsCorrectly()
        {
            // Absolute paths
            Assert.IsTrue(IoExtensions.IsAbsolutePath(@"C:\Program Files"));
            Assert.IsTrue(IoExtensions.IsAbsolutePath("/usr/bin"));
            Assert.IsTrue(IoExtensions.IsAbsolutePath(@"\\server\share"));

            // Relative paths
            Assert.IsFalse(IoExtensions.IsAbsolutePath("documents/file.txt"));
            Assert.IsFalse(IoExtensions.IsAbsolutePath(@"..\folder"));
            Assert.IsFalse(IoExtensions.IsAbsolutePath("./config"));
        }

        #endregion

        #region Relative Paths

        [TestCase("folder")]
        [TestCase("folder/subfolder")]
        [TestCase(@"folder\subfolder")]
        [TestCase("./file.txt")]
        [TestCase("../parent/file.txt")]
        [TestCase(@"..\parent\file.txt")]
        [TestCase("subfolder/another/file.txt")]
        [TestCase(@"subfolder\another\file.txt")]
        [TestCase("file.txt")]
        [TestCase(".")]
        [TestCase("..")]
        public void IsAbsolutePath_RelativePaths_ReturnsFalse(string path)
        {
            Assert.IsFalse(IoExtensions.IsAbsolutePath(path));
        }

        #endregion
    }
}
