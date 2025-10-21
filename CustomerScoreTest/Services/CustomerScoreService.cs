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

        #region Save score and customer count to CustomerRepository.AllScoresAndCustomerCount
        lock (_locker)
        {
            //handle old score
            if (CustomerRepository.AllScoresAndCustomerCount.ContainsKey(customer.Value))
            {
                CustomerRepository.AllScoresAndCustomerCount[customer.Value] = CustomerRepository.AllScoresAndCustomerCount[customer.Value] - 1;
            }

            //handle new score
            if (CustomerRepository.AllScoresAndCustomerCount.ContainsKey(newScore))
            {
                CustomerRepository.AllScoresAndCustomerCount[newScore] = CustomerRepository.AllScoresAndCustomerCount[newScore] + 1;
            }
            else
            {
                CustomerRepository.AllScoresAndCustomerCount[newScore] = 1;
            }

        }
        #endregion

        response.Score = newScore;
        return await Task.FromResult(response);
    }

    public async Task<List<Customer>> GetGustomersByRank(int start, int end)
    {
        List<Customer> customers = new List<Customer>();
        int rank = 0;
        List<int> scores = new List<int>();
        foreach (var scorePair in CustomerRepository.AllScoresAndCustomerCount)
        {
            var score = scorePair.Key;
            var customerCountInScore = scorePair.Value;
            rank = rank + customerCountInScore;
            if (rank < start) continue;
            scores.Add(score);
            if (rank > end) break;
        }
        int? maxScore = scores.FirstOrDefault();
        if (maxScore == null) return customers;

        int minRank = CustomerRepository.AllScoresAndCustomerCount.Where(s => s.Key > maxScore).Sum(c => c.Value);
        foreach (int score in scores)
        {
            foreach (var customer in CustomerRepository.AllCustomerScores.Where(c => c.Value == score))
            {
                customers.Add(new Customer()
                {
                    CustomerID = customer.Key,
                    Score = customer.Value
                });
            }
        }

        customers = customers.OrderByDescending(c => c.Score).ThenBy(c => c.CustomerID).ToList();
        foreach (var c in customers)
        {
            c.Rank = minRank + 1;
            minRank++;
        }

        customers = customers.Where(c => c.Rank >= start && c.Rank <= end).ToList();
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
            int[] allScores = CustomerRepository.AllScoresAndCustomerCount.Select(s => s.Key).Reverse().ToArray();
            int customerScoreIndex = Search(allScores, customerScore);
            //int customerScoreIndex = CustomerRepository.AllScoresAndCustomerCount.IndexOfKey(customerScore);
            int lowScore = 0, highScore = 0;

            #region get high score and low score
            int lowScoreIndex = customerScoreIndex;
            while (lowScoreIndex < CustomerRepository.AllScoresAndCustomerCount.Count && lowScoreIndex <= customerScoreIndex + low)
            {
                lowScore = CustomerRepository.AllScoresAndCustomerCount.GetKeyAtIndex(lowScoreIndex);
                lowScoreIndex++;
            }

            int highScoreIndex = customerScoreIndex;
            while (highScoreIndex >= 0 && highScoreIndex >= customerScoreIndex - high)
            {
                highScore = CustomerRepository.AllScoresAndCustomerCount.GetKeyAtIndex(highScoreIndex);
                highScoreIndex--;
            }
            #endregion

            int minRank = CustomerRepository.AllScoresAndCustomerCount.Where(s => s.Key > highScore).Sum(c => c.Value);
            List<int> scores = CustomerRepository.AllScoresAndCustomerCount.Where(k=>k.Key>=lowScore && k.Key<=highScore).Select(k=>k.Key).ToList();
            foreach (int score in scores)
            {
                foreach (var cs in CustomerRepository.AllCustomerScores.Where(c => c.Value == score))
                {
                    customers.Add(new Customer()
                    {
                        CustomerID = cs.Key,
                        Score = cs.Value
                    });
                }
            }
            customers = customers.OrderByDescending(c => c.Score).ThenBy(c => c.CustomerID).ToList();
            foreach (var c in customers)
            {
                c.Rank = minRank + 1;
                minRank++;
            }
            customerRank = customers.First(c => c.CustomerID == customerid).Rank;
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

    public int Search(int[] arr, int key)
    {
        if (arr == null || arr.Length == 0)
            return -1;

        int low = 0;
        int high = arr.Length - 1;

        while (low <= high && key >= arr[low] && key <= arr[high])
        {

            if (arr[high] == arr[low])
            {
                if (arr[low] == key)
                    return low;
                return -1;
            }

            int pos = low + ((key - arr[low]) * (high - low)) / (arr[high] - arr[low]);

            if (pos < low || pos > high)
                break;

            if (arr[pos] == key)
                return pos;
            else if (arr[pos] < key)
                low = pos + 1;
            else
                high = pos - 1;
        }

        return -1;
    }
}

