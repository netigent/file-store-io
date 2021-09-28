using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Netigent.Utils.FileStoreIO.Dal;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Netigent.Utils.FileStoreIO
{
	public class FileStoreIOClient : IFileStoreIOClient
	{
		public bool IsReady { get; internal set; } = false;
		public bool FileSystemStorageAvailable { get; internal set; } = false;
		public List<string> Messages { get; internal set; } = new();

		private readonly IOptions<FileIOConfig> _config;
		private readonly string _fileStoreRoot;
		private readonly InternalDatabaseClient _ioClient;
		private readonly string _dynamicFileFlag;

		private const string _notSpecifiedFlag = "_$";
		private const string _notSpecifiedSchema = "dbo";

		/// <summary>
		/// FileIOClient using FileIOConfig
		/// </summary>
		/// <param name="fileIOConfig"></param>
		public FileStoreIOClient(IOptions<FileIOConfig> fileIOConfig)
		{
			_config = fileIOConfig;

			_ioClient = new InternalDatabaseClient(_config.Value.Database, _config.Value.DatabaseSchema);
			_fileStoreRoot = _config.Value.FileStoreRoot;
			_dynamicFileFlag = _config.Value.FileFlag ?? _notSpecifiedFlag;

			Startup(out string fileSystemErrorMessage);

			if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
				IsReady = true;

			else
			{
				if (!string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
					Messages.Add(_ioClient.DbClientErrorMessage);

				if (!string.IsNullOrEmpty(fileSystemErrorMessage))
					Messages.Add(fileSystemErrorMessage);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="databaseConnection">Database to use for fileStoreIndex table, account in connection string should have the ability to create tables in that database</param>
		/// <param name="fileStoreRoot">Location you wish to store files uploaded to the filesystem e.g. c:\temp\filestore \\myfiles\fileuploads</param>
		/// <param name="dynamicFileFlag">Will mark the files with a prefix that is used in the file system</param>
		/// <param name="dbSchema">Database schema to create or use the FileStoreIndex table, default is dbo</param>
		public FileStoreIOClient(string databaseConnection, string fileStoreRoot, string dynamicFileFlag = _notSpecifiedFlag, string dbSchema = _notSpecifiedSchema)
		{
			//Create the filestore client
			_ioClient = new InternalDatabaseClient(databaseConnection, dbSchema);
			_fileStoreRoot = fileStoreRoot;
			_dynamicFileFlag = dynamicFileFlag;

			Startup(out string fileSystemErrorMessage);

			if (string.IsNullOrEmpty(fileSystemErrorMessage) && string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
				IsReady = true;

			else
			{
				if (!string.IsNullOrEmpty(_ioClient.DbClientErrorMessage))
					Messages.Add(_ioClient.DbClientErrorMessage);

				if (!string.IsNullOrEmpty(fileSystemErrorMessage))
					Messages.Add(fileSystemErrorMessage);
			}
		}

		/// <summary>
		/// Insert / Update a File to the intended file storage
		/// </summary>
		/// <param name="file">File as an IFormFile bject</param>
		/// <param name="storageType">Where to store file, current options FileSystem or Database</param>
		/// <param name="description">(Optional) Description of the file, if omitted, will use the filename</param>
		/// <param name="customer">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{customer}\{fileGroupType}\filename.ext</param>
		/// <param name="fileTypeGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{customer}\{fileGroupType}\filename.ext</param>
		/// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
		public async Task<string> File_Upsert(IFormFile file, FileStorageProvider storageType, string description = "", string customer = "", string fileTypeGroup = "")
		{
			var fileName = Path.GetFileNameWithoutExtension(file.FileName);
			var extension = Path.GetExtension(file.FileName);
			var fileModel = new InternalFileModel
			{
				Created = DateTime.UtcNow,
				FileType = file.ContentType,
				Extension = extension,
				Name = fileName,
				Description = description ?? fileName,
				FileLocation = (int)storageType
			};

			using (var dataStream = new MemoryStream())
			{
				await file.CopyToAsync(dataStream);
				fileModel.Data = dataStream.ToArray();
			}

			return File_Upsert(fileModel, storageType, customer, fileTypeGroup);
		}

		/// <summary>
		/// Insert / Update a File to the intended file storage
		/// </summary>
		/// <param name="fileObject">The file to save, ensure the file is saved as byte[] to Data</param>
		/// <param name="storageType">Where to store file, current options FileSystem or Database</param>
		/// <param name="customer">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{customer}\{fileGroupType}\filename.ext</param>
		/// <param name="fileTypeGroup">(Optional) Used when storing to filesystem will store as \\myserver\filestore\{customer}\{fileGroupType}\filename.ext</param>
		/// <returns>A unique file-reference for getting the file, if blank issues creating the file</returns>
		public string File_Upsert(InternalFileModel fileObject, FileStorageProvider storageType, string customer = "", string fileTypeGroup = "")
		{
			//Assign unique fileRef code
			fileObject.FileRef = FileStore_NewFileRef();

			//Determine where the file needs to go
			switch (storageType)
			{
				case FileStorageProvider.FileSystem:
					//Send byte[] to filesystem
					if (!FileSystemStorageAvailable)
						throw new InvalidOperationException($"Couldnt write to folder '{_fileStoreRoot}', please ensure you specified a folder and that you have permissions to access it");

					try
					{
						var successfullFullFilePath = Write2Filesystem($"{fileObject.Name}{fileObject.Extension}", fileObject.Data, customer, fileTypeGroup);
						
						if (string.IsNullOrEmpty(successfullFullFilePath))
							return string.Empty;

						//Update the model to insert into database tracking table
						fileObject.FilePath = successfullFullFilePath;
						fileObject.Data = null; //Clear so it doesnt write to database also
					}
					catch(Exception ex)
					{
						throw new Exception($"Couldn't create file, {Path.Combine(_fileStoreRoot, customer, fileTypeGroup, fileObject.Name, fileObject.Extension)}. Please ensure you have permissions and no opened files by same name etc, here is the error {ex.Message}");
					}

					break;
				case FileStorageProvider.Database:
					break;
				default:
					break;
			}

			//Add the fileRecord
			long id = _ioClient.FileStore_Upsert(fileObject);
			if (id > 0)
				return _ioClient.FileStore_GetFileRef(id);

			return string.Empty;
		}

		/// <summary>
		/// Gets the file by the unique file-reference
		/// </summary>
		/// <param name="fileRef">Pass the unique file-reference</param>
		/// <returns>FileObjectModel</returns>
		public async Task<FileObjectModel> File_Get(string fileRef)
		{
			//Grab the fileRecord and if stored in database get the binary also
			var file = _ioClient.FileStore_Get(fileRef);
			if (file == null) return null;

			//Build file for output
			FileObjectModel outputFile = new() { FileRef = file.FileRef, ContentType = file.FileType, Description = file.Description, Name = file.Name + file.Extension };

			switch (file.FileLocation)
			{
				case (int)FileStorageProvider.FileSystem:
					//Get from filesystem
					var memory = new MemoryStream();
					using (var stream = new FileStream(file.FilePath, FileMode.Open))
					{
						await stream.CopyToAsync(memory);
					}
					memory.Position = 0;
					outputFile.Data = memory.ToArray();
					break;


				case (int)FileStorageProvider.Database:
					outputFile.Data = file.Data;
					break;

				default:
					break;
			}

			return outputFile;
		}

		public async Task<InternalFileModel> File_Delete(string fileRef)
		{
			InternalFileModel fileInfo = _ioClient.FileStore_GetInfo(fileRef);
			if (fileInfo?.Id > 0)
			{
				switch (fileInfo.FileLocation)
				{
					case (int)FileStorageProvider.FileSystem:
						if (File.Exists(fileInfo.FilePath))
							File.Delete(fileInfo.FilePath);
						break;

					default:
						break;
				}

				_ioClient.FileStore_Delete(fileInfo.Id);
			}

			return fileInfo;
		}

		public async Task<List<InternalFileModel>> Files_GetAll() => _ioClient.FileStore_GetAll();

		private string Write2Filesystem(string fileNameWithExt, byte[] data, string customer = "", string fileTypeGroup = "")
		{
			try
			{
				//Ensure folder exists
				string fileStorePath = Path.Combine(_fileStoreRoot, customer, fileTypeGroup);
				bool basePathExists = Directory.Exists(fileStorePath);
				if (!basePathExists) Directory.CreateDirectory(fileStorePath);

				//Full filepath
				string fullFilePath = Path.Combine(fileStorePath, fileNameWithExt);

				if (File.Exists(fullFilePath))
					File.Delete(fullFilePath);

				using (var stream = new FileStream(fullFilePath, FileMode.Create))
				{
					stream.Write(data, 0, data.Length);
				}

				return fullFilePath;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private string FileStore_NewFileRef()
		{
			string generatedFileRef = "";

			do
			{
				generatedFileRef = $"{_dynamicFileFlag}{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
			} while (!_ioClient.FileStore_IsFileRefUnique(generatedFileRef));

			return generatedFileRef;
		}

		private bool Startup(out string fileSystemErrors)
		{
			if (string.IsNullOrEmpty(_fileStoreRoot))
				FileSystemStorageAvailable = false;

			//Test file to check access to the file system
			string testFile = Path.Combine(_fileStoreRoot, "TestFile90u34gj93p4n3p9wcfp3h4pc9qf.txt");
			try
			{

				fileSystemErrors = string.Empty;
				if (!Directory.Exists(_fileStoreRoot))
					Directory.CreateDirectory(_fileStoreRoot);

				if (File.Exists(testFile))
					File.Delete(testFile);
				else
				{
					File.CreateText(testFile).Dispose();
					File.Delete(testFile);
				}

				FileSystemStorageAvailable = true;
				return true;
			}
			catch (Exception ex)
			{
				fileSystemErrors = ex.Message;
				FileSystemStorageAvailable = false;
				return false;
			}
		}
	}
}
