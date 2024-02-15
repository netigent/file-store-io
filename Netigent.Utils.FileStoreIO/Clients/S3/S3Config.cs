using Netigent.Utils.FileStoreIO.Enums;

namespace Netigent.Utils.FileStoreIO.Clients.S3
{
    public class S3Config : IConfig
    {
        public FileStorageProvider StoreType => FileStorageProvider.S3;

        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;

        public S3Config() { }

        public S3Config(
            string accessKey,
            string secretKey,
            string region,
            string bucket
            )
        {
            AccessKey = accessKey;
            SecretKey = secretKey;
            Region = region;
            BucketName = bucket;
        }
    }
}
