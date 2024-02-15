using Netigent.Utils.FileStoreIO.Clients.Box;
using Netigent.Utils.FileStoreIO.Clients.FileSystem;
using Netigent.Utils.FileStoreIO.Clients.S3;
using Netigent.Utils.FileStoreIO.Enums;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class FileStoreIOConfig
    {
        public static string Section { get; } = "FileStoreIO";

        public string Database { get; set; }

        /// <summary>
        /// If you are pointed to a shared folder, do you want to scope to this?
        /// </summary>
        public string AppPrefix { get; set; } = string.Empty;

        public string FilePrefix { get; set; } = "_$";

        public string DatabaseSchema { get; set; } = "fileStore";

        public int MaxVersions { get; set; } = 1;

        public FileStorageProvider DefaultStorage { get; set; } = FileStorageProvider.Database;

        public FileSystemConfig? FileSystem { get; set; }

        public BoxConfig? Box { get; set; }

        public S3Config? S3 { get; set; }
    }
}
