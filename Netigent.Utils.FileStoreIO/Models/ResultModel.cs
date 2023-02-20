using System.Collections.Generic;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class ResultModel
    {
        public bool Success { get; set; } = false;     
        public List<string> Message { get; set; } = new List<string>();
    }
}
