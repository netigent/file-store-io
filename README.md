# FileStoreIOClient
A generic layer to allow the saving and loading of file from configurable stores using a uniqueFileRef with prefix code for detection in your code.

# How to use
Initially thanks for considering using this library - we hope that it gives you some benefits.
In terms of using the Library the following should get you up and running quickly

## Direct Usage
You can use the client directly as follows

```
	IFileStoreIOClient fileStoreIOClient = new FileStoreIOClient("mysqlserver connection string", "c:\\temp\\files\\", "dbo");
	var newFile = fileStoreIOClient.File_Get("_$23fe627c5a5b410aa6017db308b71077");
```

## Using Dependency Injection
The below method steps you through using IServiceCollection registration and injection to your controllers

### FileStoreIOClient settings in **appSettings.json**

```
"FileStoreIO": {
    "Database": "mysqlserver connection string",
    "FileStoreRoot": "c:\\temp\\files\\",
    "FilePrefix": "_$",
    "DatabaseSchema": "filestore",
    "StoreFileAsUniqueRef":  true
  }
```
  
### Registering In **Startup.cs**
Register the service into the DI 
```
public void ConfigureServices(IServiceCollection services)
{
	 //Inject FileStoreIOClient provider
	services.Configure<FileStoreIOConfig>(Configuration.GetSection(FileStoreIOConfig.Section));
	services.AddSingleton<Netigent.Utils.FileStoreIO.IFileStoreIOClient, FileStoreIOClient>();
}
```

### Usage in a Controller Example
Utilising in the controller class, the below should give you a good example of how to use the Library

```
using Netigent.Examples.UploadApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Enum;
using System.Threading.Tasks;

namespace Netigent.Examples.UploadApp.Controllers
{
	public class HomeController : Controller
	{
		public readonly IFileStoreIOClient _ioClient;

		public HomeController(IFileStoreIOClient fileStoreIOClient)
		{
			_ioClient = fileStoreIOClient;
		}

		public async Task<IActionResult> Index()
		{
			ViewBag.Message = TempData["Message"];
			return View(new FileUploadViewModel { Files = await _ioClient.Files_GetAll() });
		}

		[HttpPost]
		[ActionName("Upload")]
		public async Task<IActionResult> Upload(IFormFile selectedFile, string location = "database", string description = "", string customerCode = "", string itemType = "")
		{
			var uploadLocation = (location ?? "").Equals("Database", System.StringComparison.CurrentCultureIgnoreCase) ? FileStorageProvider.Database : FileStorageProvider.FileSystem;
			var uploadedCode = await _ioClient.File_Upsert(selectedFile, uploadLocation, description: description, customerCode, itemType);
			TempData["Message"] = $"File successfully uploaded to {uploadLocation.ToString()} {uploadedCode}";

			//Setting success properties
			TempData["FileId"] = uploadedCode;
			TempData["FileLink"] = $"https://localhost:44317/Home/GetFile/{uploadedCode}";

			//In this case you can now for example render dynamic images from FileStoreIOClient
			//e.g. <img src="@TempData["FileLink"]" style="height: 100px; width: auto;" />
			
			return RedirectToAction("Index");
		}

		public async Task<IActionResult> GetFile(string id)
		{
			var file = await _ioClient.File_Get(id);
			if (file == null) return null;

			return File(file.Data, file.ContentType, file.Name);
		}

		public async Task<IActionResult> DeleteFile(string id)
		{
			var fileInfo = await _ioClient.File_Delete(id);
			TempData["Message"] = $"Removed {fileInfo.Name} successfully from File System.";
			return RedirectToAction("Index");
		}
	}
}


```


