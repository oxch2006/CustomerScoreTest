using CustomerScoreTest.Models;
using CustomerScoreTest.Repositories;
using System;
using System.Security.Cryptography.Xml;

namespace CustomerScoreTest.Services;

public class CustomerScoreService : ICustomerScoreService
{
    private static object _locker = new object();

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
        if (customer.Key == 0)
        {
            CustomerRepository.AllScoresAndCustomers[newScore].Add(customerid, customerid);
        }
        else
        {
            CustomerRepository.AllScoresAndCustomers[customer.Value].Remove(customerid); //remove the customerid from the old score
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
            rank2 = rank;
            if (rank < start) continue;
            
            foreach (var customer in customersInScore)
            {
                if (rank2 >= start && rank2 <= end)
                {
                    customers.Add(new Customer()
                    {
                        CustomerID = customer.Key,
                        Score = score,
                        Rank = ++rank2
                    });
                }
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
        if (customer.Key != 0) //find the customerid
        {
            int customerScore = customer.Value;
            int customerRank = 0;
            int rank = 0;
            int rank2 = 1;
            foreach (var scorePair in CustomerRepository.AllScores)
            {
                var score = scorePair.Key;
                var customersInScore = CustomerRepository.AllScoresAndCustomers[score];
                rank = rank + customersInScore.Count();
                if (score >= customerScore - low && score <= customerScore + high) //use score instead rank, expand the range
                {
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

}

