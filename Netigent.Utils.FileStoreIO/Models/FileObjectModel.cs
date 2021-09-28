namespace Netigent.Utils.FileStoreIO.Models
{
	public class FileObjectModel
	{
		public string FileRef { get; set; }
		public string Name {  get; set; }	
		public string Description {  get; set; }
		public string ContentType {  get; set; }	
		public byte[] Data {  get; set; }
	}
}
