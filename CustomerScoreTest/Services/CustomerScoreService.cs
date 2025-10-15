using CustomerScoreTest.Models;
using CustomerScoreTest.Repositories;

namespace CustomerScoreTest.Services;

public class CustomerScoreService : ICustomerScoreService
{
    private static object _locker = new object();

    private readonly IWebHostEnvironment _webHostEnvironment;

    public CustomerScoreService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<UpdateCustomerResponse> UpdateCustomer(long customerid, int score)
    {
        int newScore = score;
        UpdateCustomerResponse response = new UpdateCustomerResponse()
        {
            CustomerID = customerid,
            Score = score
        };

        var customer = CustomerRepository.AllCustomerScores.FirstOrDefault(c => c.Key == customerid);

        #region Save CustomerScore to CustomerRepository.AllCustomerScores
        if (customer.Key == 0)
        {
            bool addResult = CustomerRepository.AllCustomerScores.TryAdd(customerid, newScore);
            if (!addResult) throw new Exception($"Add CustomerScore failed:customerid:{customerid}, score:{newScore}");
        }
        else
        {
            newScore = customer.Value + score;
            bool updateResult = CustomerRepository.AllCustomerScores.TryUpdate(customerid, newScore, customer.Value);
            if (!updateResult) throw new Exception($"Update CustomerScore failed:customerid:{customerid}, newScore:{newScore}, oldScore:{customer.Value}");
        }
        #endregion

        #region Save score to CustomerRepository.AllScores
        lock (_locker)
        {
            if (!CustomerRepository.AllScores.ContainsKey(newScore))
            {
                CustomerRepository.AllScores.Add(newScore, newScore);
            }
        }
        #endregion

        #region Save customer to CustomerRepository.AllScoresAndCustomers
        if (!CustomerRepository.AllScoresAndCustomers.ContainsKey(newScore))
            CustomerRepository.AllScoresAndCustomers.TryAdd(newScore, new SortedList<long, long>());
        if (customer.Key == 0)
        {
            CustomerRepository.AllScoresAndCustomers[newScore].Add(customerid, customerid);
        }
        else
        {
            CustomerRepository.AllScoresAndCustomers[customer.Value].Remove(customerid); //remove the customerid from the old score
            if(CustomerRepository.AllScoresAndCustomers[customer.Value].Count == 0)
            {
                CustomerRepository.AllScoresAndCustomers.TryRemove(customer.Value, out _); //clear score which not has customer
                lock (_locker)
                {
                    if (CustomerRepository.AllScores.ContainsKey(customer.Value))
                    {
                        CustomerRepository.AllScores.Remove(customer.Value); //clear score which not has customer
                    }
                }
            }
            CustomerRepository.AllScoresAndCustomers[newScore].Add(customerid, customerid); //add the customerid to the new score
        }
        #endregion

        response.Score = newScore;
        return await Task.FromResult(response);
    }

    public async Task<List<Customer>> GetGustomersByRank(int start, int end)
    {
        List<Customer> customers = new List<Customer>();
        int rank = 0, rank2 = 0;
        foreach (var scorePair in CustomerRepository.AllScores)
        {
            var score = scorePair.Key;
            var customersInScore = CustomerRepository.AllScoresAndCustomers[score];
            rank = rank + customersInScore.Count();
            if (rank < start) continue;
            rank2 = rank - customersInScore.Count() + 1;
            foreach (var customer in customersInScore)
            {
                if (rank2 >= start && rank2 <= end)
                {
                    customers.Add(new Customer()
                    {
                        CustomerID = customer.Key,
                        Score = score,
                        Rank = rank2
                    });
                }
                rank2++;
                if (rank2 > end) break;
            }

            if (rank > end) break;

        }
        return await Task.FromResult(customers);
    }

    public async Task<List<Customer>> GetGustomersByCustomerId(long customerid, int high, int low)
    {
        List<Customer> customers = new List<Customer>();
        var customer = CustomerRepository.AllCustomerScores.FirstOrDefault(c => c.Key == customerid);
        if (customer.Key != 0) // the customerid exist
        {
            int customerScore = customer.Value;
            int customerRank = 0;
            int rank = 0;
            int customerScoreIndex = CustomerRepository.AllScores.IndexOfKey(customerScore);
            int lowScore=0, highScore=0;

            #region get high score and low score
            int lowScoreIndex = customerScoreIndex;
            while (lowScoreIndex < CustomerRepository.AllScores.Count && lowScoreIndex < customerScoreIndex + low)
            {
                lowScore = CustomerRepository.AllScores.GetKeyAtIndex(lowScoreIndex);
                lowScoreIndex++;
            }

            int highScoreIndex = customerScoreIndex;
            while (highScoreIndex >= 0 && highScoreIndex> customerScoreIndex - high)
            {
                highScore = CustomerRepository.AllScores.GetKeyAtIndex(highScoreIndex);
                highScoreIndex--;
            }
            #endregion

            foreach (var scorePair in CustomerRepository.AllScores)
            {
                var score = scorePair.Key;
                var customersInScore = CustomerRepository.AllScoresAndCustomers[score];
                rank = rank + customersInScore.Count();
                if (score >= customerScore - lowScore && score <= customerScore + highScore) //use score instead rank, expand the range
                {
                    int rank2 = rank - customersInScore.Count() + 1;
                    foreach (var customerId in customersInScore)
                    {
                        customers.Add(new Customer()
                        {
                            CustomerID = customerId.Key,
                            Score = score,
                            Rank = rank2
                        });

                        if (customerId.Key == customerid)
                        {
                            customerRank = rank2;
                        }
                        rank2++;
                    }
                }
            }
            customers = customers.Where(c => c.Rank >= customerRank - high && c.Rank <= customerRank + low).ToList();
        }

        return await Task.FromResult(customers);
    }

    public async Task ImportData()
    {
        string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "customerScore.txt");
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] fields = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length >= 2)
                {
                    if (fields[0].Trim().ToLower() == "customerid") continue;
                    long customerId = long.Parse(fields[0].Trim());
                    int score = int.Parse(fields[1].Trim());
                    await UpdateCustomer(customerId, score);
                }

            }
        }
    }
}

