
using System.Collections.Concurrent;

namespace CustomerScoreTest.Repositories
{
    public class CustomerRepository
    {
        //public static ConcurrentDictionary<int, SortedList<long, long>> AllScoresAndCustomers = new ConcurrentDictionary<int, SortedList<long, long>>(); //Store all Score and CustomerIDs
        //public static ConcurrentDictionary<long, int> AllCustomerScores = new ConcurrentDictionary<long, int>();//Store all CustomerID and Score
        //public static SortedList<int, int> AllScores = new SortedList<int, int>(new ReverseIntComparer());//store all scores from hign to low

        public static SortedList<int, int> AllScoresAndCustomerCount = new SortedList<int, int>(new ReverseNumberComparer<int>());//store all scores(from hign to low) and customer count
        public static ConcurrentDictionary<long, int> AllCustomerScores = new ConcurrentDictionary<long, int>();//Store all CustomerID and Score
    }

    public class ReverseNumberComparer<T> : IComparer<T> where T : IComparable
    {
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }
}
