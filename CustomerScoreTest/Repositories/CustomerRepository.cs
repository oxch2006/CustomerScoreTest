
using System.Collections.Concurrent;

namespace CustomerScoreTest.Repositories
{
    public class CustomerRepository
    {
        public static ConcurrentDictionary<int, SortedList<long, long>> AllScoresAndCustomers = new ConcurrentDictionary<int, SortedList<long, long>>(); //Store all Score and CustomerIDs
        public static ConcurrentDictionary<long, int> AllCustomerScores = new ConcurrentDictionary<long, int>();//Store all CustomerID and Score
        public static SortedList<int, int> AllScores = new SortedList<int, int>(new ReverseIntComparer());//store all scores from hign to low
        

        //public static ConcurrentQueue<CustomerScore> AllCustomerScoreQueue = new ConcurrentQueue<CustomerScore>(); //Store customer score for add or update
    }

    public class ReverseIntComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            // Compare long in reverse order
            return y.CompareTo(x); 

        }
    }
}
