using System.Collections.Generic;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class ResultModel
    {
        public bool Success { get; set; } = false;
        public List<string> Messages { get; set; } = new List<string>();

        public ResultModel() { }

        public ResultModel(bool success, string message)
        {
            Success = success;
            Messages = new List<string> { message };
        }

        public ResultModel(bool success, List<string> messages)
        {
            Success = success;
            Messages = messages;
        }
    }
}
