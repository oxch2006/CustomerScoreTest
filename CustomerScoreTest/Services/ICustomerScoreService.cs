using CustomerScoreTest.Models;

namespace CustomerScoreTest.Services;
public interface ICustomerScoreService
{
    Task<UpdateCustomerResponse> UpdateCustomer(long customerid, int score);

    Task<List<Customer>> GetGustomersByRank(int start, int end);

    Task<List<Customer>> GetGustomersByCustomerId(long customerid, int high, int low);

    Task ImportData();
}

