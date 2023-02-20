using Netigent.Utils.FileStoreIO.Models;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using System.Security.Cryptography;
using System;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using Netigent.Utils.FileStoreIO.Enum;
using System.Net;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public class BoxClient : IClient
    {
        #region Internal Props
        private const string ApiUrl = "https://api.box.com/2.0";
        private const string UploadUrl = "https://upload.box.com/api/2.0/files";
        private readonly BoxConfig _config;
        private readonly RSA _rsa;
        private string access_token { get; set; }
        private HttpClient _boxClient { get; }
        private DateTime _expiration { get; set; }
        private long _rootFolder { get; }
        private int _maxVersions { get; }
        #endregion

        #region Public Props
        public bool IsReady { get; set; } = false;
        #endregion

        #region ctor
        public BoxClient(BoxConfig config, int maxVersions = 1)
        {
            // Set the RootFolder we're targeting, if less than 0 or not set etc, the root is box deault root
            _rootFolder = config.RootFolder >= 0
                ? config.RootFolder
                : 0;

            _maxVersions = maxVersions >= 1
                ? maxVersions
                : 1;

            // Next, we use BouncyCastle's PemReader to read the 
            // decrypt the private key into a RsaPrivateCrtKeyParameters
            // object
            var appAuth = config.BoxAppSettings.AppAuth;
            var stringReader = new StringReader(appAuth.PrivateKey);
            var passwordFinder = new PasswordFinder(appAuth.Passphrase);
            var pemReader = new PemReader(stringReader, passwordFinder);
            var keyParams = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();

            // In the end, we will use this key in the next steps
            _rsa = CreateRSAProvider(ToRSAParameters(keyParams));
            this._config = config;

            // Make the POST call to the authentication endpoint
            _boxClient = new HttpClient();
            _boxClient.Timeout = new TimeSpan(0, _config.TimeoutInMins, 0);

            // _ = RefreshToken().Result;
            //return;
        }
        #endregion

        public RSA CreateRSAProvider(RSAParameters rp)
        {
            var rsaCsp = RSA.Create();
            rsaCsp.ImportParameters(rp);
            return rsaCsp;
        }

        public RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
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

        public byte[] ConvertRSAParametersField(Org.BouncyCastle.Math.BigInteger n, int size)
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

        private long AsLong(string folderId)
        {
            try
            {
                return long.Parse(folderId);
            }
            catch
            {
                return -1;
            }
        }

        public async Task<BoxEntry> GetFolderAsync(long folderId)
        {
            string requestUrl = $"{ApiUrl}/folders/{folderId}";
            var response = await GetAsync<BoxEntry>(requestUrl);

            return response;
        }

        public async Task<List<BoxEntry>> GetSubItemsListAsync(long folderId, int offSet = 0, int pageSize = 1000, BoxItemType itemType = BoxItemType.All)
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
            if (response.TotalCount > itemsLimit && response.TotalCount > (response.Limit + response.Offset))
            {
                int nextOffset = (response.Limit + response.Offset);
                items.AddRange(await GetSubItemsListAsync(folderId, nextOffset, itemsLimit, itemType));
            }

            return items;
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

        private async Task<long> ResolveId(string folderName, long parentId = -1, bool autoCreate = true)
        {
            long targetBoxId = -1;
            long parentBoxId = parentId >= 0 ? parentId : _rootFolder;

            if (string.IsNullOrEmpty(folderName))
            {
                return parentBoxId;
            }
            else
            {
                // Get List of FolderNames from Box
                var foldersList = await GetSubItemsListAsync(folderId: parentBoxId, itemType: BoxItemType.Folder);

                // Find Folder matching name
                BoxEntry? subFolder = foldersList.FirstOrDefault(x => x.Name.Equals(folderName, StringComparison.InvariantCultureIgnoreCase));
                if (subFolder == null || subFolder == default)
                {
                    // Ask box to create the folder
                    if (autoCreate)
                    {
                        subFolder = await CreateFolder(folderName, parentBoxId);
                        targetBoxId = -2;
                    }
                }

                targetBoxId = AsLong(subFolder.Id);
            }

            return targetBoxId;
        }

        public async Task<string> SaveFileAsync(InternalFileModel fileModel)
        {
            if (fileModel.Data?.Length == null)
            {
                // Nothing to upload
                return string.Empty;
            }

            long folderId = await ResolveId(fileModel.SubGroup, await ResolveId(fileModel.MainGroup, _rootFolder));
            string endpoint = $"{UploadUrl}/content";
            string boxAttribute = BoxAttribute(fileModel.RawName + fileModel.Extension, folderId);

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


            if (!uploadResult.IsSuccess && _maxVersions > 1)
            {
                return await UploadNewVersionAsync(fileModel, uploadResult.Result.Id, folderId);
                //var versionHistory = await GetFileInfo(uploadResult.Result.Id, true);

                //if (versionHistory.Count() > _maxVersions)
                //{
                //    var versions2Remove = versionHistory.OrderBy( x=> x.SequenceId).Take(versionHistory.Count() - _maxVersions).ToList();
                //    foreach(var version in versions2Remove)
                //    {
                //        var removeVersion = new BoxRef(version);
                //        _ = await DeleteFileAsync(removeVersion.AsFilePath);
                //    }

                //}

                //return uploadedVersionResult;
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



        private async Task<string> UploadNewVersionAsync(InternalFileModel fileModel, string existingFileId, long folderId)
        {
            string endpoint = $"{UploadUrl}/{existingFileId}/content";
            string boxAttribute = BoxAttribute(fileModel.RawName + fileModel.Extension, folderId);

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

        public async Task<BoxEntry[]> GetFileInfo(string fileId, bool includePreviousVersions = false)
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

            HttpResponseMessage httpResponse = await _boxClient.SendAsync(httpRequestMessage);

            // Read the response body
            string data = await httpResponse.Content.ReadAsStringAsync();
            BoxEntry? entry = (JsonConvert.DeserializeObject<BoxConflictResult>(data))?.ContextInfo?.conflicts;
            return new BoxApiResult
            {
                IsSuccess = httpResponse.IsSuccessStatusCode,
                StatusCode = (int)httpResponse.StatusCode,
                Message = data,
                Result = entry,
            };
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


            return (await DeleteAsync(url)).IsSuccessStatusCode;
        }

        #region Internal Functions

        private async Task<T> GetAsync<T>(string url)
        {
            try
            {
                if (await PreflightChecks())
                {
                    HttpResponseMessage httpResponse = await _boxClient.GetAsync(url);

                    if (httpResponse.IsSuccessStatusCode)
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
            }
            catch (Exception ex)
            {
                throw;
            }

            return default;
        }

        private async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            try
            {
                if (await PreflightChecks())
                {
                    return await _boxClient.DeleteAsync(url);
                }
                else
                {
                    throw new Exception("Preflight failure");
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        private async Task<BoxApiResult> PostContentAsync(string url, string boxAttribute, InternalFileModel fileModel)
        {
            try
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
                            if (fileModel?.Data?.Length > 0 && !string.IsNullOrEmpty(fileModel.RawName))
                            {
                                var fileContent = new ByteArrayContent(fileModel.Data);
                                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = fileModel.RawName,
                                    FileName = fileModel.RawName + fileModel.Extension
                                };
                                fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

                                formData.Add(fileContent, "file", fileModel.Extension);
                            }

                            // Send
                            httpResponse = await _boxClient.PostAsync(url, formData);
                        }
                    }
                    else
                    {
                        // Send
                        httpResponse = await _boxClient.PostAsync(url, httpContent);
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
                            ? (JsonConvert.DeserializeObject<BoxConflictResult>(data))?.ContextInfo.conflicts
                            : null
                    };
                }
            }
            catch (Exception ex)
            {
                return new BoxApiResult
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = ex.Message
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
                new Claim("sub", _config.EnterpriseID),
                new Claim("box_sub_type", "enterprise"),
                new Claim("jti", jti),
            };

            String authenticationUrl = "https://api.box.com/oauth2/token";

            // Rather than constructing the JWT assertion manually, we are 
            // using the System.IdentityModel.Tokens.Jwt library.
            var payload = new JwtPayload(
                _config.BoxAppSettings.ClientID,
                authenticationUrl,
                claims,
                null,
                expirationTime
            );

            // The API support "RS256", "RS384", and "RS512" encryption
            var credentials = new SigningCredentials(
                new RsaSecurityKey(_rsa),
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
                    "client_id", _config.BoxAppSettings.ClientID),
                new KeyValuePair<string, string>(
                    "client_secret", _config.BoxAppSettings.ClientSecret)
            });


            var response = await _boxClient.PostAsync(authenticationUrl, content);
            var data = await response.Content.ReadAsStringAsync();

            BoxToken? token = JsonConvert.DeserializeObject<BoxToken>(data);

            if (token != null && !string.IsNullOrEmpty(token.access_token))
            {
                // Set Internal Token
                access_token = token.access_token;
                _expiration = DateTime.Now.Add(new TimeSpan(0, 0, token.expires_in));
                
                // Staple the default Headers
                _ = _boxClient.DefaultRequestHeaders.Remove("Authorization");
                _boxClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                // Signal Ready
                this.IsReady = true;
                return true;
            }

            throw new Exception("Couldnt establish client");
        }

        private BoxEntry? ParseBoxEntry(string jsonData)
        {
            BoxEntry? boxEntry = null;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    boxEntry = (JsonConvert.DeserializeObject<BoxCollectionResult>(jsonData))?.Entries.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    boxEntry = (JsonConvert.DeserializeObject<BoxEntry>(jsonData));
                }
            }

            return boxEntry;
        }
        #endregion
    }
}
