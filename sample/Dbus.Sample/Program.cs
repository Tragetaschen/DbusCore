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
            await using (var connection = await Connection.CreateAsync(options))
            await using (var orgFreedesktopDbus = connection.Consume<IOrgFreedesktopDbus>()) //, "/org/freedesktop/DBus", "org.freedesktop.DBus"))
            using (connection.Publish(new SampleObject()))
            {
                Console.WriteLine("Connected");

                var names = await orgFreedesktopDbus.ListNamesAsync(default);
                Console.Write("Names:");
                foreach (var name in names)
                {
                    Console.Write(" ");
                    Console.Write(name);
                }
                Console.WriteLine();

                using (var player = connection.Consume<IOrgMprisMediaPlayer2Player>("/org/mpris/MediaPlayer2", "org.mpris.MediaPlayer2.vlc"))
                {
                    player.PropertyChanged += (sender, args) =>
                    {
                        Console.WriteLine($"Property Changed: {args.PropertyName}");
                        if (args.PropertyName == "PlaybackStatus")
                            Console.WriteLine($" Playback status: {player.PlaybackStatus}");
                    };
                    await player.PropertyInitializationFinished;
                    Console.WriteLine("Properties ready");

                    Console.WriteLine($"CanControl: {await player.GetCanControlAsync()}");
                    Console.WriteLine($"CanPause: {player.CanPause}");
                    Console.WriteLine($"CanPlay: {player.CanPlay}");
                    Console.WriteLine($"CanSeek: {player.CanSeek}");
                    Console.WriteLine($"MinimumRate: {player.MinimumRate}");
                    Console.WriteLine($"MaximumRate: {player.MaximumRate}");
                    Console.WriteLine($"Position: {TimeSpan.FromMilliseconds(await player.GetPositionAsync() / 1000.0)}");
                    Console.WriteLine($"Rate: {player.Rate}");
                    Console.WriteLine($"Volume: {player.Volume}");
                    Console.WriteLine("Metadata: ");
                    foreach (var entry in player.Metadata)
                        Console.WriteLine($" {entry.Key}: {entry.Value}");
                    player.Seeked += x =>
                    {
                        Console.WriteLine("Seeked to " + TimeSpan.FromMilliseconds(x / 1000.0));
                    };
                    await player.PlayAsync();
                    await Task.Delay(5000);
                    await player.PauseAsync();
                    await Task.Delay(5000);
                }

                await stopConnection;
            }
        }
    }
}
