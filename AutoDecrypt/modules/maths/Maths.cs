using System;
using System.IO;
using System.Linq;

namespace AutoDecrypt.modules.maths
{
    /// <summary>
    /// Class <c>Maths</c> provides mathematical functions to aid in decryption attempts.
    /// </summary>
    internal static class Maths
    {
        private static int[] _primes;
        private static int[] _pi;

        public static void Generate()
        {
            _primes = GeneratePrimes(900000);
            _pi = File.ReadAllText(IOTools.PersistentPath("_data/Pi.txt")).Select(n => int.Parse(n.ToString())).ToArray();
        }

        private static int[] GeneratePrimes(int n)
        {
            ParallelQuery<int> r = from i in Enumerable.Range(2, n - 1).AsParallel()
                                   where Enumerable.Range(1, (int)Math.Sqrt(i)).All(j => j == 1 || i % j != 0)
                                   select i;
            return r.ToArray().OrderBy(c => c).ToArray();
        }

        public static int Prime(int n)
        {
            return _primes[n];
        }

        public static int Pi(int n)
        {
            return _pi[n];
        }

        public static int Totient(int n)
        {
            int result = n;

            for (int p = 2; p * p <= n; ++p)
            {
                if (n % p == 0)
                {
                    while (n % p == 0)
                    {
                        n /= p;
                    }
                    result -= result / p;
                }
            }

            if (n > 1)
            {
                result -= result / n;
            }
            return result;
        }

        public static int Mod(int m, int n)
        {
            return ((m %= n) < 0) ? m + n : m;
        }
    }
}