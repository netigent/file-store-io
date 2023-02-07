using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxEntryVersion
    {
        // Version
        public string Type { get; set; }
        public string Id { get; set; }
        public string Sha1 { get; set; }
    }

    public class BoxEntry : BoxEntryVersion
    {
        [JsonProperty("sequence_id")]
        public object SequenceId { get; set; }

        public string Etag { get; set; }

        public string Name { get; set; }

        [JsonProperty("file_version")]
        public BoxEntryVersion FileVersion { get; set; }

        [JsonProperty("created_at")]
        public DateTime? CreatedDt { get; set; }

        [JsonProperty("modified_at")]
        public DateTime? ModifiedDt { get; set; }
        public string? Description { get; set; }
        public int? Size { get; set; }
    }

    public class BoxResult : BoxEntry
    {
        [JsonProperty("path_collection")]
        public PathCollectionResult PathCollection { get; set; }

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
        public ItemCollectionResult ItemCollection { get; set; }
    }

    public class PathCollectionResult
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
        public BoxEntry[] Entries { get; set; }
    }

    public class ItemCollectionResult : PathCollectionResult
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public Order[] Order { get; set; }
    }

    public class Order
    {
        public string By { get; set; }
        public string Direction { get; set; }
    }


    public class ConflictResult
    {
        public string Code { get; set; }

        [JsonProperty("context_info")]
        public ContextInfoResult ContextInfo { get; set; }
        public string Message { get; set; }
        [JsonProperty("request_id")]
        public string RequestId { get; set; }
        public int Status { get; set; }
        public string Type { get; set; }
    }

    public class ContextInfoResult
    {
        public BoxEntry conflicts { get; set; }
    }
}
