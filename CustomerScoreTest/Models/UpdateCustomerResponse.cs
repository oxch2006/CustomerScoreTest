namespace CustomerScoreTest.Models
{
    public class UpdateCustomerResponse: ResponseBase
    {
        public long CustomerID { get; set; }
        public int Score { get; set; }
    }
}
