using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Dbus.CodeGenerator;

namespace WebApplication.Dbus
{
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

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
