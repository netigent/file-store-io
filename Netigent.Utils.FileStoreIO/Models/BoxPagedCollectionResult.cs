namespace Netigent.Utils.FileStoreIO.Models
{
    public class BoxPagedCollectionResult : BoxCollectionResult
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public BoxOrder[] Order { get; set; }
    }

}
