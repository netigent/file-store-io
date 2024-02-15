using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Enums;

namespace Netigent.Utils.FileStoreIO.Clients.FileSystem
{
    public class FileSystemConfig : IConfig
    {
        public FileStorageProvider StoreType => FileStorageProvider.FileSystem;

        public string RootFolder { get; set; } = string.Empty;

        public bool StoreFileAsUniqueRef { get; set; } = false;

        public FileSystemConfig() { }

        public FileSystemConfig(string rootFolder, bool storeFileAsUniqueRef)
        {
            RootFolder = rootFolder;
            StoreFileAsUniqueRef = storeFileAsUniqueRef;
        }
    }
}
