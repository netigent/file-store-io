using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Clients.Box;
using Netigent.Utils.FileStoreIO.Clients.Box.Models;
using Netigent.Utils.FileStoreIO.Clients.FileSystem;
using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;
using Newtonsoft.Json;

namespace Netigent.Utils.FileStoreIOTests
{

    public class TestSet
    {
        #region Members
        private IFileStoreIOClient _client { get; set; }

        private const string _xlargeFile = ".\\TestFiles\\TestGifFile_88MB.gif";
        private const string _largeFile = ".\\TestFiles\\whiteVid_37MB.mp4";
        private const string _stdPdfFile = ".\\TestFiles\\pdf_180KB.pdf";
        private const string _stdImgFile = ".\\TestFiles\\photo_A_3MB.jpg";
        private const string _txtFile1 = ".\\TestFiles\\TextFile1.txt";
        private const string _txtFile2 = ".\\TestFiles\\TextFile2.txt";
        private const string _txtFile3 = ".\\TestFiles\\TextFile3.txt";
        private const string _fsLocation = "c:\\temp\\";

        private static readonly string[] _filesCollection =
        [
            @".\TestFiles\TestGifFile_88MB.gif",
            @".\TestFiles\whiteVid_37MB.mp4",
            @".\TestFiles\pdf_180KB.pdf",
            @".\TestFiles\photo_A_3MB.jpg"
        ];

        private const string _dbConnection = "Server=.;Database=TestDb2;UID=sa;PWD=abc1234==DEV;TrustServerCertificate=True;";
        private const string _dbSchema = "fileStore";
        private S3Config _s3Config;
        private BoxConfig _boxConfig;
        private FileSystemConfig _fsConfig;
        private int _maxVersionsOfFileToKeep = 10;
        private const string _appPrefix = "";

        private const string outputLog = "testingLog.txt";
        #endregion

        #region ctor
        [SetUp]
        public void Setup()
        {
            // FPDC S3
            _s3Config = new S3Config()
            {
                AccessKey = "ExampleAccessKey",
                SecretKey = "mysecretkeyinhere+",
                Region = "us-west-2",
                BucketName = "my-example-bucket-name"
            };

            // IBKS Dev Box
            _boxConfig = new BoxConfig()
            {
                EnterpriseID = "123456789",
                BoxAppSettings = new BoxAppSettings()
                {
                    ClientID = "exampleid12345",
                    ClientSecret = "examplesecret12345",
                    AppAuth = new BoxAppAuth()
                    {
                        Passphrase = "examplepassphrase12345",
                        PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
                        PublicKeyID = "abc1234",
                    },
                },
                AutoCreateRoot = true
            };

            _fsConfig = new FileSystemConfig()
            {
                RootFolder = _fsLocation,
                StoreFileAsUniqueRef = false,
            };

            _client = new FileStoreIOClient(
                appPrefix: _appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);
        }

        public void TearDown()
        {
            _client = null;
        }
        #endregion


        [Test]
        public void ConstructS3Config()
        {
            string jsonSettings = @"{
				""AccessKey"": ""ExampleAccessKey"",
				""SecretKey"": ""mysecretkeyinhere+"",
				""Region"":  ""us-west-2"",
				""Bucket"": ""my-example-bucket-name""
			}";

            var options = JsonConvert.DeserializeObject<S3Config>(jsonSettings);
        }

        [Test]
        public void ConstructBoxConfig()
        {
            string jsonSettings = @"{
		        ""EnterpriseID"": ""123456789"",
		        ""AutoCreateRoot"": false,
		        ""TimeoutInMins"": 15,
		        ""BoxAppSettings"": {
			        ""ClientID"": ""exampleid12345"",
			        ""ClientSecret"": ""examplesecret12345"",
			        ""AppAuth"": {
					        ""Passphrase"": ""examplepassphrase12345"",
					        ""PrivateKey"": ""-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n"",
					        ""PublicKeyID"": ""abc1234""
			        }
		        }
	        }";

            var options = JsonConvert.DeserializeObject<BoxConfig>(jsonSettings);
            Assert.That(options.BoxAppSettings.AppAuth.PublicKeyID == "abc1234" && options.EnterpriseID == "123456789", "Construct failed");
        }

        [Test]
        public void ConstructFSConfig()
        {
            string jsonSettings = @"{
                    ""RootFolder"": ""c:\\temp\\files\\"",
                   ""StoreFileAsUniqueRef"": false
            }";

            var options = JsonConvert.DeserializeObject<FileSystemConfig>(jsonSettings);
            Assert.That(options.RootFolder == "c:\\temp\\files\\", "Construct failed");
        }

        [Test]
        public void ConstructBasicNoProvidersOptions()
        {
            string jsonSettings = @"{
                ""Database"": " + _dbConnection + @",
                ""FileStoreRoot"": ""c:\\temp\\files\\"",
                ""FilePrefix"": ""_$"",
                ""DatabaseSchema"": ""filestore"",
                ""StoreFileAsUniqueRef"": false,
                ""MaxVersions"": 5,
                ""DefaultStorage"": ""Box"",
              }";

            var options = JsonConvert.DeserializeObject<FileStoreIOConfig>(jsonSettings);

            Assert.That(options.DefaultStorage == FileStorageProvider.Box, "Construct failed");
        }

        [Test]
        public void ConstructOptions()
        {
            string jsonSettings = @"{
				""Database"": " + _dbConnection + @",
				""AppPrefix"": ""MyAppToScopeTo"",               
				""FilePrefix"": ""_$"",
				""DatabaseSchema"": ""filestore"",
				""MaxVersions"": 5,
				""DefaultStorage"": ""S3"",
				""S3"": {
					""AccessKey"": ""ExampleAccessKey"",
					""SecretKey"": ""mysecretkeyinhere+"",
					""Region"":  ""us-west-2"",
					""Bucket"": ""my-example-bucket-name""
					},
				""Box"": {
					""EnterpriseID"": ""123456789"",
					""AutoCreateRoot"": false,
					""TimeoutInMins"": 15,
					""BoxAppSettings"": {
						""ClientID"": ""exampleid12345"",
						""ClientSecret"": ""examplesecret12345"",
						""AppAuth"": {
								""Passphrase"": ""examplepassphrase12345"",
								""PrivateKey"": ""-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n"",
								""PublicKeyID"": ""abc1234""
						}
					}
				},
				""FileSystem"": {
				    ""RootFolder"": ""c:\\temp\\files\\"",
				    ""StoreFileAsUniqueRef"": false
				}
		    }";

            var options = JsonConvert.DeserializeObject<FileStoreIOConfig>(jsonSettings);

            Assert.That(
                options.DefaultStorage == FileStorageProvider.S3
                && options.Box.EnterpriseID == "123456789"
                && options.FileSystem.RootFolder == "c:\\temp\\files\\"
                && options.S3.Region == "us-west-2",
                "Construct failed"
                );
        }

        [Test]
        public void ConstructEmptyProviders()
        {
            string jsonSettings = @"{
                ""Database"": " + _dbConnection + @",
                ""FilePrefix"": ""_$"",
                ""DatabaseSchema"": ""filestore"",
                ""MaxVersions"": 5,
                ""DefaultStorage"": ""Database"",
                ""S3"": {},
                ""Box"": {},
                ""FileSystem"": {}
              }";

            var options = JsonConvert.DeserializeObject<FileStoreIOConfig>(jsonSettings);

            Assert.That(
                options.DefaultStorage == FileStorageProvider.Database
                && options.Box == null
                && options.FileSystem == null
                && options.S3.Region == null,
                "Construct failed"
                );
        }


        [TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _xlargeFile, _appPrefix)]
        public void UploadFileToDefault(FileStorageProvider defaultStore, string uploadFilePath, string appPrefix)
        {
            DateTime start = DateTime.UtcNow;

            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: defaultStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            string fileAbsolutePath = Path.GetFullPath(uploadFilePath);
            FileInfo fi = new FileInfo(fileAbsolutePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);
            string relativePath = Path.Combine("MainFolder", "New-Approvals", $"DEF__{DateTime.UtcNow.Minute}__{fi.Name}");

            string fileRef = localClient.File_UpsertAsyncV2(
                fileContents: contents,
                relationalFilePathAndName: relativePath,
                description: fi.Name
                ).Result;

            var results = localClient.File_GetVersionsInfo(fileRef);

            double taken = (DateTime.UtcNow - start).TotalSeconds;

            Assert.That(results.FirstOrDefault(x => x.FileLocation == (int)defaultStore)?.SizeInBytes == contents.LongLength);


            string outcome = $"Upload Provider: {defaultStore}, Size: {contents.LongLength / 1024}kb, Time: {taken}secs";

        }

        //[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix)]
        //[TestCase(FileStorageProvider.S3, _stdImgFile, _appPrefix)]
        // [TestCase(FileStorageProvider.Box, _stdImgFile, _appPrefix)]

        [TestCase(FileStorageProvider.FileSystem, _txtFile1, _appPrefix, "/tuesday/photo/folder1/myFile.txt")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile2, _appPrefix, "/tuesday/photo/myFile.txt")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile3, _appPrefix, "/tuesday/myFile.txt")]
        //[TestCase(FileStorageProvider.S3, _largeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.Box, _largeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.Database, _largeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.FileSystem, _xlargeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.S3, _xlargeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.Box, _xlargeFile, _appPrefix)]
        //[TestCase(FileStorageProvider.Database, _xlargeFile, _appPrefix)]
        public void LocationPathTestUpload(FileStorageProvider defaultStore, string sourceFile, string appPrefix, string relativePath)
        {
            List<string> messages = new List<string>();
            DateTime start = DateTime.UtcNow;

            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: defaultStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            string fileAbsolutePath = Path.GetFullPath(sourceFile);
            FileInfo fi = new FileInfo(fileAbsolutePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            start = DateTime.UtcNow;
            string uploadedFileRef = localClient.File_UpsertAsyncV2(relativePath, contents).Result;

            var metadataResult = localClient.File_GetVersionsInfo(uploadedFileRef);
            var downloadResult = localClient.File_GetAsyncV2(uploadedFileRef).Result;

            Assert.That(
                downloadResult.Data.LongLength == contents.LongLength
                && metadataResult.FirstOrDefault().SizeInBytes == contents.LongLength, "Filesize matchinging...");

            double taken = (DateTime.UtcNow - start).TotalSeconds;
            string outcome = $"Upload Provider: {defaultStore}, Size: {contents.LongLength / 1024}kb, Time: {taken}secs";


            SaveToLogs(new ResultModel { Success = true, Messages = new List<string>() { outcome } });
        }

        [TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, _xlargeFile, _appPrefix)]
        public void FullCycleToDefault(FileStorageProvider defaultStore, string uploadFilePath, string appPrefix)
        {
            List<string> messages = new List<string>();
            DateTime start = DateTime.UtcNow;

            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: defaultStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            string fileAbsolutePath = Path.GetFullPath(uploadFilePath);
            FileInfo fi = new FileInfo(fileAbsolutePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string relativePath = Path.Combine("MainFolder", $"Application-[a]-{DateTime.UtcNow.ToString("hhMMss")}", $"DEF__{DateTime.UtcNow.Minute}__{fi.Name}");

            start = DateTime.UtcNow;
            string uploadedFileRef = localClient.File_UpsertAsyncV2(
                fileContents: contents,
                relationalFilePathAndName: relativePath,
                description: fi.Name
                ).Result;



            var metadataResult = localClient.File_GetVersionsInfo(uploadedFileRef);
            var downloadResult = localClient.File_GetAsyncV2(uploadedFileRef).Result;

            Assert.That(
                downloadResult.Data.LongLength == contents.LongLength
                && metadataResult.FirstOrDefault().SizeInBytes == contents.LongLength, "Filesize matchinging...");

            var deleteResult = localClient.File_DeleteAsync(uploadedFileRef).Result;
            Assert.That(deleteResult, "Delete failed");

            var postDeleteResult = localClient.File_GetVersionsInfo(uploadedFileRef);
            Assert.That(postDeleteResult?.Count == 0, "Delete failed");

            double taken = (DateTime.UtcNow - start).TotalSeconds;
            string outcome = $"Upload Provider: {defaultStore}, Size: {contents.LongLength / 1024}kb, Time: {taken}secs";


            SaveToLogs(new ResultModel { Success = true, Messages = new List<string>() { outcome } });
        }

        [TestCase(FileStorageProvider.FileSystem, "TestApp1")]
        [TestCase(FileStorageProvider.S3, "")]
        [TestCase(FileStorageProvider.Box, "TestApp1")]
        [TestCase(FileStorageProvider.Box, "TestApp2")]
        public void FetchExistingFiles(FileStorageProvider defaultStore, string appPrefix)
        {
            DateTime start = DateTime.UtcNow;
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: defaultStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            var mostRecentFileRecordFromStore = localClient.Files_GetAllV2("")
                .OrderByDescending(x => x.Modified)
                .FirstOrDefault(x => x.FileLocation == (int)defaultStore);

            if (mostRecentFileRecordFromStore != null)
            {

                double taken = (DateTime.UtcNow - start).TotalSeconds;
                var downloadedFile = localClient.File_GetAsyncV2(mostRecentFileRecordFromStore.FileRef).Result;
                string outcome = $"Download Provider: {defaultStore}, Size: {downloadedFile.Data.LongLength / 1024}kb, Time: {taken}secs";
                Assert.That(downloadedFile.Name == mostRecentFileRecordFromStore.OrginalNameWithExt && downloadedFile.Data.LongLength == mostRecentFileRecordFromStore.SizeInBytes);
            }
            else
            {
                Assert.Warn($"No recent files in Provider {default} for {_appPrefix}");
            }
        }

        [TestCase(FileStorageProvider.FileSystem, "", "TestApp1")]
        [TestCase(FileStorageProvider.FileSystem, "", "TestApp2")]
        [TestCase(FileStorageProvider.FileSystem, "", "")]
        [TestCase(FileStorageProvider.S3, "", "")]
        [TestCase(FileStorageProvider.S3, "", "TestApp1")]
        [TestCase(FileStorageProvider.Box, "", "")]
        [TestCase(FileStorageProvider.Box, "", "TestApp2")]
        [TestCase(FileStorageProvider.Box, "", "TestApp1")]
        public void IndexLocation(FileStorageProvider defaultStore, string indexLocation, string appPrefix)
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: defaultStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            // Upload the File
            var indexLocationResult = localClient.File_IndexAsync(defaultStore, indexFrom: indexLocation, scopeToAppPrefix: appPrefix?.Length > 0).Result;

            SaveToLogs(indexLocationResult);
            Assert.That(indexLocationResult.Success = true);
        }

        /// <summary>
        /// Backup fileStore Index before you proceed
        /// </summary>
        /// <param name="fromStore"></param>
        /// <param name="toStore"></param>
        /// <param name="shouldMoveFile"></param>
        [TestCase(FileStorageProvider.Box, FileStorageProvider.FileSystem, false)]
        [TestCase(FileStorageProvider.Box, FileStorageProvider.S3, false)]
        [TestCase(FileStorageProvider.Box, FileStorageProvider.Database, false)]

        [TestCase(FileStorageProvider.S3, FileStorageProvider.FileSystem, false)]
        [TestCase(FileStorageProvider.S3, FileStorageProvider.Database, false)]
        [TestCase(FileStorageProvider.S3, FileStorageProvider.Box, false)]

        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.S3, false)]
        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.Box, false)]
        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.Database, false)]

        [TestCase(FileStorageProvider.Box, FileStorageProvider.FileSystem, true)]
        [TestCase(FileStorageProvider.Box, FileStorageProvider.S3, true)]
        [TestCase(FileStorageProvider.Box, FileStorageProvider.Database, true)]

        [TestCase(FileStorageProvider.S3, FileStorageProvider.FileSystem, true)]
        [TestCase(FileStorageProvider.S3, FileStorageProvider.Database, true)]
        [TestCase(FileStorageProvider.S3, FileStorageProvider.Box, true)]

        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.S3, true)]
        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.Box, true)]
        [TestCase(FileStorageProvider.FileSystem, FileStorageProvider.Database, true)]

        public void MigrateFile(FileStorageProvider fromStore, FileStorageProvider toStore, bool shouldMoveFile)
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: "",
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: toStore,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            var sourceFileList = localClient.Files_GetAllV2("").Where(x => x.FileLocation == (int)fromStore).ToList();
            Assert.That(sourceFileList.Count() > 0, "No source files to test moving");

            var testFileToMigrate = sourceFileList.First();
            var migrateFileResult = localClient.File_Migrate(testFileToMigrate.FileRef, toStore, shouldMoveFile).Result;

            SaveToLogs(migrateFileResult);
            Assert.That(migrateFileResult.Success, migrateFileResult.Messages.First());
        }

        // Database
        [TestCase(FileStorageProvider.Database, "/Database/Project 1/Budget Files", _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "/Database/Project 1/Progress Photos", _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "/Database/Project 2/Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "\\Database\\Project A\\Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "\\Database\\Project A\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "\\Database\\Project B\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "Database|Project Z1|Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "Database|Project Z1|Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Database, "Database|Project Z2|Progress Photos", _stdImgFile, _appPrefix)]

        // FileSystem
        [TestCase(FileStorageProvider.FileSystem, "/FolderStore/Project 1/Budget Files", _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "/FolderStore/Project 1/Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "/FolderStore/Project 2/Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "\\FolderStore\\Project A\\Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "\\FolderStore\\Project A\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "\\FolderStore\\Project B\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "FolderStore|Project Z1|Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "FolderStore|Project Z1|Progress Photos", _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, "FolderStore|Project Z2|Progress Photos", _xlargeFile, _appPrefix)]

        // AWS
        [TestCase(FileStorageProvider.S3, "/S3/Project 1/Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "/S3/Project 1/Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "/S3/Project 2/Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "\\S3\\Project A\\Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "\\S3\\Project A\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "\\S3\\Project B\\Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "S3|Project Z1|Budget Files", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "S3|Project Z1|Progress Photos", _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, "S3|Project Z2|Progress Photos", _stdImgFile, _appPrefix)]
        public void BulkMixedPopulation(FileStorageProvider fsp, string saveAsFolderPath, string uploadingFile, string appPrefix)
        {
            DateTime start = DateTime.UtcNow;

            FileStorageProvider p = fsp;

            string fileAbsolutePath = Path.GetFullPath(uploadingFile);
            FileInfo fi = new FileInfo(fileAbsolutePath);
            byte[] contents = File.ReadAllBytes(fi.FullName);

            string fileRef = _client.File_UpsertAsyncV2(
                relationalFilePathAndName: $"{saveAsFolderPath}/{fi.Name}",
                fileContents: contents,
                fileStorageProvider: p).Result;

            var results = _client.File_GetVersionsInfo(fileRef);

            double taken = (DateTime.UtcNow - start).TotalSeconds;

            Assert.That(results.FirstOrDefault(x => x.FileLocation == (int)fsp)?.SizeInBytes == contents.LongLength);
            string outcome = $"Upload Provider: {fsp}, Size: {contents.LongLength / 1024}kb, Time: {taken}secs";
        }

        // Database
        [TestCase(FileStorageProvider.Database, "/Database/Project M/Budget Files")]

        // FileSystem
        [TestCase(FileStorageProvider.FileSystem, "\\FolderStore\\Project M\\Budget Files")]

        // AWS
        [TestCase(FileStorageProvider.S3, "|S3|Project M|Budget Files")]

        public void LoopingLoaderPopulation(FileStorageProvider defaultStore, string saveAsFolderPath)
        {
            DateTime start = DateTime.UtcNow;

            foreach (var item in _filesCollection)
            {
                string fileAbsolutePath = Path.GetFullPath(item);
                FileInfo fi = new FileInfo(fileAbsolutePath);
                byte[] contents = File.ReadAllBytes(fi.FullName);

                string fileRef = _client.File_UpsertAsyncV2($"{saveAsFolderPath}/{fi.Name}", contents, defaultStore).Result;
            }

            double taken = (DateTime.UtcNow - start).TotalSeconds;
        }

        // Query format testing
        [TestCase("FolderStore|Project 1|Budget Files")]
        [TestCase("FolderStore|Project 1")]
        [TestCase("FolderStore")]
        [TestCase("S3")]

        [TestCase("\\FolderStore\\Project 1\\Budget Files")]
        [TestCase("\\FolderStore\\Project 1")]
        [TestCase("\\FolderStore")]
        [TestCase("\\DATABASE")]

        [TestCase("FolderStore/Project 1/Budget Files")]
        [TestCase("FolderStore/Project 1")]
        [TestCase("FolderStore/")]
        [TestCase("/")]

        public void QueryTestingAndDownload(string folderQuery)
        {
            DateTime start = DateTime.UtcNow;

            var fileList = _client.Files_GetAllV2(folderQuery);
            int i = 0;
            foreach (var item in fileList)
            {
                try
                {
                    var fileContents = _client.File_GetAsyncV2(item.FileRef).Result;
                    if (fileContents?.Data?.Length > 0)
                    {
                        i++;
                    }
                    else
                    {
                        throw new Exception("Failed to get file");
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }


            double taken = (DateTime.UtcNow - start).TotalSeconds;
            Assert.That(fileList?.Count > 0 && fileList.Count == i, "Delete failed");
        }


        // Query format testing
        [TestCase("S3/Project 1")]
        [TestCase("FolderStore|Project 1|Budget Files")]
        [TestCase("FolderStore|Project 1")]
        [TestCase("FolderStore")]
        [TestCase("")]

        [TestCase("\\FolderStore\\Project 1\\Budget Files")]
        [TestCase("\\FolderStore\\Project 1")]
        [TestCase("\\FolderStore")]
        [TestCase("\\")]

        [TestCase("FolderStore/Project 1/Budget Files")]
        [TestCase("FolderStore/Project 1")]
        [TestCase("FolderStore/")]
        [TestCase("/")]

        public void QueryTestingAndDownloadMultipleINs(string folderQuery)
        {
            DateTime start = DateTime.UtcNow;

            var fileList = _client.Files_GetAllV2(folderQuery, false);
            int i = 0;

            foreach (var item in fileList)
            {

                IFileStoreIOClient localClient = new FileStoreIOClient(
                    appPrefix: _appPrefix,
                    databaseConnection: _dbConnection,
                    maxVersions: _maxVersionsOfFileToKeep,
                    dbSchema: _dbSchema,
                    defaultFileStore: FileStorageProvider.Database,
                    boxConfig: _boxConfig,
                    s3Config: _s3Config,
                    fileSystemConfig: _fsConfig);

                try
                {
                    var fileContents = localClient.File_GetAsyncV2(item.FileRef).Result;
                    if (fileContents?.Data?.Length > 0)
                    {
                        i++;
                    }
                    else
                    {
                        throw new Exception("Failed to get file");
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }


            double taken = (DateTime.UtcNow - start).TotalSeconds;
            Assert.That(fileList?.Count > 0 && fileList.Count == i, "Delete failed");
        }


        private string SaveToLogs(ResultModel results)
        {
            string outputPath = Path.Combine(System.IO.Path.GetTempPath(), DateTime.UtcNow.ToString("yyyyMMdd"));
            Directory.CreateDirectory(outputPath);

            string outputFile = Path.Combine(outputPath, outputLog);
            File.AppendAllLines(outputFile, results.Messages);

            return outputFile;
        }
    }
}