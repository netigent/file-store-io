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
		Task<string> File_Upsert(IFormFile file, FileStorageProvider fileLocation, string description, string mainGroup = "", string subGroup = "");
		string File_Upsert(InternalFileModel model, FileStorageProvider fileLocation, string mainGroup = "", string subGroup = "");
		Task<FileObjectModel> File_Get(string fileRef);
		Task<InternalFileModel> File_Delete(string fileRef);
		Task<List<InternalFileModel>> Files_GetAll();
		Task<List<InternalFileModel>> Files_GetByMainGroup(string mainGroup);
		Task<List<InternalFileModel>> Files_GetBySubGroup(string subGroup);
		Task<List<InternalFileModel>> Files_GetByMainAndSubGroup(string mainGroup, string subGroup);
	}
}
