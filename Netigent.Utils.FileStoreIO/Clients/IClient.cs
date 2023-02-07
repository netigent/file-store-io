using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO.Clients
{
    public interface IClient
    {
        public T GetContents<T>(string path);
        public Task<long> UploadAsync(long location, InternalFileModel fileModel);
        public Task<InternalFileModel> DownloadAsync(long fileId);
    }
}
