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
using Org.BouncyCastle.Asn1.Cms;
using System.Xml.Linq;
using System.Text;
using System.Linq;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public class BoxClient : IClient
    {
        private const string ApiUrl = "https://api.box.com/2.0";
        private const string UploadUrl = "https://upload.box.com/api/2.0/files";
        private string access_token { get; set; }
        private HttpClient _client { get; }

        public BoxClient(BoxConfig config)
        {

            // Next, we use BouncyCastle's PemReader to read the 
            // decrypt the private key into a RsaPrivateCrtKeyParameters
            // object
            var appAuth = config.BoxAppSettings.AppAuth;
            var stringReader = new StringReader(appAuth.PrivateKey);
            var passwordFinder = new PasswordFinder(appAuth.Passphrase);
            var pemReader = new PemReader(stringReader, passwordFinder);
            var keyParams = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();

            // In the end, we will use this key in the next steps
            var key = CreateRSAProvider(ToRSAParameters(keyParams));

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
                new Claim("sub", config.EnterpriseID),
                new Claim("box_sub_type", "enterprise"),
                new Claim("jti", jti),
            };

            String authenticationUrl = "https://api.box.com/oauth2/token";

            // Rather than constructing the JWT assertion manually, we are 
            // using the System.IdentityModel.Tokens.Jwt library.
            var payload = new JwtPayload(
                config.BoxAppSettings.ClientID,
                authenticationUrl,
                claims,
                null,
                expirationTime
            );

            // The API support "RS256", "RS384", and "RS512" encryption
            var credentials = new SigningCredentials(
                new RsaSecurityKey(key),
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
                    "client_id", config.BoxAppSettings.ClientID),
                new KeyValuePair<string, string>(
                    "client_secret", config.BoxAppSettings.ClientSecret)
            });

            // Make the POST call to the authentication endpoint
            _client = new HttpClient();
            var response = _client.PostAsync(authenticationUrl, content).Result;

            var data = response.Content.ReadAsStringAsync().Result;
            Token? token = JsonConvert.DeserializeObject<Token>(data);
            if (token != null)
            {
                access_token = token.access_token;
                _client.DefaultRequestHeaders.Add(
                "Authorization", "Bearer " + access_token

            );

                return;
            }

            throw new Exception("Couldnt establish client");
        }

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

        public T GetContents<T>(string path = "0")
        {
            // https://developer.box.com/guides/authentication/jwt/without-sdk/
            HttpResponseMessage response = _client.GetAsync($"{ApiUrl}/folders/{path}").Result;
            string data = response.Content.ReadAsStringAsync().Result;

            try
            {
                var returnOutput = JsonConvert.DeserializeObject<T>(data);
                return returnOutput;
            }
            catch
            {
            }

            return default;
        }

        public async Task<long> UploadAsync(long location, InternalFileModel fileModel)
        {
            string endpoint = $"{UploadUrl}/content";

            var fileContent = new ByteArrayContent(fileModel.Data);
            //fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            //{
            //    Name = fileModel.Name,
            //    FileName = fileModel.Name
            //};
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            using (var formData = new MultipartFormDataContent())
            {
                var att = new
                {
                    name = fileModel.Name,
                    parent = new
                    {
                        id = location
                    }
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(att), Encoding.UTF8, "application/json");
                formData.Add(jsonContent, "attributes");
                formData.Add(fileContent, "file", fileModel.Name);


                var response = await _client.PostAsync(UploadUrl, formData);
                string data = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    PathCollectionResult? result = JsonConvert.DeserializeObject<PathCollectionResult>(data);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    ConflictResult? conflict = JsonConvert.DeserializeObject<ConflictResult>(data);
                }
            }
            return 0;
        }

        private Task<long> UploadVersionAsync(string location, string existingfileId, InternalFileModel fileModel)
        {
            string endpoint = $"{UploadUrl}/{existingfileId}/content";

        }

        private string BoxAttribute(string fileName, string folderId)
        {
            var boxAttribute = new
            {
                name = fileName,
                parent = new
                {
                    id = folderId
                }
            };

            return JsonConvert.SerializeObject(boxAttribute);
        }

        private async Task<ApiResult> UploadContentAsync(string url, string boxAttribute, string? fileName, byte[]? content)
        {
            try
            {
                // Build the message
                using (var formData = new MultipartFormDataContent())
                {
                    // Add the box attributes
                    formData.Add(new StringContent(boxAttribute, Encoding.UTF8, "application/json"), "attributes");

                    // Add the fileContent if any
                    if (content != null && !string.IsNullOrEmpty(fileName) && content?.LongLength > 0)
                    {
                        formData.Add(new ByteArrayContent(content), "file", fileName);
                    }

                    // Set content Type
                    formData.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

                    // Send
                    var response = await _client.PostAsync(url, formData);
                    string data = await response.Content.ReadAsStringAsync();

                    return new ApiResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Message = data,
                        Result = (JsonConvert.DeserializeObject<PathCollectionResult>(data))?.Entries.FirstOrDefault(),
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = ex.Message
                };
            }
        }



        public Task<InternalFileModel> DownloadAsync(long fileId)
        {
            throw new NotImplementedException();
        }
    }

    class ApiResult
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }

        public string Message { get; set; }

        public BoxEntry? Result { get; set; }
    }


    class Token
    {
        public string access_token { get; set; }
    }

    public class PasswordFinder : IPasswordFinder
    {
        private string password;
        public PasswordFinder(string _password) { password = _password; }
        public char[] GetPassword() { return password.ToCharArray(); }
    }
}
