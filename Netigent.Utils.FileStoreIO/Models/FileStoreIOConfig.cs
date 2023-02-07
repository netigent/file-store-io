using Netigent.Utils.FileStoreIO.Enum;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class FileStoreIOConfig
    {
        public static string Section { get; } = "FileStoreIO";
        public string Database { get; set; }
        public string FileStoreRoot { get; set; }
        public string FilePrefix { get; set; } = "_$";
        public string DatabaseSchema { get; set; } = "fileStore";
        public bool StoreFileAsUniqueRef { get; set; } = true;
        public int MaxVersions { get; set; } = 1;

        public BoxConfig? Box { get; set; }
    }

    public class BoxConfig
    {
        public BoxAppSettings BoxAppSettings { get; set; }
        public string EnterpriseID { get; set; }
    }

    public class BoxAppSettings
    {
        public string ClientSecret { get; set; }
        public string ClientID { get; set; }
        public AppAuth AppAuth { get; set; }
    }

    public class AppAuth
    {
        public string PublicKeyID { get; set; }
        public string PrivateKey { get; set; }
        public string Passphrase { get; set; }
    }



}
