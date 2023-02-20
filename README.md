# FileStoreIOClient
A generic layer to allow the saving and loading of file from configurable stores using a uniqueFileRef with prefix code for detection in your code, supports UNC, Database and Box.

# How to use
Initially thanks for considering using this library - we hope that it gives you some benefits.
In terms of using the Library the following should get you up and running quickly

# Version Changes
**1.0.8** Support for Box Storage (>50MB files not yet supported will be in next version), Migrate File function (keep file references and relocate the binary to new location), default Provider

**1.0.7** Relational Filepath Storage, makes it easier to move UNC shares around

**1.0.6** Versioning, if you via Constructor increment maxVersions the app will store that many latest copies, if you push same file+ext in the same mainGroup, subGroup, it considers same file and will return same fileRef keeping X last versions.

**1.0.5** Upgraded to .net 6 LTS

**1.0.4** Stablity improvements.

**1.0.3** Stablity improvements.

**1.0.2** 'Customer' has been relabelled to 'MainGroup', and 'FileTypeGroup' relabelled to 'SubGroup' for clarity and added functionality to access those. 'FileType' is also now known as 'MimeType' to reflect the data stored.

***Database*** If you can best to delete the table [FileStoreIndex] and allow recreation, but you dont have too. The Client will auto upgrade your database, you can then manually remove column [FileType] from [FileStoreIndex] if you want - its no longer used!

```
	Files_GetByMainGroup(string mainGroup);
	Files_GetBySubGroup(string subGroup);
	Files_GetByMainAndSubGroup(string mainGroup, string subGroup);
```

## Direct Usage
You can use the client directly as follows, keeping Last 3 versions of the file

```
    BoxConfig myExampleBoxReference = new BoxConfig()
    {
        EnterpriseID = "123456789",
        BoxAppSettings = new BoxAppSettings()
        {
            ClientID = "exampleid12345",
            ClientSecret = "examplesecret12345",
            AppAuth = new AppAuth()
            {
                Passphrase = "examplepassphrase12345",
                PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
                PublicKeyID = "abc1234",
            },
        },
        RootFolder = 0,
    };

	IFileStoreIOClient fileStoreIOClient = new FileStoreIOClient("mysqlserver connection string", "c:\\temp\\files\\", "dbo", 3, myExampleBoxReference);
	var newFile = fileStoreIOClient.File_Get("_$23fe627c5a5b410aa6017db308b71077");
```

## Using Dependency Injection
The below method steps you through using IServiceCollection registration and injection to your controllers

### FileStoreIOClient settings in **appSettings.json**

Define via appSettings, keeping last 5 versions of file.

```
"FileStoreIO": {
		"Database": "mysqlserver connection string",
		"FileStoreRoot": "c:\\temp\\files\\",
		"FilePrefix": "_$",
		"DatabaseSchema": "filestore",
		"StoreFileAsUniqueRef":  true,
		"MaxVersions": 5,
		"BoxConfig": {
					EnterpriseID = "123456789",
					BoxAppSettings = new BoxAppSettings()
					{
						ClientID = "exampleid12345",
						ClientSecret = "examplesecret12345",
						AppAuth = new AppAuth()
						{
							Passphrase = "examplepassphrase12345",
							PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
							PublicKeyID = "abc1234",
						},
					},
					RootFolder = 0,
		},
  },
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
			var xyz = await something();
			var fileInfo = _ioClient.File_Delete(id);
			return RedirectToAction("Index");
		}
	}
}


```


