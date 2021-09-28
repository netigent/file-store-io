using System;

namespace Netigent.Utils.FileStoreIO.Models
{
	public class CustomerFiles
	{
		public DateTime? Created { get; set; }
		public int CustomerId { get; set; }
		public long FileId { get; set; }
		public long Id { get; set; }
		public DateTime? Modified { get; set; }
	}
}