using Netigent.Utils.FileStoreIO.Extensions;

namespace Netigent.Utils.FileStoreIOTests
{
    [TestFixture]
    public class RelativePathTests
    {
        private const string _fsLocation = "\\\\karl17\\Users\\karl\\OneDrive\\Desktop\\FS\\";

        #region RelativeFolder tests


        [TestCase("./Catalog/222/", "", "Catalog/222/")]
        [TestCase("./Catalog/222/", "", "Catalog/222/")]
        [TestCase("Catalog/222/", "", "Catalog/222/")]
        [TestCase("Catalog/222/", "", "Catalog/222/")]
        [TestCase("|Catalog|222|", "", "Catalog/222/")]
        [TestCase("Catalog|222|", "", "Catalog/222/")]
        [TestCase("Catalog|222|", "", "Catalog/222/")]
        [TestCase("\\Catalog\\222\\", "", "Catalog/222/")]
        [TestCase("\\Catalog\\222\\", "", "Catalog/222/")]
        [TestCase("Catalog\\222\\", "", "Catalog/222/")]

        [TestCase("./Catalog/222/", "./", "./Catalog/222/")]
        [TestCase("./Catalog/222/", "./", "./Catalog/222/")]
        [TestCase("./Catalog/222/", "./", "./Catalog/222/")]
        [TestCase("Catalog/222/", "./", "./Catalog/222/")]
        [TestCase("|Catalog|222|", "./", "./Catalog/222/")]
        [TestCase("Catalog|222|", "./", "./Catalog/222/")]
        [TestCase("Catalog|222|", "./", "./Catalog/222/")]
        [TestCase("\\Catalog\\222\\", "./", "./Catalog/222/")]
        [TestCase("\\Catalog\\222\\", "./", "./Catalog/222/")]
        [TestCase("Catalog\\222\\", "./", "./Catalog/222/")]

        public async Task ToRelativePath_FromRelativeFolder_Tests(
         string sourceFile,
         string relativeRoot,
         string expectedFolder)
        {
            // Arrange
            sourceFile = sourceFile.Replace("{{FsRoot}}", string.IsNullOrEmpty(_fsLocation) ? string.Empty : _fsLocation + "/");
            string relPath = IoExtensions.ToRelativeFolder(sourceFile, includePrefix: string.Empty, useRelativeRoot: relativeRoot);

            // Assert - ExtClientRef matches expected (path normalization)
            Assert.That(relPath, Is.EqualTo(expectedFolder),
                $"Relative Path mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{relPath}'");
        }

        #endregion
        #region With Prefix

        [TestCase("./Catalog/222/myFile2.txt", "", "NETIGENT/Catalog/222/")]
        [TestCase("/Catalog/22/myFile2.txt", "", "NETIGENT/Catalog/22/")]
        [TestCase("\\Catalog\\21\\myFile2.txt", "", "NETIGENT/Catalog/21/")]
        [TestCase("Catalog/20/myFile2.txt", "", "NETIGENT/Catalog/20/")]
        [TestCase("Catalog|2|myFile1.txt", "", "NETIGENT/Catalog/2/")]
        [TestCase("\\Catalog\\Item-32\\myFile3.txt", "", "NETIGENT/Catalog/Item-32/")]
        [TestCase("Catalog\\Item-32\\myFile2.txt", "", "NETIGENT/Catalog/Item-32/")]
        [TestCase("Catalog/Item-32/myFile1.txt", "", "NETIGENT/Catalog/Item-32/")]
        [TestCase("2/myFile1.txt", "", "NETIGENT/2/")]
        [TestCase("1\\myFile1.txt", "", "NETIGENT/1/")]
        [TestCase("myFile1.txt", "", "NETIGENT/")]
        [TestCase("./Catalog/222/myFile2.txt", "./", "./NETIGENT/Catalog/222/")]
        [TestCase("/Catalog/22/myFile2.txt", "./", "./NETIGENT/Catalog/22/")]
        [TestCase("\\Catalog\\21\\myFile2.txt", "./", "./NETIGENT/Catalog/21/")]
        [TestCase("Catalog/20/myFile2.txt", "./", "./NETIGENT/Catalog/20/")]
        [TestCase("Catalog|2|myFile1.txt", "./", "./NETIGENT/Catalog/2/")]
        [TestCase("\\Catalog\\Item-32\\myFile3.txt", "./", "./NETIGENT/Catalog/Item-32/")]
        [TestCase("Catalog\\Item-32\\myFile2.txt", "./", "./NETIGENT/Catalog/Item-32/")]
        [TestCase("Catalog/Item-32/myFile1.txt", "./", "./NETIGENT/Catalog/Item-32/")]
        [TestCase("2/myFile1.txt", "./", "./NETIGENT/2/")]
        [TestCase("1\\myFile1.txt", "./", "./NETIGENT/1/")]
        [TestCase("myFile1.txt", "./", "./NETIGENT/")]
        public async Task ToRelativePath_WithPrefix_Tests(
             string sourceFile,
             string relativeRoot,
             string expectedFolder)
        {
            // Arrange
            sourceFile = sourceFile.Replace("{{FsRoot}}", string.IsNullOrEmpty(_fsLocation) ? string.Empty : _fsLocation + "/");
            string relPath = IoExtensions.ToRelativeFolder(sourceFile, includePrefix: "NETIGENT", useRelativeRoot: relativeRoot);

            // Assert - ExtClientRef matches expected (path normalization)
            Assert.That(relPath, Is.EqualTo(expectedFolder),
                $"Relative Path mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{relPath}'");
        }

        #endregion

        [TestCase("./Catalog/222/myFile2.txt", "", "Catalog/222/")]
        [TestCase("/Catalog/22/myFile2.txt", "", "Catalog/22/")]
        [TestCase("\\Catalog\\21\\myFile2.txt", "", "Catalog/21/")]
        [TestCase("Catalog/20/myFile2.txt", "", "Catalog/20/")]
        [TestCase("Catalog|2|myFile1.txt", "", "Catalog/2/")]
        [TestCase("\\Catalog\\Item-32\\myFile3.txt", "", "Catalog/Item-32/")]
        [TestCase("Catalog\\Item-32\\myFile2.txt", "", "Catalog/Item-32/")]
        [TestCase("Catalog/Item-32/myFile1.txt", "", "Catalog/Item-32/")]
        [TestCase("2/myFile1.txt", "", "2/")]
        [TestCase("1\\myFile1.txt", "", "1/")]
        [TestCase("myFile1.txt", "", "")]
        [TestCase("./Catalog/222/myFile2.txt", "./", "./Catalog/222/")]
        [TestCase("/Catalog/22/myFile2.txt", "./", "./Catalog/22/")]
        [TestCase("\\Catalog\\21\\myFile2.txt", "./", "./Catalog/21/")]
        [TestCase("Catalog/20/myFile2.txt", "./", "./Catalog/20/")]
        [TestCase("Catalog|2|myFile1.txt", "./", "./Catalog/2/")]
        [TestCase("\\Catalog\\Item-32\\myFile3.txt", "./", "./Catalog/Item-32/")]
        [TestCase("Catalog\\Item-32\\myFile2.txt", "./", "./Catalog/Item-32/")]
        [TestCase("Catalog/Item-32/myFile1.txt", "./", "./Catalog/Item-32/")]
        [TestCase("2/myFile1.txt", "./", "./2/")]
        [TestCase("1\\myFile1.txt", "./", "./1/")]
        [TestCase("myFile1.txt", "./", "./")]
        public async Task ToRelativePath_NoPrefix_Tests(
         string sourceFile,
         string relativeRoot,
         string expectedFolder)
        {
            // Arrange
            sourceFile = sourceFile.Replace("{{FsRoot}}", string.IsNullOrEmpty(_fsLocation) ? string.Empty : _fsLocation + "/");
            string relPath = IoExtensions.ToRelativeFolder(sourceFile, includePrefix: string.Empty, useRelativeRoot: relativeRoot);

            // Assert - ExtClientRef matches expected (path normalization)
            Assert.That(relPath, Is.EqualTo(expectedFolder),
                $"Relative Path mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{relPath}'");
        }
    }
}
