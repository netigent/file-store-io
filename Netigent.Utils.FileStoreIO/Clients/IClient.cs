using Netigent.Utils.FileStoreIO.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public interface IClient
    {
        public bool IsReady { get; }

        public Task<string> SaveFileAsync(InternalFileModel fileModel);

        public Task<InternalFileModel> GetFileAsync(string filePath);

        public Task<bool> DeleteFileAsync(string filePath);

        public Task IndexContentsAsync(ObservableCollection<InternalFileModel> indexList);
    }
}
