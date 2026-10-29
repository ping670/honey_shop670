using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the amount: ");
            int amount = int.Parse(Console.ReadLine());

            Console.Write("Enter the coins separated by space: ");
            int[] coins = Console.ReadLine().Split(' ').Select(int.Parse).ToArray();

            var result = GreedyCoins(amount, coins);

            if (result != null)
            {
                Console.WriteLine("Used coins: " + string.Join(", ", result));
            }
            else
            {
                Console.WriteLine("Cannot pay the exact amount with these coins.");
            }
        }

        static List<int> GreedyCoins(int amount, int[] coins)
        {
            var sortedCoins = coins.OrderByDescending(c => c).ToList();
            List<int> result = new List<int>();

            foreach (int coin in sortedCoins)
            {
                while (amount >= coin)
                {
                    result.Add(coin);
                    amount -= coin;
                }
            }

            if (amount != 0)
            {
                return null;
            }

            return result;
        }
    }
}