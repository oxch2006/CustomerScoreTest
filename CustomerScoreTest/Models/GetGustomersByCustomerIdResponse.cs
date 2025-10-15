namespace CustomerScoreTest.Models
{
    public class GetGustomersByCustomerIdResponse: ResponseBase
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();
    }
}
