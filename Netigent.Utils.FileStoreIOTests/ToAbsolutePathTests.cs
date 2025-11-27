using Netigent.Utils.FileStoreIO.Extensions;

namespace Netigent.Utils.FileStoreIOTests
{
    [TestFixture]
    public class ToAbsolutePathTests
    {
        #region Empty Path Tests

        [TestCase("", @"C:\temp", "", @"C:\temp")]
        [TestCase("", @"C:\temp\", "", @"C:\temp")]
        [TestCase("", @"D:\data", "CustomerA", @"D:\data\CustomerA")]
        [TestCase("", "/home/user", "", "/home/user")]
        [TestCase("", "/var/www/", "", "/var/www")]
        [TestCase("", @"\\server\share", "", @"\\server\share")]
        [TestCase("", "//server/share/", "", "//server/share")]
        [TestCase("", "~/documents", "", "~/documents")]
        [TestCase("", @"%USERPROFILE%\Documents", "", @"%USERPROFILE%\Documents")]
        public void ToAbsolutePath_EmptyPath_ReturnsRoot(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Single Slash Path Tests

        [TestCase("/", @"C:\temp", "", "/")]
        [TestCase("/", "/home/user", "", "/")]
        [TestCase("/", @"\\server\share", "", "/")]
        public void ToAbsolutePath_SingleSlashPath_ReturnsUnchanged(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Single File Tests - Windows Root

        [TestCase("myfile.txt", @"C:\temp", "", @"C:\temp\myfile.txt")]
        [TestCase("myfile.txt", @"C:\temp\", "", @"C:\temp\myfile.txt")]
        [TestCase("myfile.txt", @"D:\data", "", @"D:\data\myfile.txt")]
        [TestCase("myfile.txt", @"E:\projects\code", "", @"E:\projects\code\myfile.txt")]
        [TestCase("document.pdf", @"C:\Users\John\Documents", "", @"C:\Users\John\Documents\document.pdf")]
        public void ToAbsolutePath_SingleFile_WindowsRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", @"C:\temp", "CustomerA", @"C:\temp\CustomerA\myfile.txt")]
        [TestCase("myfile.txt", @"C:\temp\", "CustomerA", @"C:\temp\CustomerA\myfile.txt")]
        [TestCase("report.xlsx", @"D:\data", "Company", @"D:\data\Company\report.xlsx")]
        [TestCase("config.json", @"C:\app", "settings", @"C:\app\settings\config.json")]
        public void ToAbsolutePath_SingleFile_WindowsRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Single File Tests - Linux Root

        [TestCase("myfile.txt", "/home/user", "", "/home/user/myfile.txt")]
        [TestCase("myfile.txt", "/home/user/", "", "/home/user/myfile.txt")]
        [TestCase("myfile.txt", "/var/www", "", "/var/www/myfile.txt")]
        [TestCase("myfile.txt", "/tmp", "", "/tmp/myfile.txt")]
        [TestCase("document.pdf", "/home/john/documents", "", "/home/john/documents/document.pdf")]
        public void ToAbsolutePath_SingleFile_LinuxRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", "/home/user", "CustomerA", "/home/user/CustomerA/myfile.txt")]
        [TestCase("myfile.txt", "/var/www/", "app", "/var/www/app/myfile.txt")]
        [TestCase("config.json", "/etc", "myapp", "/etc/myapp/config.json")]
        public void ToAbsolutePath_SingleFile_LinuxRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Single File Tests - UNC Root

        [TestCase("myfile.txt", @"\\server\share", "", @"\\server\share\myfile.txt")]
        [TestCase("myfile.txt", @"\\server\share\", "", @"\\server\share\myfile.txt")]
        [TestCase("myfile.txt", @"\\192.168.1.1\docs", "", @"\\192.168.1.1\docs\myfile.txt")]
        [TestCase("document.pdf", @"\\fileserver\public", "", @"\\fileserver\public\document.pdf")]
        public void ToAbsolutePath_SingleFile_UNCBackslash_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", @"\\server\share", "CustomerA", @"\\server\share\CustomerA\myfile.txt")]
        [TestCase("report.xlsx", @"\\server\share\", "Company", @"\\server\share\Company\report.xlsx")]
        public void ToAbsolutePath_SingleFile_UNCBackslash_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", "//server/share", "", "//server/share/myfile.txt")]
        [TestCase("myfile.txt", "//server/share/", "", "//server/share/myfile.txt")]
        [TestCase("document.pdf", "//fileserver/public", "", "//fileserver/public/document.pdf")]
        public void ToAbsolutePath_SingleFile_UNCForwardSlash_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", "//server/share", "CustomerA", "//server/share/CustomerA/myfile.txt")]
        [TestCase("report.xlsx", "//server/share/", "Company", "//server/share/Company/report.xlsx")]
        public void ToAbsolutePath_SingleFile_UNCForwardSlash_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Multi-Level Path Tests - Windows Root

        [TestCase("hr/training/users/", @"C:\temp", "", @"C:\temp\hr\training\users")]
        [TestCase("hr/training/users/", @"C:\temp\", "", @"C:\temp\hr\training\users")]
        [TestCase(@"myfiles\docs\a123", @"C:\data", "", @"C:\data\myfiles\docs\a123")]
        [TestCase(@"myfiles\docs\a123", @"D:\storage\", "", @"D:\storage\myfiles\docs\a123")]
        [TestCase("folder/subfolder/file.txt", @"C:\root", "", @"C:\root\folder\subfolder\file.txt")]
        [TestCase(@"dept\team\project\", @"E:\work", "", @"E:\work\dept\team\project")]
        public void ToAbsolutePath_MultiLevel_WindowsRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("hr/training/users/", @"C:\temp", "CustomerA", @"C:\temp\CustomerA\hr\training\users")]
        [TestCase("hr/training/users/", @"C:\temp\", "CustomerA", @"C:\temp\CustomerA\hr\training\users")]
        [TestCase(@"myfiles\docs\a123", @"C:\data", "Client1", @"C:\data\Client1\myfiles\docs\a123")]
        [TestCase("folder/subfolder/file.txt", @"D:\root", "AppData", @"D:\root\AppData\folder\subfolder\file.txt")]
        public void ToAbsolutePath_MultiLevel_WindowsRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Multi-Level Path Tests - Linux Root

        [TestCase("hr/training/users/", "/home/user", "", "/home/user/hr/training/users")]
        [TestCase("hr/training/users/", "/var/www/", "", "/var/www/hr/training/users")]
        [TestCase(@"myfiles\docs\a123", "/tmp", "", "/tmp/myfiles/docs/a123")]
        [TestCase("folder/subfolder/file.txt", "/opt/data", "", "/opt/data/folder/subfolder/file.txt")]
        [TestCase(@"dept\team\project\", "/home/dev", "", "/home/dev/dept/team/project")]
        public void ToAbsolutePath_MultiLevel_LinuxRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("hr/training/users/", "/home/user", "CustomerA", "/home/user/CustomerA/hr/training/users")]
        [TestCase("hr/training/users/", "/var/www/", "CustomerA", "/var/www/CustomerA/hr/training/users")]
        [TestCase(@"myfiles\docs\a123", "/tmp", "Client1", "/tmp/Client1/myfiles/docs/a123")]
        [TestCase("folder/subfolder/file.txt", "/opt/data", "AppData", "/opt/data/AppData/folder/subfolder/file.txt")]
        public void ToAbsolutePath_MultiLevel_LinuxRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Multi-Level Path Tests - UNC Root

        [TestCase("hr/training/users/", @"\\server\share", "", @"\\server\share\hr\training\users")]
        [TestCase("hr/training/users/", @"\\server\share\", "", @"\\server\share\hr\training\users")]
        [TestCase(@"myfiles\docs\a123", @"\\server\data", "", @"\\server\data\myfiles\docs\a123")]
        [TestCase("folder/subfolder/file.txt", @"\\fileserver\root", "", @"\\fileserver\root\folder\subfolder\file.txt")]
        public void ToAbsolutePath_MultiLevel_UNCBackslash_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("hr/training/users/", @"\\server\share", "CustomerA", @"\\server\share\CustomerA\hr\training\users")]
        [TestCase(@"myfiles\docs\a123", @"\\server\data\", "Client1", @"\\server\data\Client1\myfiles\docs\a123")]
        public void ToAbsolutePath_MultiLevel_UNCBackslash_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("hr/training/users/", "//server/share", "", "//server/share/hr/training/users")]
        [TestCase("hr/training/users/", "//server/share/", "", "//server/share/hr/training/users")]
        [TestCase(@"myfiles\docs\a123", "//server/data", "", "//server/data/myfiles/docs/a123")]
        public void ToAbsolutePath_MultiLevel_UNCForwardSlash_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("hr/training/users/", "//server/share", "CustomerA", "//server/share/CustomerA/hr/training/users")]
        [TestCase(@"myfiles\docs\a123", "//server/data/", "Client1", "//server/data/Client1/myfiles/docs/a123")]
        public void ToAbsolutePath_MultiLevel_UNCForwardSlash_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Mixed Separator Tests

        [TestCase(@"hr\training/users", @"C:\temp", "", @"C:\temp\hr\training\users")]
        [TestCase("folder/sub\\another", "/home/user", "", "/home/user/folder/sub/another")]
        [TestCase(@"a\b/c\d", @"\\server\share", "", @"\\server\share\a\b\c\d")]
        [TestCase("mix/ed\\sep|arators", "//server/share", "", "//server/share/mix/ed/sep/arators")]
        public void ToAbsolutePath_MixedSeparators_NormalizesCorrectly(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Environment Variable Roots

        [TestCase("myfile.txt", @"%USERPROFILE%\Documents", "", @"%USERPROFILE%\Documents\myfile.txt")]
        [TestCase("myfile.txt", @"%USERPROFILE%\Documents\", "", @"%USERPROFILE%\Documents\myfile.txt")]
        [TestCase("hr/training/users/", @"%APPDATA%", "", @"%APPDATA%\hr\training\users")]
        [TestCase("config.json", @"%TEMP%\app", "", @"%TEMP%\app\config.json")]
        public void ToAbsolutePath_WindowsEnvVarRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", @"%USERPROFILE%\Documents", "CustomerA", @"%USERPROFILE%\Documents\CustomerA\myfile.txt")]
        [TestCase("hr/training/users/", @"%APPDATA%\", "CustomerA", @"%APPDATA%\CustomerA\hr\training\users")]
        public void ToAbsolutePath_WindowsEnvVarRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", "~/documents", "", "~/documents/myfile.txt")]
        [TestCase("myfile.txt", "~/documents/", "", "~/documents/myfile.txt")]
        [TestCase("hr/training/users/", "~/projects", "", "~/projects/hr/training/users")]
        [TestCase(@"myfiles\docs\a123", "~/data", "", "~/data/myfiles/docs/a123")]
        public void ToAbsolutePath_TildeRoot_NoPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("myfile.txt", "~/documents", "CustomerA", "~/documents/CustomerA/myfile.txt")]
        [TestCase("hr/training/users/", "~/projects/", "CustomerA", "~/projects/CustomerA/hr/training/users")]
        public void ToAbsolutePath_TildeRoot_WithPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region AppPrefix Already in Path Tests
        [TestCase("customerA/docs", @"C:\temp", "CustomerA", @"C:\temp\CustomerA\docs")] // Case insensitive
        [TestCase("CUSTOMERA/docs", @"C:\temp", "CustomerA", @"C:\temp\CustomerA\docs")] // Case insensitive
        [TestCase("CustomerA/docs", @"C:\temp", "CustomerA", @"C:\temp\CustomerA\docs")]
        [TestCase(@"CustomerA\hr\training", @"C:\data", "CustomerA", @"C:\data\CustomerA\hr\training")]
        [TestCase("CustomerA/myfile.txt", "/home/user", "CustomerA", "/home/user/CustomerA/myfile.txt")]
        [TestCase(@"CustomerA\myfile.txt", @"\\server\share", "CustomerA", @"\\server\share\CustomerA\myfile.txt")]
        public void ToAbsolutePath_PrefixAlreadyInPath_DoesNotDuplicate(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        [TestCase("CustomerABC/docs", @"C:\temp", "Customer", @"C:\temp\Customer\CustomerABC\docs")] // Partial match prevented
        [TestCase("MyCustomer/docs", @"C:\temp", "Customer", @"C:\temp\Customer\MyCustomer\docs")] // Not at start
        public void ToAbsolutePath_PartialPrefixMatch_StillAddsPrefix(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Already Absolute Path Tests

        [TestCase(@"C:\Windows\System32", @"C:\temp", "", @"C:\Windows\System32")]
        [TestCase(@"C:\Windows\System32", @"C:\temp", "CustomerA", @"C:\Windows\System32")]
        [TestCase(@"D:\absolute\path", "/home/user", "", @"D:\absolute\path")]
        [TestCase("/home/user/docs", @"C:\temp", "", "/home/user/docs")]
        [TestCase("/var/www/html", "/opt/data", "AppData", "/var/www/html")]
        [TestCase(@"\\server\share\folder", @"C:\temp", "", @"\\server\share\folder")]
        [TestCase("//server/share/folder", "/home/user", "CustomerA", "//server/share/folder")]
        public void ToAbsolutePath_AlreadyAbsolute_ReturnsUnchanged(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Trailing Separator in Path Tests

        [TestCase("folder/", @"C:\temp", "", @"C:\temp\folder")]
        [TestCase(@"folder\", @"C:\temp", "", @"C:\temp\folder")]
        [TestCase("hr/training/users/", @"C:\temp", "", @"C:\temp\hr\training\users")]
        [TestCase(@"dept\team\project\", "/home/user", "", "/home/user/dept/team/project")]
        public void ToAbsolutePath_TrailingSeparatorInPath_Removed(string path, string root, string appPrefix, string expected)
        {
            var result = IoExtensions.ToAbsolutePath(path, root, appPrefix);
            Assert.AreEqual(expected, result);
        }

        #endregion

        #region Real-World Scenarios

        [Test]
        public void ToAbsolutePath_WindowsDocumentsFolder_TypicalUsage()
        {
            var result = IoExtensions.ToAbsolutePath("projects/myapp/src", @"C:\Users\John\Documents", "work");
            Assert.AreEqual(@"C:\Users\John\Documents\work\projects\myapp\src", result);
        }

        [Test]
        public void ToAbsolutePath_LinuxHomeDirectory_TypicalUsage()
        {
            var result = IoExtensions.ToAbsolutePath("projects/myapp/src", "/home/john/documents", "work");
            Assert.AreEqual("/home/john/documents/work/projects/myapp/src", result);
        }

        [Test]
        public void ToAbsolutePath_NetworkShare_TypicalUsage()
        {
            var result = IoExtensions.ToAbsolutePath("reports/Q4/summary.xlsx", @"\\fileserver\shared", "Company");
            Assert.AreEqual(@"\\fileserver\shared\Company\reports\Q4\summary.xlsx", result);
        }

        [Test]
        public void ToAbsolutePath_GitBashWindowsStyle()
        {
            var result = IoExtensions.ToAbsolutePath("src/main.cs", "C:/projects/myapp", "");
            Assert.AreEqual("C:/projects/myapp/src/main.cs", result);
        }

        #endregion
    }
}
