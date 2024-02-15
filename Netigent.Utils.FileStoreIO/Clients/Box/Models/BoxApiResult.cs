namespace Netigent.Utils.FileStoreIO.Clients.Box.Models
{
    public class BoxApiResult
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }

        public string Message { get; set; }

        public BoxEntry Result { get; set; }
    }
}
