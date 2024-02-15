using System;

namespace Netigent.Utils.FileStoreIO.Enums
{
    public enum FileStorageProvider
    {
        UseDefault = -1,
        FileSystem,
        Database,
        Box,
        S3,
        Azure,
    }

    public static class FileStorageProviderExts
    {
        public static FileStorageProvider GetProvider(int providerId)
        {
            foreach (FileStorageProvider pt in Enum.GetValues(typeof(FileStorageProvider)))
            {
                if ((int)pt == providerId)
                {
                    return pt;
                }
            }

            return FileStorageProvider.UseDefault;
        }
    }
}
