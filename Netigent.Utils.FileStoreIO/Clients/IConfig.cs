using Netigent.Utils.FileStoreIO.Enums;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public interface IConfig
    {
        FileStorageProvider StoreType { get; }
    }
}
