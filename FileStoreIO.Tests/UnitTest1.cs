using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;

namespace FileStoreIO.Tests
{
    public class Tests
    {
        private IFileStoreIOClient _client;

      
        private const string _largeFile = ".\\TestFiles\\whiteVid_37MB.mp4";
        private const string _stdPdfFile = ".\\TestFiles\\pdf_180KB.pdf";
        private const string _stdImgFile = ".\\TestFiles\\photo_A_3MB.jpg";
        private const string _rootFolder = @"C:\temp\Files\";
        private int _maxVErsions = 3;

        [SetUp]
        public void Setup()
        {
            BoxConfig exampleBox = new BoxConfig()
            {
                EnterpriseID = "123456789",
                BoxAppSettings = new BoxAppSettings()
                {
                    ClientID = "exampleid12345",
                    ClientSecret = "examplesecret12345",
                    AppAuth = new AppAuth()
                    {
                        Passphrase = "examplepassphrase12345",
                        PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
                        PublicKeyID = "abc1234",
                    },
                },
                RootFolder = 0,
            };

            _client = new FileStoreIOClient(
            databaseConnection: "Server=.;Database=[DATABASE];UID=[USER];PWD=[PASSWORD];",
            fileStoreRoot: _rootFolder,
            maxVersions: _maxVErsions,
            dbSchema: "filestore",
            defaultFileStore: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Box,
            boxConfig: exampleBox);
        }

        [Test]
        public void StandardFileDownload()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: fi.Name,
                fileLocation: FileStorageProvider.Box,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals");
        }


        [Test]
        public void StandardFileUploadNoVersions()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: fi.Name,
                fileLocation: FileStorageProvider.Box,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals");
        }


        [Test]
        public void GetFileFromBox()
        {
            var fileTest = _client.Files_GetAll().FirstOrDefault();

            //FileInfo fi = new FileInfo(filePath);

            var result = _client.File_Get(fileRef: fileTest.FileRef).Result;
            Assert.IsNotNull(result);
        }

        [Test]
        public void BulkUpload()
        {
            List<string> files = LoopThroughDirectories(_rootFolder);


            foreach (var item in files)
            {
                string[] sections = item.Replace(_rootFolder, string.Empty).Split('\\');

                FileInfo fi = new FileInfo(item);
                byte[] contents = File.ReadAllBytes(item);
                string maingroup = sections[0] ?? string.Empty;
                string sgroup = sections[1] ?? string.Empty;

                _client.File_UpsertAsync(
              fileContents: contents,
              fullFilename: fi.Name,
              fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Box,
              description: fi.Name,
              mainGroup: maingroup,
              subGroup: sgroup);

            }

            Assert.True(files.Count > 0);
        }

        [Test]
        public void MultipleVersionsTestingDEF()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = string.Empty;

            for (int i = 0; i < _maxVErsions + 3; i++)
            {
                fileRef = _client.File_UpsertAsync(
                    fileContents: contents,
                    fullFilename: "DEF__" + fi.Name,
                    description: fi.Name,
                    mainGroup: "1981",
                    subGroup: "New Approvals").Result;
            }

            var results = _client.File_GetVersionsInfo(fileRef);

            Assert.AreEqual(_maxVErsions, results.Count);

            // Go beyond size of array to ensure ok
            List<KeyValuePair<int, long>> tests = new List<KeyValuePair<int, long>>();

            for (int iVersion = 0; iVersion < _maxVErsions + 1; iVersion++)
            {
                var currentResult = _client.File_Get(fileRef, iVersion).Result;
                tests.Add(new KeyValuePair<int, long>(iVersion, currentResult?.Data?.LongLength ?? 0));
            }

            Assert.AreEqual(tests.Where(x => x.Value > 0).Count(), _maxVErsions);
        }

        [Test]
        public void FileUploadLargeDEF()
        {
            string filePath = Path.GetFullPath(_largeFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: "DEF__" + fi.Name,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals").Result;


            var currentResult = _client.File_Get(fileRef, 0).Result;

            Assert.AreEqual(currentResult?.Data?.LongLength, contents.LongLength);

        }


        [Test]
        public void MultipleVersionsTestingDB()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = string.Empty;

            for (int i = 0; i < _maxVErsions + 3; i++)
            {
                fileRef = _client.File_UpsertAsync(
                    fileContents: contents,
                    fullFilename: "DB__" + fi.Name,
                    fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Database,
                    description: fi.Name,
                    mainGroup: "1981",
                    subGroup: "New Approvals").Result;
            }

            var results = _client.File_GetVersionsInfo(fileRef);

            Assert.AreEqual(_maxVErsions, results.Count);

            // Go beyond size of array to ensure ok
            List<KeyValuePair<int, long>> tests = new List<KeyValuePair<int, long>>();

            for (int iVersion = 0; iVersion < _maxVErsions + 1; iVersion++)
            {
                var currentResult = _client.File_Get(fileRef, iVersion).Result;
                tests.Add(new KeyValuePair<int, long>(iVersion, currentResult?.Data?.LongLength ?? 0));
            }

            Assert.AreEqual(tests.Where(x => x.Value > 0).Count(), _maxVErsions);
        }

        [Test]
        public void FileUploadLargeDB()
        {
            string filePath = Path.GetFullPath(_largeFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: "DB__" + fi.Name,
                fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Database,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals").Result;


            var currentResult = _client.File_Get(fileRef, 0).Result;

            Assert.AreEqual(currentResult?.Data?.LongLength, contents.LongLength);

        }

        [Test]
        public void MultipleVersionsTestingUNC()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = string.Empty;

            for (int i = 0; i < _maxVErsions + 3; i++)
            {
                fileRef = _client.File_UpsertAsync(
                    fileContents: contents,
                    fullFilename: "unc__" + fi.Name,
                    fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.FileSystem,
                    description: fi.Name,
                    mainGroup: "1981",
                    subGroup: "New Approvals").Result;
            }

            var results = _client.File_GetVersionsInfo(fileRef);

            Assert.AreEqual(_maxVErsions, results.Count);

            // Go beyond size of array to ensure ok
            List<KeyValuePair<int, long>> tests = new List<KeyValuePair<int, long>>();

            for (int iVersion = 0; iVersion < _maxVErsions + 1; iVersion++)
            {
                var currentResult = _client.File_Get(fileRef, iVersion).Result;
                tests.Add(new KeyValuePair<int, long>(iVersion, currentResult?.Data?.LongLength ?? 0));
            }

            Assert.AreEqual(tests.Where(x => x.Value > 0).Count(), _maxVErsions);
        }

        [Test]
        public void FileUploadLargeUNC()
        {
            string filePath = Path.GetFullPath(_largeFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: "unc__" + fi.Name,
                fileLocation: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.FileSystem,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals").Result;


            var currentResult = _client.File_Get(fileRef, 0).Result;

            Assert.AreEqual(currentResult?.Data?.LongLength, contents.LongLength);

        }


        [Test]
        public void MultipleVersionsTestingBox()
        {
            string filePath = Path.GetFullPath(_stdImgFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = string.Empty;

            for (int i = 0; i < _maxVErsions + 5; i++)
            {
                fileRef = _client.File_UpsertAsync(
                    fileContents: contents,
                    fullFilename: "box__" + fi.Name,
                    fileLocation: FileStorageProvider.Box,
                    description: fi.Name,
                    mainGroup: "1981",
                    subGroup: "New Approvals").Result;
            }

            var results = _client.File_GetVersionsInfo(fileRef);

            Assert.AreEqual(_maxVErsions, results.Count);

            // Go beyond size of array to ensure ok
            List<KeyValuePair<int,long>> tests = new List<KeyValuePair<int, long>>();

            for (int iVersion = 0; iVersion < _maxVErsions + 1; iVersion++)
            {
                var currentResult = _client.File_Get(fileRef, iVersion).Result;
                tests.Add(new KeyValuePair<int, long>(iVersion, currentResult?.Data?.LongLength ?? 0));
            }

            Assert.AreEqual(tests.Where(x => x.Value > 0).Count(), _maxVErsions);
        }

        [Test]
        public void FileUploadLargeBox()
        {
            string filePath = Path.GetFullPath(_largeFile);

            FileInfo fi = new FileInfo(filePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = _client.File_UpsertAsync(
                fileContents: contents,
                fullFilename: "box__" + fi.Name,
                fileLocation: FileStorageProvider.Box,
                description: fi.Name,
                mainGroup: "987",
                subGroup: "New Approvals").Result;


            var currentResult = _client.File_Get(fileRef, 0).Result;

            Assert.AreEqual(currentResult?.Data?.LongLength, contents.LongLength);

        }

        [Test]
        public void MigrateAllFiles()
        {
            FileStorageProvider location = FileStorageProvider.Box;

            var fileCollections = _client.Files_GetAll();
            int moveFiles = fileCollections.Count;
            int moved = 0;

            foreach (var file in fileCollections)
            {
                var outcome = _client.File_Migrate(file.FileRef, location).Result;

                if (outcome.Success)
                {
                    moved++;
                }
            }

            Assert.AreEqual(moved, moveFiles);
        }


        [Test]
        public void GetAllFiles()
        {
            var fileCollections = _client.Files_GetAll();
            int count = fileCollections.Count;
            int allFiles = count;
            int fetched = 0;

            foreach (var file in fileCollections)
            {

                var result = _client.File_Get(fileRef: file.FileRef).Result;
                if (result.Data?.LongLength > 0)
                {
                    fetched++;
                }

            }


            Assert.AreEqual(fetched, allFiles);
        }


        private static List<string> LoopThroughDirectories(string rootDirectory)
        {
            List<string> result = new List<string>();

            try
            {
                foreach (string directory in Directory.GetDirectories(rootDirectory))
                {
                    result.AddRange(Directory.GetFiles(directory).ToList());
                    result.AddRange(LoopThroughDirectories(directory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return result;
        }
    }
}