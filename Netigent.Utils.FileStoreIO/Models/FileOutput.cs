namespace Netigent.Utils.FileStoreIO.Models
{
    /// <summary>
    /// A Simple Output Version of the File intended for output to browser such as a download.
    /// </summary>
    public class FileOutput
    {
        /// <summary>
        /// Unique FileRef for your file object e.g. _$eabdd19081e04ddeb38bf2a871e7893b
        /// </summary>
        public string FileRef { get; set; }

        /// <summary>
        /// Name of the File with extension e.g. Summary.pdf
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// File Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// What MimeType is the file according to Extension e.g. application/pdf
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// The Binary of the File.
        /// </summary>
        public byte[] Data { get; set; }
    }
}
