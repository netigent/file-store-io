using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Clients.Box;
using Netigent.Utils.FileStoreIO.Clients.Box.Models;
using Netigent.Utils.FileStoreIO.Clients.FileSystem;
using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Models;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Netigent.Utils.FileStoreIOTests
{

    public class TestSet
    {
        #region Members
        private IFileStoreIOClient _clientNoPreFix { get; set; }
        private IFileStoreIOClient _clientWithPrefix { get; set; }



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

        private const string _dbConnection = "Server=.;Database=TestDb2;UID=mySa;PWD=myPassword;TrustServerCertificate=True;";
        private const string _dbSchema = "fileStore";
        private S3Config _s3Config;
        private BoxConfig _boxConfig;
        private FileSystemConfig _fsConfig;
        private int _maxVersionsOfFileToKeep = 3;
        private const string _appPrefix = "NETIGENT";

        private const string outputLog = "testingLog.txt";

        private List<string> _uploadedFileRefs;
        #endregion

        #region ctor
        [SetUp]
        public void Setup()
        {
            _uploadedFileRefs = new List<string>();

            // Netigent S3
            _s3Config = new S3Config()
            {
                AccessKey = "ExampleAccessKey",
                SecretKey = "mysecretkeyinhere+",
                Region = "us-west-2",
                BucketName = "my-example-bucket-name"
            };

            // Netigent Box
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

            _clientWithPrefix = new FileStoreIOClient(
                appPrefix: _appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            _clientNoPreFix = new FileStoreIOClient(
                appPrefix: string.Empty,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Clean up uploaded files from database and storage
            if (_uploadedFileRefs?.Any() == true)
            {
                foreach (var fileRef in _uploadedFileRefs)
                {
                    try
                    {
                        await _clientWithPrefix.File_DeleteAsync(fileRef);
                        TestContext.WriteLine($"Cleaned up: {fileRef}");
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"Failed to cleanup {fileRef}: {ex.Message}");
                    }
                }
            }

            // _client?.Dispose(); Doesnt implement IDisposible
            _clientWithPrefix = null;
            _clientNoPreFix = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up test storage directory if empty
            try
            {
                if (Directory.Exists(_fsLocation) && !Directory.EnumerateFileSystemEntries(_fsLocation).Any())
                {
                    Directory.Delete(_fsLocation);
                }
            }
            catch { }

            // Clear SQL connection pool
            SqlConnection.ClearAllPools();
        }
        #endregion

        [TestCase(FileStorageProvider.S3, _txtFile1, "myFile1.txt", "NETIGENT\\myFile1.txt", "./NETIGENT/")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile1, "myFile1.txt", "NETIGENT\\myFile1.txt", "./NETIGENT/")]
        [TestCase(FileStorageProvider.Database, _txtFile1, "myFile1.txt", null, "./NETIGENT/")]
        [TestCase(FileStorageProvider.S3, _txtFile1, "1\\myFile1.txt", "NETIGENT\\1\\myFile1.txt", "./NETIGENT/1/")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile1, "1\\myFile1.txt", "NETIGENT\\1\\myFile1.txt", "./NETIGENT/1/")]
        [TestCase(FileStorageProvider.Database, _txtFile1, "1\\myFile1.txt", null, "./NETIGENT/1/")]
        public async Task Upsert_FileSystem_Prefix(
             FileStorageProvider defaultStore,
             string sourceFile,
             string storagePath,
             string? expectedExtRef,
             string expectedFolder)
        {

            var client = _clientWithPrefix;

            // Fetch
            FileInfo fi = new FileInfo(Path.GetFullPath(sourceFile));
            byte[] contents = await File.ReadAllBytesAsync(fi.FullName);

            // Upload it
            string uploadedFileRef = await client.File_UpsertAsyncV2(storagePath, contents, fileStorageProvider: defaultStore, uploadedBy: "UnitTest");
            TestContext.WriteLine($"Uploaded: {uploadedFileRef}");

            // Assert - Database metadata record exists
            FileStoreItem? fsResult = client.File_GetVersionsInfo(uploadedFileRef).FirstOrDefault();
            Assert.That(fsResult, Is.Not.Null,
                $"Database metadata not found for FileRef: {uploadedFileRef}");

            // Assert - Storage matches
            Assert.That((int)fsResult.FileLocation, Is.EqualTo((int)defaultStore),
                $"FileStore Provider mismatch.\n" +
                $"  Expected: '{(int)defaultStore}'\n" +
                $"  Actual:   '{(int)fsResult.FileLocation}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - ExtClientRef matches expected (path normalization)
            string extRef = fsResult.ExtClientRefNoVersion == null ? null : fsResult.ExtClientRefNoVersion.ReplaceSeperators();
            string targetRef = expectedExtRef == null ? null : expectedExtRef.ReplaceSeperators();
            Assert.That(extRef, Is.EqualTo(targetRef),
                $"ExtClientRef mismatch.\n" +
                $"  Expected: '{targetRef}'\n" +
                $"  Actual:   '{extRef}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - Folder structure matches expected
            Assert.That(fsResult.Folder, Is.EqualTo(expectedFolder),
                $"Folder mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{fsResult.Folder}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - File size recorded in database
            Assert.That(fsResult.SizeInBytes, Is.EqualTo(contents.LongLength),
                $"Database size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {fsResult.SizeInBytes} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Act - Download file
            var downloadResult = await client.File_GetAsyncV2(uploadedFileRef);

            // Assert - Downloaded file size matches original
            Assert.That(downloadResult.Data.LongLength, Is.EqualTo(contents.LongLength),
                $"Downloaded file size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");


            var folderResults = client.Files_GetByFolder(fsResult.Folder);
            Assert.That(folderResults.Count > 0 && folderResults.Any(x => x.FileRef == uploadedFileRef),
                $"Missing from folder Search.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");
        }


        [TestCase(FileStorageProvider.S3, _txtFile1, "myFile1.txt", "myFile1.txt", "./")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile1, "myFile1.txt", "myFile1.txt", "./")]
        [TestCase(FileStorageProvider.Database, _txtFile1, "myFile1.txt", null, "./")]
        [TestCase(FileStorageProvider.S3, _txtFile1, "1\\myFile1.txt", "1\\myFile1.txt", "./1/")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile1, "1\\myFile1.txt", "1\\myFile1.txt", "./1/")]
        [TestCase(FileStorageProvider.Database, _txtFile1, "1\\myFile1.txt", null, "./1/")]
        public async Task Upsert_FileSystem_NoPrefix(
            FileStorageProvider defaultStore,
            string sourceFile,
            string storagePath,
            string? expectedExtRef,
            string expectedFolder)
        {

            var client = _clientNoPreFix;

            // Fetch
            FileInfo fi = new FileInfo(Path.GetFullPath(sourceFile));
            byte[] contents = await File.ReadAllBytesAsync(fi.FullName);

            // Upload it
            string uploadedFileRef = await client.File_UpsertAsyncV2(storagePath, contents, fileStorageProvider: defaultStore, uploadedBy: "UnitTest");
            TestContext.WriteLine($"Uploaded: {uploadedFileRef}");

            // Assert - Database metadata record exists
            FileStoreItem? fsResult = client.File_GetVersionsInfo(uploadedFileRef).FirstOrDefault();
            Assert.That(fsResult, Is.Not.Null,
                $"Database metadata not found for FileRef: {uploadedFileRef}");

            // Assert - Storage matches
            Assert.That((int)fsResult.FileLocation, Is.EqualTo((int)defaultStore),
                $"FileStore Provider mismatch.\n" +
                $"  Expected: '{(int)defaultStore}'\n" +
                $"  Actual:   '{(int)fsResult.FileLocation}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - ExtClientRef matches expected (path normalization)
            string extRef = fsResult.ExtClientRefNoVersion == null ? null : fsResult.ExtClientRefNoVersion.ReplaceSeperators();
            string targetRef = expectedExtRef == null ? null : expectedExtRef.ReplaceSeperators();
            Assert.That(extRef, Is.EqualTo(targetRef),
                $"ExtClientRef mismatch.\n" +
                $"  Expected: '{targetRef}'\n" +
                $"  Actual:   '{extRef}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - Folder structure matches expected
            Assert.That(fsResult.Folder, Is.EqualTo(expectedFolder),
                $"Folder mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{fsResult.Folder}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - File size recorded in database
            Assert.That(fsResult.SizeInBytes, Is.EqualTo(contents.LongLength),
                $"Database size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {fsResult.SizeInBytes} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Act - Download file
            var downloadResult = await client.File_GetAsyncV2(uploadedFileRef);

            // Assert - Downloaded file size matches original
            Assert.That(downloadResult.Data.LongLength, Is.EqualTo(contents.LongLength),
                $"Downloaded file size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");


            var folderResults = client.Files_GetByFolder(fsResult.Folder);
            Assert.That(folderResults.Count > 0 && folderResults.Any(x => x.FileRef == uploadedFileRef),
                $"Missing from folder Search.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");
        }

        [TestCase(FileStorageProvider.S3, _txtFile1, "myFileA.txt", 25, "myFileA.txt", "./")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile1, "myFileB.txt", 25, "myFileB.txt", "./")]
        [TestCase(FileStorageProvider.Database, _txtFile1, "myFileB.txt", 25, null, "./")]
        public async Task Upsert_FileSystem_HistoryOverride(
            FileStorageProvider defaultStore,
            string sourceFile,
            string storagePath,
            int copiesCount,
            string? expectedExtRef,
            string expectedFolder)
        {

            var client = _clientNoPreFix;

            // Fetch
            FileInfo fi = new FileInfo(Path.GetFullPath(sourceFile));
            byte[] contents = await File.ReadAllBytesAsync(fi.FullName);

            // Upload it
            string uploadedFileRef = await client.File_UpsertAsyncV2(storagePath, contents, fileStorageProvider: defaultStore, uploadedBy: "UnitTest", priorCopies: copiesCount);

            for (int i = 1; i < (copiesCount * 2); i++)
            {
                string newRef = await client.File_UpsertAsyncV2(storagePath, contents, fileStorageProvider: defaultStore, uploadedBy: "UnitTest", priorCopies: copiesCount);

                // Assert - Storage matches
                Assert.That(newRef, Is.EqualTo(uploadedFileRef),
                    $"FileRef Mismatch - Version issue?.\n" +
                    $"  Expected: '{uploadedFileRef}'\n" +
                    $"  Actual:   '{newRef}'");
            }

            TestContext.WriteLine($"Uploaded: {uploadedFileRef}");

            // Assert - Database metadata record exists
            var fsResults = client.File_GetVersionsInfo(uploadedFileRef);

            Assert.That(fsResults.Count, Is.EqualTo(copiesCount),
                $"Count Mismatch: {uploadedFileRef}");

            var fsResult = fsResults.FirstOrDefault();

            // Assert - Storage matches
            Assert.That((int)fsResult.FileLocation, Is.EqualTo((int)defaultStore),
                $"FileStore Provider mismatch.\n" +
                $"  Expected: '{(int)defaultStore}'\n" +
                $"  Actual:   '{(int)fsResult.FileLocation}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - ExtClientRef matches expected (path normalization)
            string extRef = fsResult.ExtClientRefNoVersion == null ? null : fsResult.ExtClientRefNoVersion.ReplaceSeperators();
            string targetRef = expectedExtRef == null ? null : expectedExtRef.ReplaceSeperators();
            Assert.That(extRef, Is.EqualTo(targetRef),
                $"ExtClientRef mismatch.\n" +
                $"  Expected: '{targetRef}'\n" +
                $"  Actual:   '{extRef}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - Folder structure matches expected
            Assert.That(fsResult.Folder, Is.EqualTo(expectedFolder),
                $"Folder mismatch.\n" +
                $"  Expected: '{expectedFolder}'\n" +
                $"  Actual:   '{fsResult.Folder}'\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Assert - File size recorded in database
            Assert.That(fsResult.SizeInBytes, Is.EqualTo(contents.LongLength),
                $"Database size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {fsResult.SizeInBytes} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");

            // Act - Download file
            var downloadResult = await client.File_GetAsyncV2(uploadedFileRef);

            // Assert - Downloaded file size matches original
            Assert.That(downloadResult.Data.LongLength, Is.EqualTo(contents.LongLength),
                $"Downloaded file size mismatch.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");


            var folderResults = client.Files_GetByFolder(fsResult.Folder);
            Assert.That(folderResults.Count > 0 && folderResults.Any(x => x.FileRef == uploadedFileRef),
                $"Missing from folder Search.\n" +
                $"  Expected: {contents.LongLength} bytes\n" +
                $"  Actual:   {downloadResult.Data.LongLength} bytes\n" +
                $"  FileRef:  '{uploadedFileRef}'");
        }


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
				""MaxVersions"": 1,
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


        //[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix)]
        //[TestCase(FileStorageProvider.S3, _stdImgFile, _appPrefix)]
        // [TestCase(FileStorageProvider.Box, _stdImgFile, _appPrefix)]


        //[TestCase(FileStorageProvider.FileSystem, _txtFile2, _appPrefix, "myfile.txt")]
        //[TestCase(FileStorageProvider.FileSystem, _txtFile3, _appPrefix, "|1\\myFile3.txt")]
        //[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "/2/main-photo3.jpg")]
        //[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "2/main-photod.jpg")]
        ////[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "xxxx\\20/main-photdo.jpg")]
        ////[TestCase(FileStorageProvider.Database, _largeFile, _appPrefix, "main-xxxx.jpg")]
        ////[TestCase(FileStorageProvider.FileSystem, _largeFile, _appPrefix, "main-xxxx11.jpg")]



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

        [TestCase(FileStorageProvider.FileSystem, _txtFile1, _appPrefix, "\\1\\myFile1.txt")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile2, _appPrefix, "myfile.txt")]
        [TestCase(FileStorageProvider.FileSystem, _txtFile3, _appPrefix, "|1\\myFile3.txt")]
        [TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "/2/main-photo3.jpg")]
        [TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "2/main-photod.jpg")]
        //[TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix, "xxxx\\20/main-photdo.jpg")]
        //[TestCase(FileStorageProvider.Database, _largeFile, _appPrefix, "main-xxxx.jpg")]
        //[TestCase(FileStorageProvider.FileSystem, _largeFile, _appPrefix, "main-xxxx11.jpg")]
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
                Assert.That(downloadedFile.FullName == mostRecentFileRecordFromStore.NameNoVersionWithExt && downloadedFile.Data.LongLength == mostRecentFileRecordFromStore.SizeInBytes);
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
            var indexLocationResult = localClient.File_IndexAsync(defaultStore, startPath: indexLocation, scopeToAppPrefix: appPrefix?.Length > 0).Result;

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

            string fileRef = _clientWithPrefix.File_UpsertAsyncV2(
                relationalFilePathAndName: $"{saveAsFolderPath}/{fi.Name}",
                fileContents: contents,
                fileStorageProvider: p).Result;

            var results = _clientWithPrefix.File_GetVersionsInfo(fileRef);

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

                string fileRef = _clientWithPrefix.File_UpsertAsyncV2($"{saveAsFolderPath}/{fi.Name}", contents, defaultStore).Result;
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

            var fileList = _clientWithPrefix.Files_GetAllV2(folderQuery);
            int i = 0;
            foreach (var item in fileList)
            {
                try
                {
                    var fileContents = _clientWithPrefix.File_GetAsyncV2(item.FileRef).Result;
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

            var fileList = _clientWithPrefix.Files_GetAllV2(folderQuery, false);
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


        [TestCase(FileStorageProvider.FileSystem, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _stdImgFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _largeFile, _appPrefix)]
        [TestCase(FileStorageProvider.FileSystem, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.S3, _xlargeFile, _appPrefix)]
        [TestCase(FileStorageProvider.Box, _xlargeFile, _appPrefix)]
        public async Task MoveFile_ByPathTags_ShouldRelocateFile(FileStorageProvider defaultStore, string uploadFilePath, string appPrefix)
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: _appPrefix,
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

            // Upload
            var originalPathTags = new[] { "Test", "Old" };
            string fileRef = localClient.File_UpsertAsyncV2(
                contents,
                fi.Name,
                originalPathTags
            ).Result;

            // Move file
            var newPathTags = new[] { "Test", "New", fi.Name };
            var moveResult = localClient.File_MoveAsync(fileRef, newPathTags).Result;

            Assert.IsTrue(moveResult.Success, string.Join(", ", moveResult.Messages));

            // Verify file is in new location
            var filesInNewLocation = localClient.Files_GetAllV2(newPathTags, true);
            Assert.IsTrue(filesInNewLocation.Any(f => f.FileRef == fileRef), "File not found in new location.");

            // Verify file is not in old location
            var filesInOldLocation = localClient.Files_GetAllV2(originalPathTags, true);
            Assert.IsFalse(filesInOldLocation.Any(f => f.FileRef == fileRef), "File still found in old location.");
        }

        [Test]
        public async Task MoveFile_ByPathTagsAndRef_ShouldRelocateFile()
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: _appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            // Upload
            string fileRef = "YOUR_FILE_REF_HERE";

            // Move file
            var newPathTags = new[] { "Test", "Latest", "latest_tags.jpg" };
            var moveResult = localClient.File_MoveAsync(fileRef, newPathTags).Result;

            Assert.IsTrue(moveResult.Success, string.Join(", ", moveResult.Messages));

            // Verify file is in new location
            var filesInNewLocation = localClient.Files_GetAllV2(newPathTags, true);
            Assert.IsTrue(filesInNewLocation.Any(f => f.FileRef == fileRef), "File not found in new location.");
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
        public async Task MoveFile_ByRelationalPath_ShouldRelocateFile(FileStorageProvider defaultStore, string uploadFilePath, string appPrefix)
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: _appPrefix,
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

            // Upload
            string originalRelationalPath = "Test/Old/" + fi.Name;
            string fileRef = localClient.File_UpsertAsyncV2(
                originalRelationalPath,
                contents,
                defaultStore).Result;

            // Move file
            string newRelationalPath = "Test/New/" + fi.Name;
            var moveResult = localClient.File_MoveAsync(fileRef, newRelationalPath).Result;

            Assert.IsTrue(moveResult.Success, string.Join(", ", moveResult.Messages));

            // Verify file is in new location
            var filesInNewLocation = localClient.Files_GetAllV2(newRelationalPath, true);
            Assert.IsTrue(filesInNewLocation.Any(f => f.FileRef == fileRef), "File not found in new location.");

            // Verify file is not in old location
            var filesInOldLocation = localClient.Files_GetAllV2(originalRelationalPath, true);
            Assert.IsFalse(filesInOldLocation.Any(f => f.FileRef == fileRef), "File still found in old location.");
        }

        [Test]
        public async Task MoveFile_ByRelationalPathAndRef_ShouldRelocateFile()
        {
            IFileStoreIOClient localClient = new FileStoreIOClient(
                appPrefix: _appPrefix,
                databaseConnection: _dbConnection,
                maxVersions: _maxVersionsOfFileToKeep,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: _boxConfig,
                s3Config: _s3Config,
                fileSystemConfig: _fsConfig);

            // Upload
            string fileRef = "YOUR_FILE_REF_HERE";

            // Move file
            string newRelationalPath = "Test/Latest/latest.jpg";
            var moveResult = localClient.File_MoveAsync(fileRef, newRelationalPath).Result;

            Assert.IsTrue(moveResult.Success, string.Join(", ", moveResult.Messages));

            // Verify file is in new location
            var filesInNewLocation = localClient.Files_GetAllV2(newRelationalPath, true);
            Assert.IsTrue(filesInNewLocation.Any(f => f.FileRef == fileRef), "File not found in new location.");
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