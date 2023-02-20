using Newtonsoft.Json;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxConflictResult
    {
        public string Code { get; set; }

        [JsonProperty("context_info")]
        public BoxContextInfoResult ContextInfo { get; set; }
        public string Message { get; set; }
        [JsonProperty("request_id")]
        public string RequestId { get; set; }
        public int Status { get; set; }
        public string Type { get; set; }
    }

}
