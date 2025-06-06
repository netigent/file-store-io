# FileStoreIOClient
A generic layer to allow the saving and loading of file from configurable stores using a uniqueFileRef with prefix code for detection in your code, supports UNC, Database, Box and AWS S3 Buckets.

# How to use
Initially thanks for considering using this library - we hope that it gives you some benefits.
In terms of using the Library the following should get you up and running quickly

**Database** The library will autocreate and upgrade filestore table {schema}.[FileStoreIndex]. You can then manually remove column no longer used [FileType], [MainGroup] ,[SubGroup] from [FileStoreIndex] if you want - they're longer used!

# Version Changes
**1.2.1** Bug fix Subfile, gets replaced by single Parent. RecursiveSearch = false is now default, if you want a list of all files, including subFolders then set as true.
```
Files_GetAllV2(string[] pathTags, bool recursiveSearch = false);
Files_GetAllV2(string relationalFilePathAndName, bool recursiveSearch = false);
```
**1.2.0** Recursive flag has been added to Files_GetAllV2. Obsolete legacy methods have been removed.

**1.1.7** Bug fix for AppPrefix setting not populating correctly. Upgraded .net to 8.0 LTS support and various internal package upgrades.

**1.1.6** Database Migration update, include AppPrefix into PathTags column, if you implemented v1.1.5 and are using AppPrefix and your files have 'disappeared', then run (this will apply update to ensure files show again)
```
UPDATE {schema}.[FileStoreIndex] SET PathTags = '{AppPrefix}/' + PathTags WHERE PathTags NOT LIKE '{AppPrefix}%'
```

**1.1.5** Readme updates and stablization updates.

**1.1.4** Internal database upgrade.

**1.1.3** Added this Readme.md into package

**1.1.2** Added Legacy MainGroup, SubGroup helpers onto InternalFileModel, also added helper Properties onto the client AppPrefix and PathSeperator

**1.1.1** Added Support for AWS S3 Bucket Storage, upgraded code to .net 7 and .net Standard 2.0, some methods have been marked as Obsolete and have been repointed to newer methods. Older versions are still working, but try and swap.

**1.0.14** Upgraded Bounycastle dependencies.

**1.0.13** If the appSettings / DefaultStorage, it will default to Database.

**1.0.10** Support for indexing existing Box Storage account (still to come FileStore Indexing and >50MB files in Box). New field sizeInBytes for the file.

**1.0.9** Missing Prop in DI / appSettings for default Storage, updating Readme example

**1.0.8** Support for Box Storage (>50MB files not yet supported will be in next version), Migrate File function (keep file references and relocate the binary to new location), default Provider

**1.0.7** Relational Filepath Storage, makes it easier to move UNC shares around

**1.0.6** Versioning, if you via Constructor increment maxVersions the app will store that many latest copies, if you push same file+ext in the same mainGroup, subGroup, it considers same file and will return same fileRef keeping X last versions.

**1.0.5** Upgraded to .net 6 LTS

**1.0.4** Stablity improvements.

**1.0.3** Stablity improvements.

**1.0.2** 'Customer' has been relabelled to 'MainGroup', and 'FileTypeGroup' relabelled to 'SubGroup' for clarity and added functionality to access those. 'FileType' is also now known as 'MimeType' to reflect the data stored.

```
	Files_GetAllV2(string[] pathTags);
	Files_GetAllV2(string relationalFilePathAndName);
```

## Direct Usage
You can use the client directly as follows, keeping Last 3 versions of the file

```
    BoxConfig myBoxConfig = new BoxConfig()
    {
        EnterpriseID = "123456789",
        BoxAppSettings = new BoxAppSettings()
        {
            ClientID = "exampleid12345",
            ClientSecret = "examplesecret12345",
            AppAuth = new BoxAppAuth()
            {
                Passphrase = "examplepassphrase12345",
                PrivateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
                PublicKeyID = "abc1234",
            },
        },
		TimeoutInMins = 15,
    };

	S3Config myS3Config = new S3Config()
    {
        AccessKey = "ExampleAccessKey",
        BucketName = "my-example-bucket-name",
        Region = "us-west-2",
        SecretKey = "mysecretkeyinhere",
    };

	FileSystemConfig myFsConfig = new FileSystemConfig()
    {
        RootFolder = @"C:\temp\Files\",
        StoreFileAsUniqueRef = false,
    };

	IFileStoreIOClient fileStoreIOClient = new FileStoreIOClient(
                appPrefix: "MyApp",
                databaseConnection: "mysqlserver connection string",
                maxVersions: 3,
                dbSchema: _dbSchema,
                defaultFileStore: FileStorageProvider.FileSystem,
                boxConfig: myBoxConfig,
                s3Config: myS3Config,
                fileSystemConfig: myFsConfig);
	
	var newFile = await fileStoreIOClient.File_GetAsyncV2("_$23fe627c5a5b410aa6017db308b71077");
```

## Using Dependency Injection
The below method steps you through using IServiceCollection registration and injection to your controllers

### FileStoreIOClient settings in **appSettings.json**

Define via appSettings, keeping last 5 versions of file.

```
"FileStoreIO": {
	"Database": "Server=.;Database=myDatabase;UID=mySa;PWD=myPassword;",
	"AppPrefix": "MyAppToScopeTo",               
	"FilePrefix": "_$",
	"DatabaseSchema": "filestore",
	"MaxVersions": 5,
	"DefaultStorage": "S3",
	"S3": {
		"AccessKey": "ExampleAccessKey",
		"SecretKey": "mysecretkeyinhere+",
		"Region":  "us-west-2",
		"BucketName": "my-example-bucket-name"
		},
	"Box": {
		"EnterpriseID": "123456789",
		"AutoCreateRoot": false,
		"TimeoutInMins": 15,
		"BoxAppSettings": {
			"ClientID": "exampleid12345",
			"ClientSecret": "examplesecret12345",
			"AppAuth": {
					"Passphrase": "examplepassphrase12345",
					"PrivateKey": "-----BEGIN ENCRYPTED PRIVATE KEY-----\nEXAMPLEEXAMPLEEXMAPLE\n-----END ENCRYPTED PRIVATE KEY-----\n",
					"PublicKeyID": "abc1234"
			}
		}
	},
	"FileSystem": {
		"RootFolder": "c:\\temp\\files\\",
		"StoreFileAsUniqueRef": false
	}
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
using Netigent.Utils.FileStoreIO.Enums;
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
			var uploadedCode = await _ioClient.File_UpsertAsync(selectedFile, uploadLocation, description: description, customerCode, itemType);
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
			var fileInfo = await _ioClient.File_DeleteAsync(id);
			return RedirectToAction("Index");
		}
	}
}
```
