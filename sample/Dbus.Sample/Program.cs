using Dbus.CodeGenerator;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "gen")
            {
                var code = Generator.Run();
                File.WriteAllText("DbusImplementations.Generated.cs", @"namespace Dbus.Sample
{
" + code + @"
}
");
                return;
            }

            DbusImplementations.Init();

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
            using (var orgFreedesktopDbus = new OrgFreedesktopDbus(connection)) //, "/org/freedesktop/DBus", "org.freedesktop.DBus"))
            using (connection.Publish(new SampleObject()))
            {
                orgFreedesktopDbus.NameAcquired += async x =>
                {
                    await Task.Run(() => Console.WriteLine($"Name acquired {x}"));
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

                var requestResult = await orgFreedesktopDbus.RequestNameAsync("org.dbuscore.sample", 0);
                Console.WriteLine($"Request result: {requestResult}");

                await stopConnection;
            }
        }
    }
}
