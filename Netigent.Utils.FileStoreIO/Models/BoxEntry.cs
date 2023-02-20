using Newtonsoft.Json;
using System;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxEntry : BoxItem
    {
        [JsonProperty("created_at")]
        public DateTime? CreatedDt { get; set; }

        [JsonProperty("modified_at")]
        public DateTime? ModifiedDt { get; set; }
        public string? Description { get; set; }
        public int? Size { get; set; }
    }

}
