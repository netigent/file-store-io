# FileStoreIOClient
A generic layer to allow the saving a loading of file from configurable stores

# How to use

Initially thanks for considering using this library - we hope that it gives you some benefits.
In terms of using the Library the following should get you up and running quickly

### LDAP settings in **appSettings.json**

```
"FileIOClient": {
    "Database": "mysqlserver connection string",
    "FileStoreRoot": "c:\\temp\\files\\",
    "FileFlag": "_$",
    "DatabaseSchema": "dbo"
  }
```
  
### Registering In **Startup.cs**
Register the service into the DI 
```
public void ConfigureServices(IServiceCollection services)
{
	 //Inject FileStoreIOClient provider
	services.Configure<FileIOConfig>(Configuration.GetSection(FileIOConfig.Section));
	services.AddSingleton<IFileStoreIO, FileStoreIO>();
}
```

### Usage in a Controller Example
Utilising in the controller class, the below should give you a good example of how to use the Library

```
using System;

```


