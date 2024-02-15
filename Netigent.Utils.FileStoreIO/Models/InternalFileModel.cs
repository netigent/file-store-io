using Netigent.Utils.FileStoreIO.Extensions;
using System;
using System.Linq;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class InternalFileModel
    {
        #region Internal
        private const string _versionFlag = "__ver_";
        #endregion

        #region Public ReadOnly Props
        /// <summary>
        /// Original Name with Version info strippped out...
        /// </summary>
        public string OrginalNameNoExt
        {
            get
            {
                return Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
        }

        public string OrginalNameWithExt
        {
            get
            {
                return Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() + Extension;
            }
        }

        public string MainGroup(string appPrefix)
        {
            string[] pathTags = PathTags.SplitToTags();
            bool hasAppPrefix = pathTags.Length > 0 && pathTags[0].Equals(appPrefix, StringComparison.InvariantCultureIgnoreCase);

            return hasAppPrefix
                ? pathTags[1] ?? string.Empty
                : pathTags[0] ?? string.Empty;
        }

        public string SubGroup(string appPrefix)
        {
            string[] pathTags = PathTags.SplitToTags();
            bool hasAppPrefix = pathTags.Length > 0 && pathTags[0].Equals(appPrefix, StringComparison.InvariantCultureIgnoreCase);

            return hasAppPrefix
                ? pathTags[2] ?? string.Empty
                : pathTags[1] ?? string.Empty;
        }

        public int VersionInfo
        {
            get
            {
                string[] parts = Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 1 && Int32.TryParse(s: parts[1], out int existingMaxId))
                {
                    return existingMaxId;
                }

                return default;
            }
        }
        #endregion

        #region ctor
        public InternalFileModel()
        {

        }

        public InternalFileModel(InternalFileModel model)
        {
            Id = model.Id;
            Name = model.Name;
            MimeType = model.MimeType;
            Extension = model.Extension;
            Description = model.Description;
            UploadedBy = model.UploadedBy;
            ExtClientRef = model.ExtClientRef;
            Data = model.Data;
            FileRef = model.FileRef;
            Created = model.Created;
            Modified = model.Modified;
            FileLocation = model.FileLocation;
            SizeInBytes = model.SizeInBytes;
            PathTags = model.PathTags;
        }
        #endregion

        public byte[] Data { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Extension of file e.g. .pdf .xls .doc etc
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// PathTags e.g. MyAppPrefix|RecordId|Category|Id 
        /// </summary>
        public string PathTags { get; set; }

        /// <summary>
        /// 3rd Party Path, e.g. Box {Fileid}/{RevisionId} 1185335280675/1291996203075, AWS-S3 /myFolder/subFolder/subFolder/myfile.pdf
        /// </summary>
        public string ExtClientRef { get; set; }

        /// <summary>
        /// Unique FileRef for your file object e.g. _$eabdd19081e04ddeb38bf2a871e7893b
        /// </summary>
        public string FileRef { get; set; }


        public string MimeType { get; set; }
        public long Id { get; set; }

        /// <summary>
        /// Name of file e.g. '200094 March Invoice 13149R'
        /// </summary>
        public string Name { get; set; }
        public string UploadedBy { get; set; }
        public int FileLocation { get; set; }

        public long SizeInBytes { get; set; } = -1;

        public DateTime? Created { get; set; } = DateTime.UtcNow;
        public DateTime? Modified { get; set; } = DateTime.UtcNow;

    }
}