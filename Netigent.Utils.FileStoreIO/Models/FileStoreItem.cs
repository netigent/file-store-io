using Netigent.Utils.FileStoreIO.Enums;
using Netigent.Utils.FileStoreIO.Extensions;
using Netigent.Utils.FileStoreIO.Helpers;
using System;
using System.IO;
using System.Linq;

namespace Netigent.Utils.FileStoreIO.Models
{
    /// <summary>
    /// More reflective version the FileStoreIndex record.
    /// </summary>
    public class FileStoreItem : FileOutput
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
            get => Name.Split([_versionFlag], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        public string NameNoVersionWithExt
        {
            get => Name.Split([_versionFlag], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() + Extension;
        }

        public string FullPath
        {
            get => Path.Combine(Folder, Name + Extension);
        }

        public string NameWithVersion
        {
            get => $"{Name}{_versionFlag}{Version}{Extension}";
        }

        public string ExtClientRefNoVersion
        {
            get => ExtClientRef == null ? null : ExtClientRef.Split([_versionFlag], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() + Extension;
        }

        public string MainGroup(string appPrefix)
        {
            string[] folder = Folder.SplitToTags();
            bool hasAppPrefix = folder.Length > 0 && folder[0].Equals(appPrefix, StringComparison.InvariantCultureIgnoreCase);

            return hasAppPrefix
                ? folder[1] ?? string.Empty
                : folder[0] ?? string.Empty;
        }

        public string SubGroup(string appPrefix)
        {
            string[] folder = Folder.SplitToTags();
            bool hasAppPrefix = folder.Length > 0 && folder[0].Equals(appPrefix, StringComparison.InvariantCultureIgnoreCase);

            return hasAppPrefix
                ? folder[2] ?? string.Empty
                : folder[1] ?? string.Empty;
        }
        #endregion

        #region ctor
        public FileStoreItem()
        {

        }

        public FileStoreItem(FileStoreItem model)
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
            Folder = model.Folder;
        }

        public FileStoreItem(string fullPath, byte[] contents, FileStorageProvider fsp = FileStorageProvider.UseDefault, string appPrefix = "", string description = "", DateTime created = default, string uploadedBy = "")
        {
            var createdDate = created == default ? DateTime.UtcNow : created;
            var extension = Path.GetExtension(fullPath);

            Name = Path.GetFileNameWithoutExtension(fullPath);
            MimeType = MimeHelper.GetMimeType(extension);
            Extension = extension;
            Description = description;
            UploadedBy = uploadedBy;
            Data = contents;
            Created = createdDate;
            Modified = createdDate;
            FileLocation = (int)fsp;
            SizeInBytes = contents.LongLength;
            Folder = fullPath.ToRelativeFolder(includePrefix: appPrefix);
        }
        #endregion

        /// <summary>
        /// Extension of file e.g. .pdf .xls .doc etc
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Folder e.g. ./MyAppPrefix/RecordId/Category/Id/ 
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// 3rd Party Path, e.g. Box {Fileid}/{RevisionId} 1185335280675/1291996203075, AWS-S3 /myFolder/subFolder/subFolder/myfile.pdf
        /// </summary>
        public string ExtClientRef { get; set; }

        /// <summary>
        /// Internal Index Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Name of file e.g. '200094 March Invoice 13149R'
        /// </summary>
        public string Name { get; set; }

        public string UploadedBy { get; set; }


        public int FileLocation { get; set; }

        public int Version { get; set; } = 1;

        public long SizeInBytes { get; set; } = -1;

        public DateTime? Created { get; set; } = DateTime.UtcNow;
        public DateTime? Modified { get; set; } = DateTime.UtcNow;
    }
}