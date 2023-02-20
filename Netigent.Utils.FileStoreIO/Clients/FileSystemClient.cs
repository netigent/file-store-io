using Netigent.Utils.FileStoreIO.Models;
using System.IO;
using System;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public class FileSystemClient : IClient
    {
        #region Internal Props
        private const string TestFile = "TestFile90u34gj93p4n3p9wcfp3h4pc9qf.txt";
        private const string _fileFlag = @":\";
        private const string _internetFlag = @"://";
        private const string _networkFlag = @"\\";
        private string _fileStoreRoot { get; }

        #endregion

        #region Public Props
        public int _maxVersions { get; }
        
        public bool IsReady { get; set; } = false;
        #endregion

        #region ctor
        public FileSystemClient(string rootFolder)
        {
            _fileStoreRoot = rootFolder;

            //Test file to check access to the file system
            string testFile = Path.Combine(_fileStoreRoot, TestFile);

            try
            {
                if (!Directory.Exists(_fileStoreRoot))
                    Directory.CreateDirectory(_fileStoreRoot);

                if (System.IO.File.Exists(testFile))
                    System.IO.File.Delete(testFile);
                else
                {
                    System.IO.File.CreateText(testFile).Dispose();
                    System.IO.File.Delete(testFile);
                }

                IsReady = true;
            }
            catch
            {
                IsReady = false;
                throw;
            }
        }
        #endregion

        #region Implementation
        public async Task<string> SaveFileAsync(InternalFileModel fileModel)
        {
            try
            {
                // Will be stored in db
                string fileNameWithExt = $"{fileModel.Name}{fileModel.Extension}";
                string relativeFolder = Path.Combine(fileModel.MainGroup, fileModel.SubGroup);

                // Ensure folder exists
                string absoluteFolder = Path.Combine(_fileStoreRoot, relativeFolder);
                bool absoluteFolderExists = Directory.Exists(absoluteFolder);
                if (!absoluteFolderExists)
                {
                    Directory.CreateDirectory(absoluteFolder);
                }

                //Full filepath
                string absoluteFile = Path.Combine(absoluteFolder, fileNameWithExt);
                string relativeFile = Path.Combine(relativeFolder, fileNameWithExt);

                // Wipe any blocking files with same name...
                if (System.IO.File.Exists(absoluteFile))
                {
                    System.IO.File.Delete(absoluteFile);
                }

                // Write 2 Disk
                using (var stream = new FileStream(absoluteFile, FileMode.Create))
                {
                    await stream.WriteAsync(fileModel.Data, 0, fileModel.Data.Length);
                }

                // Relative File will be stored in index
                return relativeFile;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<InternalFileModel> GetFileAsync(string filePath)
        {
            string absoluteFilePath = GetAbsoluteFilePath(filePath);

            if (!File.Exists(absoluteFilePath))
                return null;

            FileInfo fileInfo = new FileInfo(absoluteFilePath);

            //Get from filesystem
            var memory = new MemoryStream();
            using (var stream = new FileStream(absoluteFilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return new InternalFileModel
            {
                Data = memory.ToArray(),
                Name = fileInfo.Name,
                Extension = fileInfo.Extension,
            };
        }

        public Task<bool> DeleteFileAsync(string fileId)
        {
            // This will be the true path
            string absoluteFilePath = GetAbsoluteFilePath(fileId);

            if (System.IO.File.Exists(absoluteFilePath))
            {
                System.IO.File.Delete(absoluteFilePath);
            }

            return Task.FromResult(true);
        }


        public Task<bool> DeleteFileAsync(long fileId, long? versionId)
        {
            return Task.FromResult(false);
        }
        #endregion

        #region Internal Functions
        private string GetAbsoluteFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }

            // Is the file absolute location??
            if (filePath.Contains(_fileFlag)
            || filePath.Contains(_internetFlag)
                || filePath.StartsWith(_networkFlag, StringComparison.InvariantCultureIgnoreCase))
            {
                return filePath;
            }
            // Treat as relative
            else
            {
                return Path.Combine(_fileStoreRoot, filePath);
            }
        }
        #endregion
    }
}
