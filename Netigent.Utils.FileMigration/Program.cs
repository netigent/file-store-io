using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Abstractions;
using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Enum;
using Netigent.Utils.FileStoreIO.Models;
using System.Drawing;

internal class Program
{
    private static IFileStoreIOClient _client;
    private static bool _isReady { get; set; } = false;
    private static bool _isUNCReady { get; set; } = false;
    private static bool _isBoxReady { get; set; } = false;

    private static string LogFile;

    private static void LogMessage(string message, bool newLine = true, ConsoleColor textColour = ConsoleColor.White)
    {
        if (!string.IsNullOrEmpty(LogFile))
        {

            using (var fo = File.AppendText(LogFile))
            {
                if (newLine)
                {
                    fo.WriteLine(message);

                }
                else
                {
                    fo.Write(message);
                }
            }
        }

        Console.ForegroundColor = textColour;

        if (newLine)
        {

            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    static async Task Main(string[] args)
    {
        string outputFolder = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(outputFolder);

        LogFile = Path.Combine(outputFolder, $"Log_{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")}.txt");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection()
            .AddOptions()
            .Configure<FileStoreIOConfig>(configuration.GetSection(FileStoreIOConfig.Section))
            .BuildServiceProvider();

        var options = services.GetService<IOptions<FileStoreIOConfig>>();

        LogMessage("Netigent FileStore Attachment Migration (Starting)...");

        _isReady = Init(options);

        if (_isReady)
        {
            LogMessage($"");
            LogMessage($"Logfile for this session is '{LogFile}'", true, ConsoleColor.Yellow);
            LogMessage($"");

            LogMessage($"Storage Options:");
            LogMessage($"================");
            LogMessage($"Database = Y", true, ConsoleColor.DarkGreen);
            LogMessage($"FileSystem = {(_isUNCReady ? "Y" : "N")} ({options.Value.FileStoreRoot})", true, _isUNCReady ? ConsoleColor.Green : ConsoleColor.Red);
            LogMessage($"Box = {(_isBoxReady ? "Y" : "N")} (EnterpriseId: {options.Value.Box?.EnterpriseID}, AppId: {options.Value.Box?.BoxAppSettings?.ClientID})", true, _isBoxReady ? ConsoleColor.Green : ConsoleColor.Red);
            LogMessage($"");

            bool exitSystem = false;
            do
            {
                string option = GetCommand();
                switch (option)
                {
                    case "0":
                        _ = PrintStats();

                        break;

                    case "1":
                        _ = ListAllFiles();
                        break;

                    case "2":
                        _ = await MigrateSingleFile();
                        break;

                    case "3":
                        _ = await MigrateFilesAsync();

                        break;

                    case "4":
                        _ = await BuildFilesIndex();

                        break;

                    case "5":
                        _ = await DeleteSingleFile();
                        break;

                    case "6":
                        _ = await ValidateAllFiles();
                        break;

                    default:
                        exitSystem = true;
                        break;
                }

            } while (!exitSystem);

        }
        else
        {
            // End Application
            LogMessage("Press Any Key to End", true, ConsoleColor.Cyan);
            var outinfo = Console.ReadLine();
        }
    }

    private static bool PrintStats()
    {
        LogMessage("File Statistics:");
        LogMessage("================");


        var fileList = _client.Files_GetAll();
        if (fileList?.Count > 0)
        {
            var uFiles = fileList.DistinctBy(x => x.FileRef).ToList();
            long tFiles = uFiles.Count();
            long dbCount = uFiles.Where(x => x.FileLocation == (int)FileStorageProvider.Database).Count();
            long fsCount = uFiles.Where(x => x.FileLocation == (int)FileStorageProvider.FileSystem).Count();
            long boxCount = uFiles.Where(x => x.FileLocation == (int)FileStorageProvider.Box).Count();

            LogMessage($"There are {uFiles.Count}x files");
            LogMessage($"Database = {dbCount}");
            LogMessage($"Filesystem = {fsCount}");
            LogMessage($"Box = {boxCount}");
        }
        else
        {
            LogMessage("No Files");


        }
        LogMessage("===============");

        LogMessage("");


        return true;
    }

    private static FileStorageProvider GetLocation(int loc)
    {
        switch (loc)
        {
            case (int)FileStorageProvider.Database:
                return FileStorageProvider.Database;

            case (int)FileStorageProvider.Box:
                return FileStorageProvider.Box;

            case (int)FileStorageProvider.FileSystem:
                return FileStorageProvider.FileSystem;

            default:
                return FileStorageProvider.UseDefault;
        }
    }


    private static bool ListAllFiles()
    {
        LogMessage("List Files:");
        LogMessage("================");


        var fileList = _client.Files_GetAll();
        if (fileList?.Count > 0)
        {
            var uFiles = fileList.DistinctBy(x => x.FileRef).ToList();
            long tFiles = uFiles.Count();
            for (int i = 0; i < uFiles.Count; i++)
            {
                var fileInfo = uFiles[i];
                var vh = _client.File_GetVersionsInfo(fileInfo.FileRef);

                LogMessage($"{i + 1}:\tRef:{fileInfo.FileRef}\tVersions: {vh.Count}\tLocation: {GetLocation(fileInfo.FileLocation)}\t{fileInfo.RawName}");
            }
        }
        else
        {
            LogMessage("No Files");
        }

        LogMessage("===============");
        LogMessage("");
        return true;
    }

    private static async Task<bool> BuildFilesIndex()
    {
        LogMessage("Build File Index:");
        LogMessage("================");
        LogMessage("Pick location (F)ilesystem or (B)ox (anything else will cancel command) then press ENTER", true, ConsoleColor.Cyan);
        string location = (Console.ReadLine() ?? "").Trim().ToLower();

        FileStorageProvider sp = FileStorageProvider.UseDefault;

        switch (location)
        {
            case "b":
                sp = FileStorageProvider.Box;
                break;

            case "f":
                sp = FileStorageProvider.FileSystem;
                break;

            default:
                LogMessage("Cancelled");
                LogMessage("");

                return false;
        }

        LogMessage($"Confirm you want to BUILD FILE INDEX ON '{sp}', this may take a long time.......... (y/n)?", true, ConsoleColor.Cyan);

        string confirm = (Console.ReadLine() ?? "").Trim().ToLower();

        if (confirm == "y")
        {
            var result = await _client.File_IndexAllAsync(sp);

            LogMessage("");
            LogMessage("================");
            LogMessage($"Build File Index Complete, Success = {result.Success}, Messages = {string.Join(", ", result.Message)}");
            LogMessage("================");
            LogMessage("");
            return true;
        }
        else
        {
            LogMessage("Cancelled");
            LogMessage("");
            return false;
        }

    }

    private static async Task<bool> ValidateAllFiles()
    {
        LogMessage("Verfiy ALL Files (this maybe take some time!!):");
        LogMessage("================");


        var fileList = _client.Files_GetAll();
        if (fileList?.Count > 0)
        {
            var uFiles = fileList.DistinctBy(x => x.FileRef).ToList();
            long tFiles = uFiles.Count();
            int good = 0;
            int bad = 0;
            LogMessage($"Items to Files {tFiles}, Versions {fileList.Count}");

            for (int i = 0; i < uFiles.Count; i++)
            {
                var fileInfo = uFiles[i];
                var vh = _client.File_GetVersionsInfo(fileInfo.FileRef);

                for (int z = 0; z < vh.Count; z++)
                {
                    LogMessage($"{i + 1}:\tRef:{fileInfo.FileRef}\tVersion #:{z}\tLocation: {GetLocation(fileInfo.FileLocation)}\t{fileInfo.RawName}", false);

                    var fileObject = await _client.File_Get(fileInfo.FileRef, z);

                    if (fileObject != null && fileObject?.Data?.LongLength > 0)
                    {
                        LogMessage(" Success", true, ConsoleColor.DarkGreen);
                        good++;
                    }
                    else
                    {
                        LogMessage(" Error", true, ConsoleColor.Red);
                        bad++;
                    }
                }
            }

            LogMessage($"Files {tFiles}x, Total-Versioned Files {fileList.Count}x Good={good} Bad={bad}");
        }
        else
        {
            LogMessage("No Files");
        }
        LogMessage("");
        return true;
    }


    private static async Task<bool> DeleteSingleFile()
    {
        LogMessage("DELETE Single File:");
        LogMessage("======================");
        LogMessage("Type FileRef: e.g. _$3421412342314123421341231, then press ENTER", true, ConsoleColor.Cyan);
        string fileRef = (Console.ReadLine() ?? "").Trim();

        var fi = _client.File_GetVersionsInfo(fileRef).FirstOrDefault();

        if (fi != null)
        {
            LogMessage($"Confirm Delete '{fi.RawName}' in {GetLocation(fi.FileLocation)} (y/n)?", true, ConsoleColor.Cyan);

            string confirm = (Console.ReadLine() ?? "").Trim().ToLower();

            if (confirm == "y")
            {
                LogMessage($"Deleting FileRef: {fileRef}...", false);
                var result = await _client.File_DeleteAsync(fileRef);
                if (result)
                {
                    LogMessage($"Done...", true, ConsoleColor.Green);

                }
                else
                {
                    LogMessage($"Warning...", true, ConsoleColor.Red);
                }
            }
            else
            {
                LogMessage($"Cancelled Delete...", true, ConsoleColor.Cyan);
            }
        }
        else
        {
            LogMessage($"No File...", true, ConsoleColor.Cyan);
        }

        return true;

    }

    private static async Task<bool> MigrateSingleFile()
    {
        LogMessage("Single File Migration:");
        LogMessage("======================");
        LogMessage("Type FileRef: e.g. _$3421412342314123421341231, then press ENTER", true, ConsoleColor.Cyan);
        string fileRef = (Console.ReadLine() ?? "").Trim();

        return await MigrateFilesAsync(fileRef);

    }

    private static async Task<bool> MigrateFilesAsync(string fileRef = "")
    {
        bool allFiles = string.IsNullOrEmpty(fileRef) ? true : false;

        if (allFiles)
        {
            LogMessage("ALL File Migration:");
            LogMessage("======================");
        }

        LogMessage("Destination ?:");
        LogMessage("(D)atabase, (F)ilesystem or (B)ox (anything else will cancel command) then press ENTER", true, ConsoleColor.Cyan);
        string location = (Console.ReadLine() ?? "").Trim().ToLower();

        FileStorageProvider sp = FileStorageProvider.UseDefault;

        switch (location)
        {
            case "b":
                sp = FileStorageProvider.Box;
                break;

            case "d":
                sp = FileStorageProvider.Database;
                break;

            case "f":
                sp = FileStorageProvider.FileSystem;
                break;

            default:
                LogMessage("Cancelled");
                LogMessage("");

                return false;
        }

        LogMessage($"Confirm Move '{(allFiles ? "AllFiles" : $"File: {fileRef}")}' to {sp} (y/n)?", true, ConsoleColor.Cyan);

        string confirm = (Console.ReadLine() ?? "").Trim().ToLower();

        if (confirm == "y")
        {
            if (allFiles)
            {
                var fileList = _client.Files_GetAll();
                int tFiles = fileList.Count;
                LogMessage($"Moving {tFiles}x files to {sp} ");
                if (tFiles > 0)
                {
                    for (int i = 0; i < fileList.Count; i++)
                    {
                        var fileItem = fileList[i];
                        LogMessage($"{i + 1}/{tFiles}: Moving '{fileItem.Name}' ({fileItem.FileRef})... ", false);
                        _ = await MoveFileAsync(fileItem.FileRef, sp);
                    }
                }
            }
            else
            {
                var fileToMove = _client.File_GetVersionsInfo(fileRef).FirstOrDefault();
                if (fileToMove != null)
                {
                    LogMessage($"1/1: Moving '{fileToMove.Name}' ({fileToMove.FileRef})... ", false);
                    _ = await MoveFileAsync(fileToMove.FileRef, sp);
                }
                else
                {
                    LogMessage($"{fileRef}: Not Found... ");
                }
            }

        }

        // Return Option
        LogMessage("");
        return true;
    }

    private static async Task<bool> MoveFileAsync(string fileRef, FileStorageProvider loc)
    {
        var result = await _client.File_Migrate(fileRef, loc);
        if (result.Success)
        {
            LogMessage("Confirming...", false, ConsoleColor.Yellow);
            FileObjectModel? checkFile = await _client.File_Get(fileRef, 0);

            if (checkFile != null && checkFile?.Data?.LongLength > 0)
            {
                LogMessage($"Success ({checkFile?.Data.LongLength} bytes)....", true, ConsoleColor.Green);
            }
            else
            {
                LogMessage("Error missing file / zero sized!!!", true, ConsoleColor.Red);

            }

            return true;
        }
        else
        {
            LogMessage($"Migration Error Message: {string.Join(",", result.Message)}", true, ConsoleColor.Yellow);
            return false;
        }
    }

    private static string GetCommand()
    {
        LogMessage("Command Options:");
        LogMessage("================");
        LogMessage("0 = File Statics (Good Place to Start)");
        LogMessage("1 = List All Files");
        LogMessage("2 = Migrate Single File");
        LogMessage("3 = Migrate All Files");
        LogMessage("4 = Build Index (Not available yet)");
        LogMessage("5 = Delete Single File");
        LogMessage("6 = Verify All Files (This will download each file and its version, slow!)");
        LogMessage("Type option (or ANY KEY to exit)", true, ConsoleColor.Cyan);
        LogMessage("");

        // Return Option
        return (Console.ReadLine() ?? "").Trim();
    }

    private static bool Init(IOptions<FileStoreIOConfig> options)
    {
        try
        {
            // var options = services.GetService<IOptions<FileStoreIOConfig>>();

            _client = new FileStoreIOClient(options);
            _isUNCReady = _client.IsFileSystemAvailable;
            _isBoxReady = _client.IsBoxAvailable;

            return true;
        }
        catch (Exception ex)
        {
            LogMessage("Error Starting Client " + ex.Message.ToString());
        }

        return false;
    }
}