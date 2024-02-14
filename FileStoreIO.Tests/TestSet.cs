using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Clients.Box;
using Netigent.Utils.FileStoreIO.Clients.Box.Models;
using Netigent.Utils.FileStoreIO.Clients.FileSystem;
using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;
using Newtonsoft.Json;

namespace FileStoreIO.Tests
{
    public class TestSet
    {
        #region Members
        private IFileStoreIOClient _client { get; set; }

        private const string _xlargeFile = ".\\TestFiles\\TestGifFile_88MB.gif";
        private const string _largeFile = ".\\TestFiles\\whiteVid_37MB.mp4";
        private const string _stdPdfFile = ".\\TestFiles\\pdf_180KB.pdf";
        private const string _stdImgFile = ".\\TestFiles\\photo_A_3MB.jpg";

        private const string _dbConnection = "Server=.;Database=myDatabase;UID=mySa;PWD=myPassword;";
        private const string _dbSchema = "fileStore";
        private S3Config _s3Config;
        private BoxConfig _boxConfig;
        private FileSystemConfig _fsConfig;
        private int _maxVersionsOfFileToKeep = 10;
        private const string _appPrefix = "TestAppInSky";

        private const string outputLog = "testingLog.txt";
        #endregion

        #region ctor
        [SetUp]
        public void Setup()
        {
            // My S3
            _s3Config = new S3Config()
            {
                AccessKey = "ExampleAccessKey",
                BucketName = "my-example-bucket-name",
                Region = "us-west-2",
                SecretKey = "mysecretkeyinhere",
            };

            // My Boxx
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
                TimeoutInMins = 15,
            };

            _fsConfig = new FileSystemConfig()
            {
                RootFolder = @"C:\temp\Files\",
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

            var mostRecentFileRecordFromStore = localClient.Files_GetAll("")
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