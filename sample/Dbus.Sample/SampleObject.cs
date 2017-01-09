using System;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    [DbusProvide("org.dbuscore.sample.interface", Path = "/org/dbuscore/sample")]
    public class SampleObject
    {
        public Task MyVoidAsync()
        {
            Console.WriteLine("MyVoid called");
            return Task.FromResult(0);
        }

        public Task<string> MyEchoAsync(string message)
        {
            Console.WriteLine($"MyEcho called: {message}");
            return Task.FromResult(message);
        }

        public Task<Tuple<string, int>> MyComplexMethodAsync(string p1, int p2, int p3)
        {
            Console.WriteLine($"MyComplexMethod called: {p1} {p2} {p3}");
            return Task.FromResult(
                Tuple.Create(p1 + p2, p3)
            );
        }
    }
}
