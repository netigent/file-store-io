using Newtonsoft.Json;

namespace Netigent.Utils.FileStoreIO.Clients.Box.Models
{
    public class BoxItem : BoxEntryVersion
    {
        public BoxItemType ItemType
        {
            get
            {
                switch (Type.ToLower())
                {
                    case "folder":
                        return BoxItemType.Folder;

                    case "web_link":
                        return BoxItemType.Web_Link;

                    default:
                        // Could be file or VErsion of file
                        return BoxItemType.File;
                }
            }
        }
        [JsonProperty("sequence_id")]
        public object SequenceId { get; set; }

        public string Etag { get; set; }

        public string Name { get; set; }

        // Only Valid for WebLink
        public string Url { get; set; }

        // Only Valid for File
        [JsonProperty("file_version")]
        public BoxEntryVersion FileVersion { get; set; }
    }

}
