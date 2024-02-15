using Microsoft.IdentityModel.Tokens;
using Netigent.Utils.FileStoreIO.Clients.Box.Models;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Helpers;
using Netigent.Utils.FileStoreIO.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients.Box
{
    public class BoxClient : IClient
    {
        #region Members
        public FileStorageProvider ProviderType => FileStorageProvider.Box;

        public bool HasInit { get; set; } = false;

        private string access_token { get; set; }

        private HttpClient BoxHttpClient { get; set; }

        private DateTime _expiration { get; set; }

        private long RootFolderId { get; set; } = 0;
        // If this exists it will be the 1st main folder used...
        private string RootContentFolder { get; set; } = string.Empty;

        private int MaxVersions { get; set; }

        private ObservableCollection<InternalFileModel> _indexList { get; set; }

        private const string ApiUrl = "https://api.box.com/2.0";

        private const string UploadUrl = "https://upload.box.com/api/2.0/files";
        private BoxConfig BoxConfig { get; set; }
        private RSA RsaKey { get; set; }

        private char ClientDirectoryChar => '/';
        #endregion

        #region ctor
        public BoxClient()
        {
        }

        public ResultModel Init(IConfig config, int maxVersions = 1, string appShortCode = "")
        {
            string failInitReason = string.Empty;
            if (config?.StoreType == ProviderType)
            {
                BoxConfig = config as BoxConfig;
                MaxVersions = maxVersions >= 1 ? maxVersions : 1;
                RootContentFolder = appShortCode;
                RootFolderId = 0;

                if (BoxConfig?.EnterpriseID?.Length > 0)
                {
                    try
                    {
                        // Next, we use BouncyCastle's PemReader to read the 
                        // decrypt the private key into a RsaPrivateCrtKeyParameters
                        // object
                        var appAuth = BoxConfig.BoxAppSettings.AppAuth;
                        var stringReader = new StringReader(appAuth.PrivateKey);
                        var passwordFinder = new PasswordFinder(appAuth.Passphrase);
                        var pemReader = new PemReader(stringReader, passwordFinder);
                        var keyParams = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();

                        // In the end, we will use this key in the next steps
                        RsaKey = CreateRSAProvider(ToRSAParameters(keyParams));

                        // Make the POST call to the authentication endpoint
                        BoxHttpClient = new HttpClient();
                        BoxHttpClient.Timeout = new TimeSpan(0, BoxConfig.TimeoutInMins, 0);

                        // Startup the client and get ID etc
                        _ = PreflightChecks().Result;

                        if (HasInit)
                        {
                            // If we have an appPrefix folder, lets try and use it as artifical root
                            if (!string.IsNullOrEmpty(RootContentFolder))
                            {
                                // We're dealing with a string based rootName
                                long resolvedId = ResolveFolderPath(folderPath: RootContentFolder, parentId: 0).Result;
                                if (resolvedId == -1)
                                {
                                    // Root Folder is string, but not found and autoCreate was false...
                                    HasInit = false;
                                    failInitReason = $"RootFolder: {RootContentFolder}, not found and AutoCreate is off";
                                }
                                else
                                {
                                    // String named root found and id resolved
                                    RootFolderId = resolvedId;
                                }
                            }
                            else
                            {
                                RootFolderId = 0;
                            }
                        }
                        else
                        {
                            failInitReason = "Check Credentials";
                        }
                    }
                    catch (Exception ex)
                    {
                        failInitReason = $"BadConfig {ex.Message}";
                    }
                }
            }
            else
            {
                failInitReason = "Missing or BadConfig";
            }

            return new ResultModel(HasInit, failInitReason);
        }
        #endregion

        #region Implementation
        public async Task<long> IndexContentsAsync(ObservableCollection<InternalFileModel> indexList, string indexFolderPath, bool scopeToAppFolder)
        {
            long initalFolderId = await CreateOrResolveFolderId(indexFolderPath, autoCreate: false);
            if (initalFolderId >= 0)
            {
                IList<InternalFileModel> output = await IndexFolderAsync(initalFolderId, indexFolderPath);

                if (output.Count > 0)
                {
                    indexList.InsertRange(output);
                }

                return output.Count;
            }

            return 0;
        }

        private async Task<IList<InternalFileModel>> IndexFolderAsync(long folderToIndex, string parentFolderPath)
        {
            List<InternalFileModel> output = new();
            var contents = await GetSubItemsListAsync(folderToIndex);

            long totalItems = contents.LongCount();
            if (totalItems > 0)
            {
                for (int i = 0; i < totalItems; i++)
                {
                    // Get ref to item
                    var item = contents[i];

                    // Parse BoxRef
                    BoxRef boxFolderRef = new BoxRef(item);

                    if (item.ItemType == BoxItemType.Folder)
                    {
                        string nextFolder = parentFolderPath?.Length > 0
                            ? $"{parentFolderPath}{ClientDirectoryChar}{item.Name}"
                            : item.Name;

                        output.AddRange(await IndexFolderAsync(boxFolderRef.BoxId, nextFolder));
                    }
                    else if (item.ItemType == BoxItemType.File)
                    {
                        var fi = new FileInfo(item.Name);
                        string ext = !string.IsNullOrEmpty(fi.Extension) ? fi.Extension : string.Empty;
                        string nameOnly = !string.IsNullOrEmpty(fi.Extension) ? fi.Name.Replace(fi.Extension, string.Empty) : fi.Name;
                        string mimeType = MimeHelper.GetMimeType(fi.Extension);

                        BoxEntry? getFileInfo = (await GetFileInfo(boxFolderRef.BoxId.ToString())).FirstOrDefault();

                        output.Add(new InternalFileModel
                        {
                            Name = nameOnly,
                            Description = getFileInfo?.Description ?? string.Empty,
                            ExtClientRef = boxFolderRef.AsFilePath,
                            Created = getFileInfo?.CreatedDt,
                            Modified = getFileInfo?.ModifiedDt,
                            Extension = ext,
                            FileLocation = (int)FileStorageProvider.Box,
                            FolderPath = RootFolderId > 0
                                ? $"{RootContentFolder}{ClientDirectoryChar}{parentFolderPath}" // Valid AppFolder was found prepend it
                                : parentFolderPath, // No App Prefix found...
                            MimeType = mimeType,
                            SizeInBytes = getFileInfo?.Size ?? -1,
                        });
                    }
                }
            }

            return output;
        }

        public async Task<BoxEntry> GetFolderAsync(long folderId)
        {
            string requestUrl = $"{ApiUrl}/folders/{folderId}";
            var response = await GetAsync<BoxEntry>(requestUrl);

            return response;
        }

        public async Task<string> SaveFileAsync(InternalFileModel fileModel)
        {
            if (fileModel.Data?.Length == null)
            {
                // Nothing to upload
                return string.Empty;
            }
            PathInfo fileInfo = fileModel.GetPathInfo(usePathSeparator: ClientDirectoryChar);
            long folderId = await ResolveFolderPath(fileInfo.FolderPath, RootFolderId);
            string endpoint = $"{UploadUrl}/content";
            string boxAttribute = BoxAttribute(fileInfo.Filename, folderId);

            // Check for conflicts i.e. file already exists
            BoxApiResult uploadResult = await UploadPreflightAsync(boxAttribute);
            BoxRef boxRef = new BoxRef(uploadResult.Result);

            if (uploadResult.IsSuccess)
            {
                uploadResult = await PostContentAsync(endpoint, boxAttribute, fileModel);
                if (uploadResult.IsSuccess)
                {
                    boxRef = new BoxRef(uploadResult.Result);
                    return boxRef.AsFilePath;
                }
            }

            if (!uploadResult.IsSuccess && uploadResult.StatusCode == (int)HttpStatusCode.NotFound)
            {
                throw new Exception($"Folder Path Doesnt Exist '{fileInfo.FolderPath}' and autoCreate='{BoxConfig.AutoCreateRoot.ToString()}");
            }
            if (!uploadResult.IsSuccess && MaxVersions > 1)
            {
                return await UploadNewVersionAsync(fileModel, uploadResult.Result.Id, folderId);
            }
            else
            {
                // Delete existing file and upload a new one
                // Versions arent maintained / dont have premium, replace existing version

                // Delete blocking file and retry operation
                // Passing only boxId as no version will remove all copies
                _ = await DeleteFileAsync(boxRef.BoxId.ToString());
                return await SaveFileAsync(fileModel);
            }
        }

        public async Task<InternalFileModel> GetFileAsync(string filePath)
        {
            var boxRef = new BoxRef(filePath);

            //TODO: Need to follow this
            // https://developer.box.com/reference/get-files-id-content/
            // handle 202 / 302 and retry-after

            string url = $"https://api.box.com/2.0/files/{boxRef.BoxId}/content{(boxRef.FileVersionId >= 0 ? $"?version={boxRef.FileVersionId}" : string.Empty)}";
            var result = await GetAsync<InternalFileModel>(url);

            return result;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            var boxRef = new BoxRef(filePath);
            string url = $"https://api.box.com/2.0/files/{boxRef.BoxId}{(boxRef.FileVersionId >= 0 ? $"/versions/{boxRef.FileVersionId}" : string.Empty)}";


            bool isSuccess = (await DeleteAsync(url)).IsSuccessStatusCode;
            return isSuccess;
        }
        #endregion

        #region Internal Functions
        private async Task<List<BoxEntry>> GetSubItemsListAsync(long folderId, int offSet = 0, int pageSize = 1000, BoxItemType itemType = BoxItemType.All)
        {
            List<BoxEntry> items = new();

            // 1 - 1000
            int itemsLimit = pageSize > 1000
                    ? 1000
                    : pageSize < 1
                        ? 1
                        : pageSize;

            string qs = $"limit={itemsLimit}&offset={offSet}";
            string requestUrl = $"{ApiUrl}/folders/{folderId}/items?{qs}";

            var response = await GetAsync<BoxPagedCollectionResult>(requestUrl);

            // Add this batch and do we need to go to next page?
            items.AddRange(response.Entries.Where(x => itemType == BoxItemType.All || x.Type.Equals(itemType.ToString(), StringComparison.InvariantCultureIgnoreCase)));
            if (response.TotalCount > itemsLimit && response.TotalCount > response.Limit + response.Offset)
            {
                int nextOffset = response.Limit + response.Offset;
                items.AddRange(await GetSubItemsListAsync(folderId, nextOffset, itemsLimit, itemType));
            }

            return items;
        }

        private RSA CreateRSAProvider(RSAParameters rp)
        {
            var rsaCsp = RSA.Create();
            rsaCsp.ImportParameters(rp);
            return rsaCsp;
        }
        private RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
        {
            RSAParameters rp = new RSAParameters();
            rp.Modulus = privKey.Modulus.ToByteArrayUnsigned();
            rp.Exponent = privKey.PublicExponent.ToByteArrayUnsigned();
            rp.P = privKey.P.ToByteArrayUnsigned();
            rp.Q = privKey.Q.ToByteArrayUnsigned();
            rp.D = ConvertRSAParametersField(privKey.Exponent, rp.Modulus.Length);
            rp.DP = ConvertRSAParametersField(privKey.DP, rp.P.Length);
            rp.DQ = ConvertRSAParametersField(privKey.DQ, rp.Q.Length);
            rp.InverseQ = ConvertRSAParametersField(privKey.QInv, rp.Q.Length);
            return rp;
        }

        private byte[] ConvertRSAParametersField(Org.BouncyCastle.Math.BigInteger n, int size)
        {
            byte[] bs = n.ToByteArrayUnsigned();
            if (bs.Length == size)
                return bs;
            if (bs.Length > size)
                throw new ArgumentException("Specified size too small", "size");
            byte[] padded = new byte[size];
            Array.Copy(bs, 0, padded, size - bs.Length, bs.Length);
            return padded;
        }

        /// <summary>
        /// Will try and convert number to long, if its not a long or empty string, you'll get failedLong
        /// </summary>
        /// <param name="folderId"></param>
        /// <returns></returns>
        private long AsLong(string folderId, long failedLong = -1)
        {
            if (string.IsNullOrEmpty(folderId))
            {
                return failedLong;
            }

            try
            {
                return long.Parse(folderId);
            }
            catch
            {
                return failedLong;
            }
        }

        private async Task<BoxEntry> CreateFolder(string folderName, long parentId)
        {
            string requestUrl = $"{ApiUrl}/folders";
            string boxAttribute = BoxAttribute(folderName, parentId);

            var result = await PostContentAsync(requestUrl, boxAttribute, null);
            if (result.IsSuccess)
            {
                return result.Result;
            }

            throw new Exception(result.Message);
        }

        private async Task<long> ResolveFolderPath(string folderPath, long parentId = -1)
        {
            long parentBoxId = parentId >= 0 ? parentId : RootFolderId;
            string[] folders = folderPath.Split(new string[] { ClientDirectoryChar.ToString() }, options: StringSplitOptions.RemoveEmptyEntries);

            return folders.Length > 1
                // Work way through folder tree to ensure you get all subFolder Ids
                ? await ResolveFolderPath(
                    folderPath: string.Join(ClientDirectoryChar.ToString(), folders.Skip(1)),
                    parentId: await CreateOrResolveFolderId(folders[0], parentBoxId))

                // At last subFolder
                : await CreateOrResolveFolderId(
                    folderName: folders[0],
                    parentId: parentBoxId);
        }

        /// <summary>
        /// Finds the folderByName at the passed in parentId or rootId = 0, if not found, will autoCreate if BoxConfig.AutoCreateRoot == true or give -1 if autoCreate = false
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private async Task<long> CreateOrResolveFolderId(string folderName, long parentId = -1, bool? autoCreate = null)
        {
            long targetBoxId = -1;
            long parentBoxId = parentId >= 0 ? parentId : RootFolderId;

            if (string.IsNullOrEmpty(folderName))
            {
                return parentBoxId;
            }

            else
            {
                // Get List of FolderNames from Box
                var foldersList = await GetSubItemsListAsync(folderId: parentBoxId, itemType: BoxItemType.Folder);

                // Find Folder matching name
                BoxEntry subFolder = foldersList.FirstOrDefault(x => x.Name.Equals(folderName, StringComparison.InvariantCultureIgnoreCase));
                if (subFolder == null || subFolder == default)
                {
                    // Ask box to create the folder
                    if (autoCreate ?? BoxConfig.AutoCreateRoot)
                    {
                        subFolder = await CreateFolder(folderName, parentBoxId);
                    }
                }

                targetBoxId = AsLong(subFolder?.Id ?? "");
            }

            return targetBoxId;
        }

        private async Task<string> UploadNewVersionAsync(InternalFileModel fileModel, string existingFileId, long folderId)
        {
            string endpoint = $"{UploadUrl}/{existingFileId}/content";
            string boxAttribute = BoxAttribute(fileModel.OrginalNameWithExt, folderId);

            var result = await PostContentAsync(endpoint, boxAttribute, fileModel);

            if (result.IsSuccess)
            {
                return $"{result.Result?.Id}/{result.Result?.FileVersion?.Id}";
            }

            throw new Exception(result.Message);
        }

        private string BoxAttribute(string fileName, long folderId)
        {
            var boxAttribute = new
            {
                name = fileName,
                parent = new
                {
                    id = folderId.ToString()
                }
            };

            return JsonConvert.SerializeObject(boxAttribute);
        }

        private async Task<BoxEntry[]> GetFileInfo(string fileId, bool includePreviousVersions = false)
        {
            string getFileInfoEndpoint = $"{ApiUrl}/files/{fileId}";
            var fileInfoResult = await GetAsync<BoxResult>(getFileInfoEndpoint);

            List<BoxEntry> fileCollectionResult = new();

            if (fileInfoResult != null)
            {
                int maxSeq = Convert.ToInt32(fileInfoResult.SequenceId);

                fileCollectionResult.Add(new BoxEntry
                {
                    Id = fileId,
                    SequenceId = maxSeq,
                    FileVersion = fileInfoResult.FileVersion,
                    CreatedDt = fileInfoResult.CreatedDt,
                    Name = fileInfoResult.Name,
                    Sha1 = fileInfoResult.Sha1,
                    Size = fileInfoResult.Size,
                    Description = fileInfoResult.Description,
                    Etag = fileInfoResult.Etag,
                    ModifiedDt = fileInfoResult.ModifiedDt,
                    Type = fileInfoResult.Type,
                });

                if (includePreviousVersions)
                {
                    // Get History of file
                    // Doesnt include current version
                    // Feature only available in Premium
                    string getFilePrevVersionsUrl = $"{ApiUrl}/files/{fileId}/versions";
                    var prevVersionsResult = await GetAsync<BoxCollectionResult>(getFilePrevVersionsUrl);

                    if (prevVersionsResult?.Entries?.Length > 0)
                    {
                        // Should be this way but lets ensure it
                        List<BoxEntry> prevVersions = prevVersionsResult.Entries.OrderByDescending(x => x.Id).ToList();

                        foreach (var pvItem in prevVersions)
                        {
                            maxSeq--;
                            fileCollectionResult.Add(new BoxEntry
                            {
                                Id = fileId,
                                SequenceId = maxSeq,
                                FileVersion = new BoxEntryVersion { Id = pvItem.Id, Sha1 = pvItem.Sha1, Type = pvItem.Type },
                                CreatedDt = pvItem.CreatedDt,
                                Name = pvItem.Name,
                                Sha1 = pvItem.Sha1,
                                Size = pvItem.Size,
                                Description = pvItem.Description,
                                Etag = pvItem.Etag,
                                ModifiedDt = pvItem.ModifiedDt,
                                Type = pvItem.Type,
                            });
                        }

                    }
                }
            }

            // Return file Collection result
            return fileCollectionResult.ToArray();
        }

        private async Task<BoxApiResult> UploadPreflightAsync(string boxAttribute)
        {
            _ = await PreflightChecks();
            // Buidl the ContentPart
            HttpContent httpContent = new StringContent(boxAttribute, Encoding.UTF8, "application/json");
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Options, "https://api.box.com/2.0/files/content");
            httpRequestMessage.Content = httpContent;

            HttpResponseMessage httpResponse = await BoxHttpClient.SendAsync(httpRequestMessage);

            // Read the response body
            string data = await httpResponse.Content.ReadAsStringAsync();
            BoxEntry entry = JsonConvert.DeserializeObject<BoxConflictResult>(data)?.ContextInfo?.conflicts;
            return new BoxApiResult
            {
                IsSuccess = httpResponse.IsSuccessStatusCode,
                StatusCode = (int)httpResponse.StatusCode,
                Message = data,
                Result = entry,
            };
        }

        private async Task<T> GetAsync<T>(string url)
        {
            if (await PreflightChecks())
            {
                HttpResponseMessage httpResponse = await BoxHttpClient.GetAsync(url);

                if (httpResponse?.IsSuccessStatusCode == true)
                {
                    if (typeof(T) == typeof(InternalFileModel) && httpResponse.Content.Headers?.ContentDisposition?.DispositionType == "attachment")
                    {
                        FileInfo fileInfo = new FileInfo(httpResponse.Content.Headers.ContentDisposition.FileName);

                        byte[] data = await httpResponse.Content.ReadAsByteArrayAsync();
                        return (T)Convert.ChangeType(new InternalFileModel
                        {
                            Data = data,
                            Name = fileInfo.Name,
                            Extension = fileInfo.Extension,
                            MimeType = httpResponse.Content.Headers.ContentType.MediaType,
                        }, typeof(T));
                    }

                    // We'll try and cast it to a Type
                    string bodyMessage = await httpResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(bodyMessage))
                    {
                        return JsonConvert.DeserializeObject<T>(bodyMessage);
                    }
                }
            }

            return default;
        }

        private async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            if (await PreflightChecks())
            {
                var response = await BoxHttpClient.DeleteAsync(url);
                string body = await response.Content?.ReadAsStringAsync();
                return response;
            }

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = $"Preflight check failure, possible badConfig or Credentials",
            };
        }

        private async Task<BoxApiResult> PostContentAsync(string url, string boxAttribute, InternalFileModel fileModel)
        {
            if (await PreflightChecks())
            {
                // Buidl the ContentPart
                HttpContent httpContent = new StringContent(boxAttribute, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse;

                // Build the message
                if (fileModel != null)
                {
                    // Sending FormData Content
                    using (var formData = new MultipartFormDataContent())
                    {
                        // Add the box attributes
                        formData.Add(httpContent, "attributes");

                        // Add the fileContent if any
                        if (fileModel?.Data?.Length > 0 && !string.IsNullOrEmpty(fileModel.OrginalNameNoExt))
                        {
                            var fileContent = new ByteArrayContent(fileModel.Data);
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = fileModel.OrginalNameNoExt,
                                FileName = fileModel.OrginalNameWithExt
                            };
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

                            formData.Add(fileContent, "file", fileModel.Extension);
                        }

                        // Send
                        httpResponse = await BoxHttpClient.PostAsync(url, formData);
                    }
                }
                else
                {
                    // Send
                    httpResponse = await BoxHttpClient.PostAsync(url, httpContent);
                }

                // Read the response body
                string data = await httpResponse.Content.ReadAsStringAsync();

                return new BoxApiResult
                {
                    IsSuccess = httpResponse.IsSuccessStatusCode,
                    StatusCode = (int)httpResponse.StatusCode,
                    Message = data,
                    Result = httpResponse.IsSuccessStatusCode
                    ? ParseBoxEntry(data)
                    : httpResponse.StatusCode == System.Net.HttpStatusCode.Conflict
                        ? JsonConvert.DeserializeObject<BoxConflictResult>(data)?.ContextInfo.conflicts
                        : null
                };
            }

            return default;
        }

        private async Task<bool> PreflightChecks()
        {
            // AccessToken empty or TimeExpired?
            if (string.IsNullOrEmpty(access_token) || DateTime.UtcNow >= _expiration)
            {
                return await RefreshToken();
            }

            return true;
        }

        private async Task<bool> RefreshToken()
        {
            // We create a random identifier that helps protect against
            // replay attacks
            byte[] randomNumber = new byte[64];
            RandomNumberGenerator.Create().GetBytes(randomNumber);
            var jti = Convert.ToBase64String(randomNumber);

            // We give the assertion a lifetime of 45 seconds 
            // before it expires
            DateTime expirationTime = DateTime.UtcNow.AddSeconds(45);

            // Next, we are read to assemble the payload
            var claims = new List<Claim>{
                new Claim("sub", BoxConfig.EnterpriseID),
                new Claim("box_sub_type", "enterprise"),
                new Claim("jti", jti),
            };

            string authenticationUrl = "https://api.box.com/oauth2/token";

            // Rather than constructing the JWT assertion manually, we are 
            // using the System.IdentityModel.Tokens.Jwt library.
            var payload = new JwtPayload(
                BoxConfig.BoxAppSettings.ClientID,
                authenticationUrl,
                claims,
                null,
                expirationTime
            );

            // The API support "RS256", "RS384", and "RS512" encryption
            var credentials = new SigningCredentials(
                new RsaSecurityKey(RsaKey),
                SecurityAlgorithms.RsaSha512
            );
            var header = new JwtHeader(signingCredentials: credentials);

            // Finally, let's create the assertion usign the 
            // header and payload
            var jst = new JwtSecurityToken(header, payload);
            var tokenHandler = new JwtSecurityTokenHandler();
            string assertion = tokenHandler.WriteToken(jst);

            // We start by preparing the params to send to 
            // the authentication endpoint
            var content = new FormUrlEncodedContent(new[]
            {
                // This specifies that we are using a JWT assertion
                // to authenticate
                new KeyValuePair<string, string>(
                    "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                // Our JWT assertion
                new KeyValuePair<string, string>(
                    "assertion", assertion),
                // The OAuth 2 client ID and secret
                new KeyValuePair<string, string>(
                    "client_id", BoxConfig.BoxAppSettings.ClientID),
                new KeyValuePair<string, string>(
                    "client_secret", BoxConfig.BoxAppSettings.ClientSecret)
            });


            var response = await BoxHttpClient.PostAsync(authenticationUrl, content);
            var data = await response.Content.ReadAsStringAsync();

            BoxToken token = JsonConvert.DeserializeObject<BoxToken>(data);

            if (token != null && !string.IsNullOrEmpty(token.access_token))
            {
                // Set Internal Token
                access_token = token.access_token;
                _expiration = DateTime.Now.Add(new TimeSpan(0, 0, token.expires_in));

                // Staple the default Headers
                _ = BoxHttpClient.DefaultRequestHeaders.Remove("Authorization");
                BoxHttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                // Signal Ready
                HasInit = true;
                return true;
            }

            HasInit = false;
            return false;
        }

        private BoxEntry ParseBoxEntry(string jsonData)
        {
            BoxEntry boxEntry = null;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    boxEntry = JsonConvert.DeserializeObject<BoxCollectionResult>(jsonData)?.Entries.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    boxEntry = JsonConvert.DeserializeObject<BoxEntry>(jsonData);
                }
            }

            return boxEntry;
        }
        #endregion
    }
}