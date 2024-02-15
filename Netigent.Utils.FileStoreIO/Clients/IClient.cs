using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public interface IClient
    {
        /// <summary>
        /// Initialize the StoreProvider.
        /// </summary>
        /// <param name="config">If the IConfig doesnt match the provider type it will not start.</param>
        /// <param name="maxVersions">For use when no versioning possible i.e. FileSystem, will store multiple copies of the file with a vers no in the file</param>
        /// <param name="appShortCode">(Optional) Ensures the rootFolder is scoped to your app code...</param>
        /// <returns></returns>
        public ResultModel Init(IConfig config, int maxVersions = 1, string appShortCode = "");

        public FileStorageProvider ProviderType { get; }

        public bool HasInit { get; set; }

        /// <summary>
        /// Saves file to specified storeProvider, returing storeProvider, ID / Path / Key i.e. where is it in X?
        /// </summary>
        /// <param name="fileModel"></param>
        /// <returns></returns>
        public Task<string> SaveFileAsync(InternalFileModel fileModel);

        public Task<InternalFileModel> GetFileAsync(string extClientRef);

        public Task<bool> DeleteFileAsync(string extClientRef);

        public Task<long> IndexContentsAsync(ObservableCollection<InternalFileModel> indexList, string indexPathTags, bool scopeToAppFolder);
    }
}
