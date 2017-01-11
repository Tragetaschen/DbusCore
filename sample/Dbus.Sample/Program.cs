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
                tasks[completedTask].GetAwaiter().GetResult();
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
            var options = new DbusConnectionOptions()
            {
                Address = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"),
            };
            using (var connection = await Connection.CreateAsync(options))
            using (var orgFreedesktopDbus = connection.Consume<IOrgFreedesktopDbus>()) //, "/org/freedesktop/DBus", "org.freedesktop.DBus"))
            using (connection.Publish(new SampleObject()))
            {
                orgFreedesktopDbus.NameAcquired += async x =>
                {
                    await Task.Run(() => Console.WriteLine($"Name acquired {x}"));
                };
                Console.WriteLine("Connected");

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

                using (var player = connection.Consume<IOrgMprisMediaPlayer2Player>("/org/mpris/MediaPlayer2", "org.mpris.MediaPlayer2.vlc"))
                {
                    Console.WriteLine($"CanControl: {await player.GetCanControlAsync()}");
                    Console.WriteLine($"CanPause: {await player.GetCanPauseAsync()}");
                    Console.WriteLine($"CanPlay: {await player.GetCanPlayAsync()}");
                    Console.WriteLine($"CanSeek: {await player.GetCanSeekAsync()}");
                    Console.WriteLine($"MinimumRate: {await player.GetMinimumRateAsync()}");
                    Console.WriteLine($"MaximumRate: {await player.GetMaximumRateAsync()}");
                    Console.WriteLine($"Position: {TimeSpan.FromMilliseconds(await player.GetPositionAsync() / 1000.0)}");
                    Console.WriteLine($"Rated {await player.GetRateAsync()}");
                    Console.WriteLine($"Volume: {await player.GetVolumeAsync()}");
                    Console.WriteLine("Metadata: ");
                    foreach (var entry in await player.GetMetadataAsync())
                        Console.WriteLine($" {entry.Key}: {entry.Value}");
                    player.Seeked += x =>
                    {
                        Console.WriteLine("Seeked to " + TimeSpan.FromMilliseconds(x / 1000.0));
                    };
                    Console.WriteLine($"Is Playing: {await player.GetPlaybackStatusAsync()}");
                    await player.PlayAsync();
                    Console.WriteLine($"Is Playing: {await player.GetPlaybackStatusAsync()}");
                    await Task.Delay(5000);
                    Console.WriteLine($"Is Playing: {await player.GetPlaybackStatusAsync()}");
                    await player.PauseAsync();
                    Console.WriteLine($"Is Playing: {await player.GetPlaybackStatusAsync()}");
                }

                await stopConnection;
            }
        }
    }
}
