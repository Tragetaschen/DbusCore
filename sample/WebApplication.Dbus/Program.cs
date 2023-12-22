using Dbus.CodeGenerator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace WebApplication.Dbus;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "gen")
        {
            var code = Generator.Run();
            File.WriteAllText("DbusImplementations.Generated.cs", @"namespace WebApplication.Dbus
{
" + code + @"
}
");
            return;
        }

        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
