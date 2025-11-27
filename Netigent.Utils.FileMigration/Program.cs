using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Netigent.Utils.FileStoreIO;
using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Models;

public class ProviderType
{
    public FileStorageProvider Provider { get; set; }
    public char ShortCode { get; set; }

    public bool isReady { get; set; } = false;

    public ProviderType(IFileStoreIOClient fileStore, FileStorageProvider fileStorageProvider, char shortCode)
    {
        Provider = fileStorageProvider;
        ShortCode = shortCode;
        isReady = fileStore.IsClientAvailable(fileStorageProvider);
    }
}

internal class Program
{
    private static IFileStoreIOClient _client;
    private static bool _isReady { get; set; } = false;

    private static string LogFile;

    private static IList<ProviderType> ProviderList { get; set; }

    private static IOptions<FileStoreIOConfig>? fileStoreIOConfig { get; set; }

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

    static void WriteProviders()
    {
        LogMessage($"Destinations");
        foreach (var p in ProviderList)
            if (p.isReady)
                LogMessage($"{p.ShortCode} = {p.Provider}", true, ConsoleColor.DarkGreen);


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

        fileStoreIOConfig = services.GetService<IOptions<FileStoreIOConfig>>();

        LogMessage("Netigent FileStore Attachment Migration (Starting)...");

        _client = new FileStoreIOClient(fileStoreIOConfig);
        ProviderList = new List<ProviderType>()
        {
            new ProviderType(_client, FileStorageProvider.Database, 'd'),
            new ProviderType(_client, FileStorageProvider.FileSystem, 'f'),
            new ProviderType(_client, FileStorageProvider.Box, 'b'),
            new ProviderType(_client, FileStorageProvider.S3, 's'),
            new ProviderType(_client, FileStorageProvider.Azure, 'a'),
        };

        if (_client.IsReady)
        {
            LogMessage($"");
            LogMessage($"Logfile for this session is '{LogFile}'", true, ConsoleColor.Yellow);
            WriteProviders();

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


        var fileList = _client.Files_GetAllV2("");
        if (fileList?.Count > 0)
        {
            var uFiles = fileList.DistinctBy(x => x.FileRef).ToList();
            LogMessage($"There are {uFiles.Count}x files");
            foreach (var p in ProviderList)
                LogMessage($"{p.Provider} = {uFiles.Where(x => x.FileLocation == (int)p.Provider).Count()}x files");

        }
        else
        {
            LogMessage("No Files");


        }
        LogMessage("===============");

        LogMessage("");


        return true;
    }

    private static bool ListAllFiles()
    {
        LogMessage("List Files:");
        LogMessage("================");


        var fileList = _client.Files_GetAllV2("");
        if (fileList?.Count > 0)
        {
            var uFiles = fileList.DistinctBy(x => x.FileRef).ToList();
            long tFiles = uFiles.Count();
            for (int i = 0; i < uFiles.Count; i++)
            {
                var fileInfo = uFiles[i];
                var vh = _client.File_GetVersionsInfo(fileInfo.FileRef);

                LogMessage($"{i + 1}:\tRef:{fileInfo.FileRef}\tVersions: {vh.Count}\tLocation: {GetLocation(fileInfo.FileLocation)}\t{fileInfo.NameNoVersionWithExt}");
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

        FileStorageProvider? sp = AskDestination();
        if (sp == null)
        {
            LogMessage("-= eXITING oPERATION =-");
            return false;
        }

        LogMessage($"Confirm you want to BUILD FILE INDEX ON '{sp}', this may take a long time.......... (y/n)?", true, ConsoleColor.Cyan);
        string confirm = (Console.ReadLine() ?? "").Trim().ToLower();

        LogMessage($"Should we limit indexing to the root '{fileStoreIOConfig.Value.AppPrefix}' (y/n)?", true, ConsoleColor.Cyan);
        string limitToScope = (Console.ReadLine() ?? "").Trim().ToLower();

        if (confirm == "y")
        {

            var result = await _client.File_IndexAsync(sp ?? FileStorageProvider.UseDefault, scopeToAppPrefix: !string.IsNullOrEmpty(fileStoreIOConfig.Value.AppPrefix) && limitToScope == "y");

            LogMessage("");
            LogMessage("================");
            LogMessage($"Build File Index Complete, Success = {result.Success}, Messages = {string.Join(", ", result.Messages)}");
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

        var fileList = _client.Files_GetAllV2("");
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
                    LogMessage($"{i + 1}:\tRef:{fileInfo.FileRef}\tVersion #:{z}\tLocation: {GetLocation(fileInfo.FileLocation)}\t{fileInfo.NameNoVersionWithExt}", false);

                    var fileObject = await _client.File_GetAsyncV2(fileInfo.FileRef, z);

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

    private static FileStorageProvider? GetLocation(int fileLocation)
    {
        return ProviderList.FirstOrDefault(x => (int)x.Provider == fileLocation).Provider;
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
            LogMessage($"Confirm Delete '{fi.NameNoVersionWithExt}' in {GetLocation(fi.FileLocation)} (y/n)?", true, ConsoleColor.Cyan);

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

    static FileStorageProvider? AskDestination()
    {
        LogMessage($"Destinations");
        foreach (var p in ProviderList)
            LogMessage($"{p.ShortCode} = {p.Provider}", true, ConsoleColor.DarkGreen);

        LogMessage("Press Any Key to End", true, ConsoleColor.Cyan);
        char locaitons = (Console.ReadLine() ?? "").Trim().ToLower().FirstOrDefault();

        foreach (var l in ProviderList)
        {
            if (locaitons == l.ShortCode)
                return l.Provider;
        }

        return null;
    }

    private static async Task<bool> MigrateFilesAsync(string fileRef = "")
    {
        bool allFiles = string.IsNullOrEmpty(fileRef) ? true : false;

        if (allFiles)
        {
            LogMessage("ALL File Migration:");
            LogMessage("======================");
        }

        FileStorageProvider? sp = AskDestination();
        if (sp == null)
        {
            LogMessage("-= eXITING oPERATION =-");
            return false;
        }

        LogMessage("(M)ove or (C)opy, Move, will delete original IF file copies and byte count matches, copy will leave original where it is... But record will be updated... You are advised to backup the FileStoreIndex table....", true, ConsoleColor.Cyan);
        char opType = (Console.ReadLine() ?? "").Trim().ToLower().FirstOrDefault();
        bool moveOnly = opType == 'm';


        string opType1 = moveOnly ? "Move" : "Copy";

        LogMessage($"Confirm {opType1} '{(allFiles ? "AllFiles" : $"File: {fileRef}")}' to {sp} (y/n)?", true, ConsoleColor.Cyan);

        string confirm = (Console.ReadLine() ?? "").Trim().ToLower();
        if (confirm == "y")
        {
            if (allFiles)
            {
                var fileList = _client.Files_GetAllV2("");
                int tFiles = fileList.Count;
                LogMessage($"{opType1} {tFiles}x files to {sp} ");
                if (tFiles > 0)
                {
                    for (int i = 0; i < fileList.Count; i++)
                    {
                        var fileItem = fileList[i];
                        LogMessage($"{i + 1}/{tFiles}: {opType1} '{fileItem.Name}' ({fileItem.FileRef})... ", false);
                        _ = await MoveFileAsync(fileItem.FileRef, sp ?? FileStorageProvider.UseDefault, moveOnly);
                    }
                }
            }
            else
            {
                var fileToMove = _client.File_GetVersionsInfo(fileRef).FirstOrDefault();
                if (fileToMove != null)
                {
                    LogMessage($"1/1: {opType1} '{fileToMove.Name}' ({fileToMove.FileRef})... ", false);
                    _ = await MoveFileAsync(fileToMove.FileRef, sp ?? FileStorageProvider.UseDefault, moveOnly);
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

    private static async Task<bool> MoveFileAsync(string fileRef, FileStorageProvider loc, bool moveFile)
    {
        var result = await _client.File_Migrate(fileRef, loc, moveFile);
        if (result.Success)
        {
            LogMessage("Confirming...", false, ConsoleColor.Yellow);
            FileOutput? checkFile = await _client.File_GetAsyncV2(fileRef, 0);

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
            LogMessage($"Migration Error Message: {string.Join(",", result.Messages)}", true, ConsoleColor.Yellow);
            return false;
        }
    }

    private static string GetCommand()
    {
        LogMessage($"AppPrefix: {fileStoreIOConfig?.Value?.AppPrefix}, Command Options:");
        LogMessage("================");
        LogMessage("0 = File Statics (Good Place to Start)");
        LogMessage("1 = List All Files");
        LogMessage("2 = Migrate Single File");
        LogMessage("3 = Migrate All Files");
        LogMessage("4 = Build Index");
        LogMessage("5 = Delete Single File");
        LogMessage("6 = Verify All Files (This will download each file and its version, slow!)");
        LogMessage("Type option (or ANY KEY to exit)", true, ConsoleColor.Cyan);
        LogMessage("");

        // Return Option
        return (Console.ReadLine() ?? "").Trim();
    }
}