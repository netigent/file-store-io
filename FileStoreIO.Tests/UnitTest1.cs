using Microsoft.Extensions.Options;
using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;
using Newtonsoft.Json;

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
                EnterpriseID = "194280382",
                BoxAppSettings = new BoxAppSettings()
                {
                    ClientID = "arjrpb75g08jpnz8d2ux68zn0q3jj1y2",
                    ClientSecret = "tZzkfbek4nXz3SliQmBuc1vZHTMgXEZf",
                    AppAuth = new AppAuth()
                    {
                        Passphrase = "121fb45a61423572b365cf611a559f35",
                        PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIFDjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQINldkuqqdR6oCAggA\nMBQGCCqGSIb3DQMHBAgjAoUE3gHicwSCBMgQ0BoVH3FwT7K31CyHqOmkW+T+fC42\nXLEit+br42+5hiISSJGIQfbyISOkMM7uze/FDgl2d2XG89/bkRWZ9Ta5k2sOZTZL\nMaGic/O+edV9SkR2f5C7zkycVzUEDzy0TLXa6PXz3Ix6tF+hut/VAjtr0GAJyW3G\n18/PRONV1V1dbLMwkaYYen/jepuTpd5KLtsalRBLY+jJ5qImPDa7fOVfA+Eh+BlC\n5pyTVRdG8kRRmAKQi4zbD0FaMygazApZWcOOCP8MWoBwhw3wMpf5mPQ1esvQ09Da\nntNONr5SgIOOCB4CnaKmfghmcKWQeRVlQDiIGmbrhvGYBiRlfNKn/ndamlzZOe9B\nEONcIK2Xlu3UN/1nCPaUxb55+dEn2gkHL5o+bGkgVh4i4Vlmcn7Ik02qkDnprWXz\nUUezH4Iw07uw6tov5S5c78JjTeSDI2TW9cnFTALuTHFsHDes4ROxINcEahFVnh4E\nox7CFD1NyFoE96jUTxWoZa1E9MW12a8oFnBIpvjWDZnUjvcB1U1qCfk0eSVLrgNZ\ndzJPoHHiGlTaiuwpuXeGT/fW1Dzef4NxnBa6xq8C4o4XgKMfMB38c/ao0EGUYbBD\nxwDwI2Z0HPWnbdEwrcmlatLNvXvHAvXVF4AYITtZazC1oc9PzSyeWdVBxyGIjGu2\nXoBp7hgGhdDL0jA5TdClVQe3E9kHWNxRZON9DuILv1wrcax6k+D3u/2wqPP6nT0R\nVC9at2LY185t59+T7iobzIvHig5tBGjXrLN/QQn5Wg1VAZGAkAkDTC+xQhDJCUKG\n6bmIMCdB9iLFyfzqdewykKJHhWg4b4EAZiRedpGiRpRREdnUlJ1ahsB7h986S090\nkrQ/q6XDusuw0iODqvsH/ZFk1ThXSNYp9kJOR0uYxAMfqn94tAelL+PseOc/Q3pg\nIXFVDmPG7PcDH0e1bNpPMWyPK3ncoidxBo9tSFk55s2X1QUkJWcj+fbu2MjAY1n6\nDXO03HS3H5WpMMiY0QG1+oT43KZcQYjX5Bm62ONN+VKIYOei6nEZ9fqKW734J2uW\ndv6GItqvZFssfWLGC4BDSAjqg5G0gUYdZ56DfoFcKgV7SRnoIhtD0/cImyFWI5cd\n116oqtXTxeYV2YeTqZX7AVosj35ALWpRfbybhmfsJNK8NjidOK6kTsz02xcJSo0D\n85ESJNZMXV8u+mLPXdpyCjv1/zzLccLrJRGvUucNaeOb8Im+04qxqhv5FMYf1U4F\nP3+0aSakooB1mmLLXkUdDC0hB+b/8YM22S8qrNUEjoZpbsTPNJ1YdwDw44Uiy494\ng41BgI4oGctz9KSPrDeVQQ6rRAj3yIRnvEY225UZ73PmsaLPert47ix39d55amjP\nwrE2pna/4n1r0S6KywKt4O5Uryy+2JMZDzHWrCyGsgE6jedv3RcqB3vnS8NMn9k6\nCxVWgvZMf8yKgt8KAkNW+gqZ0fkfvXUDXwvHf5RBSY3h6E2DJok+bTVyzifgfffL\n/NPzBOyeViLXKg8enuvjkHH540DQBDiBmC8OSyuLWBa2tEkECxr0zeZ/1XHQX+A2\n89bqIBCeqE3HEzfroDILbw6E9PD1Ju6Y2k054lPfJb15/bwHhP1gwuYY9PmO1T8m\nZgc=\n-----END ENCRYPTED PRIVATE KEY-----\n",
                        PublicKeyID = "qppe8gvp",
                    },
                },
                RootFolder = "0",
            };

            _client = new FileStoreIOClient(
            databaseConnection: "Server=.;Database=Hcai;UID=SA;PWD=abc1234==DEV;",
            fileStoreRoot: _rootFolder,
            maxVersions: _maxVErsions,
            dbSchema: "filestore",
            defaultFileStore: Netigent.Utils.FileStoreIO.Enum.FileStorageProvider.Box,
            boxConfig: exampleBox);
        }

        [Test]
        public void ConstructOptions()
        {
            string jsonSettings = @"{
                ""Database"": ""mysqlserver connection string"",
                ""FileStoreRoot"": ""c:\\temp\\files\\"",
                ""FilePrefix"": ""_$"",
                ""DatabaseSchema"": ""filestore"",
                ""StoreFileAsUniqueRef"": false,
                ""MaxVersions"": 5,
                ""DefaultStorage"": ""Box"",
                ""Box"": {
                  ""EnterpriseID"": ""123456789"",
                  ""RootFolder"": ""HR"",
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
                }
              }";

            var options = JsonConvert.DeserializeObject<FileStoreIOConfig>(jsonSettings);
    

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
            List<KeyValuePair<int, long>> tests = new List<KeyValuePair<int, long>>();

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


        [Test]
        public void DeleteFile()
        {
            FileStorageProvider location = FileStorageProvider.Box;

            var fileCollections = _client.File_DeleteAsync("_$9a885b869ac74942a4e1222cea562909").Result;
        }
    }
}