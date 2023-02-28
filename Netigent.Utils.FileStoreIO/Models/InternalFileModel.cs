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
        public string RawName
        {
            get
            {
                return Name.Split(new string[] { _versionFlag }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
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
            Created = model.Created;
            Data = model.Data;
            Description = model.Description;
            Extension = model.Extension;
            FilePath = model.FilePath;
            FileRef = model.FileRef;
            MimeType = model.MimeType;
            Id = model.Id;
            Modified = model.Modified;
            Name = model.Name;
            UploadedBy = model.UploadedBy;
            FileLocation = model.FileLocation;
            MainGroup = model.MainGroup;
            SubGroup = model.SubGroup;
            SizeInBytes = model.SizeInBytes;
        }
        #endregion

        public DateTime? Created { get; set; }
        public byte[] Data { get; set; }
        public string Description { get; set; }
        public string Extension { get; set; }
        public string FilePath { get; set; }
        public string FileRef { get; set; }
        public string MimeType { get; set; }
        public long Id { get; set; }
        public DateTime? Modified { get; set; }
        public string Name { get; set; }
        public string UploadedBy { get; set; }
        public int FileLocation { get; set; }
        public string MainGroup { get; set; }
        public string SubGroup { get; set; }
        public long SizeInBytes { get; set; } = -1;
    }
}