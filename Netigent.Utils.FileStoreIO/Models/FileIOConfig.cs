namespace Netigent.Utils.FileStoreIO.Models
{
	public class FileIOConfig
    {
        public static string Section { get; } = "FileIOClient";
        public string Database { get; set; }
        public string FileStoreRoot { get; set; }
        public string FileFlag { get; set; }
        public string DatabaseSchema { get; set; }
    }
}
