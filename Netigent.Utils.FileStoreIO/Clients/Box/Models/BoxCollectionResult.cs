using Newtonsoft.Json;

namespace Netigent.Utils.FileStoreIO.Clients.Box.Models
{
    public class BoxCollectionResult
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
        public BoxEntry[] Entries { get; set; }
    }

}
