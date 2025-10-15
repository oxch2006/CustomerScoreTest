namespace CustomerScoreTest.Models
{
    public class GetGustomersByRankResponse: ResponseBase
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();
    }
}
