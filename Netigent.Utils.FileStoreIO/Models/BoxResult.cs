using Newtonsoft.Json;
using System;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxResult : BoxEntry
    {
        [JsonProperty("path_collection")]
        public BoxCollectionResult PathCollection { get; set; }

        [JsonProperty("trashed_at")]
        public DateTime? TrashedAt { get; set; }

        [JsonProperty("purged_at")]
        public DateTime? PurgedAt { get; set; }

        [JsonProperty("content_created_at")]
        public DateTime? ContentCreatedAt { get; set; }

        [JsonProperty("content_modified_at")]
        public DateTime? ContentModifiedAt { get; set; }

        [JsonProperty("shared_link")]
        public object SharedLink { get; set; }

        [JsonProperty("folder_upload_email")]
        public object FolderUploadEmail { get; set; }
        public object Parent { get; set; }

        [JsonProperty("item_status")]
        public string ItemStatus { get; set; }

        [JsonProperty("item_collection")]
        public BoxPagedCollectionResult ItemCollection { get; set; }
    }

}
