using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tcs = new TaskCompletionSource<int>();
            var task = Task.Run(() => work(tcs.Task));
            Console.ReadLine();
            tcs.SetResult(0);
            task.Wait();
        }

        private static async Task work(Task shouldContinue)
        {
            Console.WriteLine("Running");
            using (var connection = await Connection.CreateAsync())
            using (var orgFreedesktopDbus = new OrgFreedesktopDbus(connection))
            {
                orgFreedesktopDbus.NameAcquired += x =>
                {
                    Console.WriteLine($"Name acquired {x}");
                };
                Console.WriteLine("Connected");
                var path = await orgFreedesktopDbus.HelloAsync();
                Console.WriteLine($"Done: {path}");

                var names = await orgFreedesktopDbus.ListNamesAsync();
                Console.Write("Names:");
                foreach (var name in names)
                {
                    Console.Write(" ");
                    Console.Write(name);
                }
                Console.WriteLine();

                await shouldContinue;
            }
        }
    }
}
