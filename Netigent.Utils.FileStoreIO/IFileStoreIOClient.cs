using Microsoft.AspNetCore.Http;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
	public interface IFileStoreIOClient
	{
		bool IsReady { get; }
		List<string> Messages { get; }

		Task<string> File_Upsert(IFormFile file, FileStorageProvider fileLocation, string description, string customer = "", string filegroup = "");
		string File_Upsert(InternalFileModel model, FileStorageProvider fileLocation, string customer = "", string filegroup = "");
		Task<FileObjectModel> File_Get(string fileRef);
		Task<InternalFileModel> File_Delete(string fileRef);
		Task<List<InternalFileModel>> Files_GetAll();
	}
}
