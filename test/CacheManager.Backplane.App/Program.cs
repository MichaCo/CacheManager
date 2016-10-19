using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.Extensions.Configuration;

namespace CacheManager.Backplane.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Environment.NewLine);
            try
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .AddJsonFile("cache.json")
                    .Build();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("done");
            Console.Read();
        }        
    }
}