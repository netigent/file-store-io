using System;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxRef
    {
        public long BoxId { get; set; } = default;
        public long FileVersionId { get; set; } = -1;

        public BoxRef() { }

        public BoxRef(BoxEntry boxEntry)
        {
            // BoxFileId/FileVersion.Id
            // 1145221699725/1246311869325

            long boxId = default;
            long fileVersionId = -1;

            if (boxEntry != null)
            {
                try
                {
                    boxId = Convert.ToInt64(boxEntry.Id);
                }
                catch { }


                try
                {
                    if (!string.IsNullOrEmpty(boxEntry?.FileVersion?.Id.ToString()))
                    {
                        fileVersionId = Convert.ToInt64(boxEntry?.FileVersion?.Id.ToString());
                    }
                }
                catch { }
            }

            BoxId = boxId;
            FileVersionId = fileVersionId;
        }

        public BoxRef(string filePath)
        {
            // BoxFileId/FileVersion.Id
            // 1145221699725/1246311869325

            if (!string.IsNullOrEmpty(filePath))
            {


                string[] boxReferences = filePath.Split('/');

                string boxFileId = boxReferences[0];
                string potentialSequenceId = boxReferences.Length > 1 ? boxReferences[1] : string.Empty;

                long boxId = default;
                long fileVersionId = -1;

                try
                {
                    boxId = Convert.ToInt64(boxFileId);
                }
                catch { }

                try
                {
                    if (!string.IsNullOrEmpty(potentialSequenceId))
                    {
                        fileVersionId = Convert.ToInt64(potentialSequenceId);
                    }
                }
                catch { }

                BoxId = boxId;
                FileVersionId = fileVersionId;
            }
        }

        public BoxRef(long boxId, long fileVersionId = -1)
        {
            BoxId = boxId;
            FileVersionId = fileVersionId;
        }

        public string AsFilePath
        {
            get
            {
                return $"{BoxId}{(FileVersionId >= 0 ? $"/{FileVersionId}" : string.Empty)}";
            }
        }
    }

}
