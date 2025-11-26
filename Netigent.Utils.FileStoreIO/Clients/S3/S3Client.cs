using Amazon.S3;
using Amazon.S3.Model;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Helpers;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace Netigent.Utils.FileStoreIO.Clients.S3
{
    public class S3Client : IClient
    {
        #region Members
        // What type of implementation is it?
        public FileStorageProvider ProviderType => FileStorageProvider.S3;

        public bool HasInit { get; set; } = false;

        private char ClientDirectoryChar = '/';

        // If this exists it will be the 1st main folder used...
        private string AppCodePrefix { get; set; } = string.Empty;

        // Implement specific properties
        private string awsAccessKey;
        private string awsSecretKey;
        private string awsRegion;
        private string awsBucketName;
        private int _maxVersions;
        private IAmazonS3 _awsClient;
        #endregion

        #region ctor
        public S3Client()
        {
            HasInit = false;
        }

        public ResultModel Init(IConfig config, int maxVersions = 1, string appShortCode = "")
        {
            if (config?.StoreType == ProviderType)
            {
                S3Config? s3Config = config as S3Config;

                if (s3Config != null
                    && s3Config?.AccessKey?.Length > 0
                    && s3Config?.SecretKey?.Length > 0
                    && s3Config?.BucketName?.Length > 0
                    && s3Config?.Region?.Length > 0)
                {
                    awsAccessKey = s3Config.AccessKey;
                    awsSecretKey = s3Config.SecretKey;
                    awsBucketName = s3Config.BucketName;
                    awsRegion = s3Config.Region;
                    AppCodePrefix = appShortCode;

                    Amazon.RegionEndpoint? endpoint = GetRegionEndpoint(awsRegion);
                    if (endpoint != null)
                    {
                        AmazonS3Config amazonS3Config = new AmazonS3Config()
                        {
                            BufferSize = 65536, //64kb
                        };

                        _maxVersions = maxVersions >= 1 ? maxVersions : 1;
                        _awsClient = new AmazonS3Client(
                            awsSecretAccessKey: awsSecretKey,
                            awsAccessKeyId: awsAccessKey,
                            region: endpoint);

                        return new ResultModel(HasInit = true, string.Empty);
                    }
                }
            }

            return new ResultModel(HasInit = false, $"Bad or Missing Config, ensure region: {awsRegion} exists");
        }
        #endregion

        #region Implementation
        public async Task<long> IndexContentsAsync(ObservableCollection<FileStoreItem> indexList, string indexFolder, bool scopeToAppFolder)
        {
            // Should we prepend AppCodePrefix
            string searchingPath = scopeToAppFolder
                ? AppCodePrefix + ClientDirectoryChar.ToString() + indexFolder
                : indexFolder;

            IList<FileStoreItem> output = await GetPageAsync(searchingPath, null);

            if (output.Count > 0)
            {
                indexList.InsertRange(output);
            }

            return output.Count;
        }

        public async Task<string> SaveFileAsync(FileStoreItem fileModel)
        {
            // Aws Key restrictions
            // https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-keys.html
            string encAwsFileKey = $"{fileModel.Folder}{fileModel.NameWithVersion}"
                .ToRelativeFile(includePrefix: AppCodePrefix, useRelativeRoot: string.Empty, useSeperator: ClientDirectoryChar)
                .SafeFilename(replaceForbiddenFilenameChar: '_', allowExtendedAscii: true, [ClientDirectoryChar]);

            using (var ms = new MemoryStream(fileModel.Data))
            {
                var putRequest = new PutObjectRequest()
                {
                    BucketName = awsBucketName,
                    Key = encAwsFileKey,
                    InputStream = ms,
                    CalculateContentMD5Header = true,
                };

                var httpResponse = await _awsClient.PutObjectAsync(request: putRequest);
                if (httpResponse?.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return encAwsFileKey;
                }
            }

            return string.Empty;
        }

        public async Task<byte[]> GetFileAsync(string extClientRef)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = awsBucketName,
                Key = extClientRef,
            };

            var httpResponse = await _awsClient.GetObjectAsync(request: getRequest);
            using (var binaryReader = new BinaryReader(httpResponse.ResponseStream))
            {
                return binaryReader.ReadBytes((int)httpResponse.ResponseStream.Length);
            }
        }

        public async Task<bool> DeleteFileAsync(string extClientRef)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = awsBucketName,
                Key = extClientRef,
            };

            var httpResponse = await _awsClient.DeleteObjectAsync(request);
            return httpResponse.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        #endregion

        #region Internal functions
        private Amazon.RegionEndpoint? GetRegionEndpoint(string regionName)
        {
            foreach (var item in Amazon.RegionEndpoint.EnumerableAllRegions)
            {
                if (item.SystemName.Equals(regionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        private async Task<IList<FileStoreItem>> GetPageAsync(string indexFromLocation, string continuationToken = null)
        {
            List<FileStoreItem> output = new();
            ListObjectsV2Request request = new ListObjectsV2Request()
            {
                BucketName = awsBucketName,
                Prefix = indexFromLocation, // Take as-is
                ContinuationToken = continuationToken?.Length > 0 ? continuationToken : null,
            };

            var httpResponse = await _awsClient.ListObjectsV2Async(request);
            if (httpResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                for (int i = 0; i < httpResponse.S3Objects.Count; i++)
                {
                    var s3Object = httpResponse.S3Objects[i];
                    string unEncKey = HttpUtility.UrlDecode(s3Object.Key);
                    var extension = Path.GetExtension(unEncKey);

                    output.Add(new FileStoreItem
                    {
                        Name = Path.GetFileNameWithoutExtension(unEncKey),
                        Description = string.Empty,
                        ExtClientRef = s3Object.Key, // Ensure to take key as-is
                        Folder = unEncKey.ToRelativeFolder(),
                        Created = s3Object.LastModified,
                        Modified = s3Object.LastModified,
                        Extension = Path.GetExtension(unEncKey),
                        FileLocation = (int)FileStorageProvider.S3,
                        MimeType = MimeHelper.GetMimeType(extension),
                        SizeInBytes = s3Object.Size,
                    });
                }

                if (httpResponse.IsTruncated)
                {
                    output.AddRange(await GetPageAsync(indexFromLocation, httpResponse.NextContinuationToken));
                }
            }

            return output;
        }
        #endregion
    }
}