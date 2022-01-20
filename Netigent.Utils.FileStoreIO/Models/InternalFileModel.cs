using System;

namespace Netigent.Utils.FileStoreIO.Models
{
	public class InternalFileModel
	{
		public DateTime? Created { get; set; }
		public byte[] Data { get; set; }
		public string Description { get; set; }
		public string Extension { get; set; }
		public string FilePath { get; set; }
		public string FileRef { get; set; }
		public string MimeType { get; set; }
		public long Id { get; set; }
		public DateTime? Modified { get; set; }
		public string Name { get; set; }
		public string UploadedBy { get; set; }
		public int FileLocation { get; set; }
		public string MainGroup { get; set; }
		public string SubGroup { get; set; }
	}
}