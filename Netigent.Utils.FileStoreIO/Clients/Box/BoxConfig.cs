using Netigent.Utils.FileStoreIO.Clients.Box.Models;
using Netigent.Utils.FileStoreIO.Enums;

namespace Netigent.Utils.FileStoreIO.Clients.Box
{
    public class BoxConfig : IConfig
    {
        public FileStorageProvider StoreType => FileStorageProvider.Box;

        public BoxAppSettings BoxAppSettings { get; set; }

        public string EnterpriseID { get; set; }

        public bool AutoCreateRoot { get; set; } = false;

        public int TimeoutInMins { get; set; } = 15;

        public BoxConfig() { }
    }
}
