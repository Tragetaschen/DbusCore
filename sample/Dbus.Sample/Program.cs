using System;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tcs = new TaskCompletionSource<int>();
            var workTask = Task.Run(() => work(tcs.Task));
            var readlineTask = Task.Factory.StartNew(() =>
            {
                Console.ReadLine();
                tcs.SetResult(0);
            }, TaskCreationOptions.LongRunning);

            try
            {
                var tasks = new[] { workTask, readlineTask };
                var completedTask = Task.WaitAny(tasks);
                tasks[completedTask].Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Ended in error");
                Console.WriteLine(e);
            }
        }

        private static async Task work(Task stopConnection)
        {
            Console.WriteLine("Running");
            var address = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
            using (var connection = await Connection.CreateAsync(address))
            using (var orgFreedesktopDbus = new OrgFreedesktopDbus(connection))
            using (var orgFreedesktopUpower = new OrgFreedesktopUpower(connection))
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

                var requestResult = await orgFreedesktopDbus.RequestNameAsync("com.dbuscore.sample", 0);
                Console.WriteLine($"Request result: {requestResult}");

                var properties = await orgFreedesktopUpower.GetAllAsync();
                foreach (var pair in properties)
                    Console.WriteLine($"Key: {pair.Key} Value: {pair.Value}");

                await stopConnection;
            }
        }
    }
}
