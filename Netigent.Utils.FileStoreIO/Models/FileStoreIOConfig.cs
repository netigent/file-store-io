namespace Netigent.Utils.FileStoreIO.Models
{
	public class FileStoreIOConfig
    {
        public static string Section { get; } = "FileStoreIO";
        public string Database { get; set; }
        public string FileStoreRoot { get; set; }
        public string FilePrefix { get; set; }
        public string DatabaseSchema { get; set; }
        public bool StoreFileAsUniqueRef { get; set; } = true;
    }
}
