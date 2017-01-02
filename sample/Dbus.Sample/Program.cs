using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(work).Wait();
        }

        private static async Task work()
        {
            Console.WriteLine("Running");
            using (var connection = await Connection.CreateAsync())
            {
                Console.WriteLine("Connected");
                var path = await connection.HelloAsync();
                Console.WriteLine($"Done: {path}");
                Console.ReadLine();
            }
        }
    }
}
