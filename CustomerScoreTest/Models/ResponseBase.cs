namespace CustomerScoreTest.Models
{
    public class ResponseBase
    {
        public ResponseBase()
        {
            ResponseHeader = new ResponseHeader()
            {
                StatusCode = "0",
                SubStatusCode = "0"
            };
        }

        public ResponseHeader ResponseHeader { get; set; }
    }

    public class ResponseHeader
    {
        public string? StatusCode { get; set; }
        public string? SubStatusCode { get; set; }
        public string? Message { get; set; }
    }

}
