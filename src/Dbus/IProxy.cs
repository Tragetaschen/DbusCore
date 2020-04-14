using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IProxy : IDisposable
    {
        object Target { get; }
        string InterfaceName { get; }
        void EncodeProperties(Encoder sendBody);
        Task HandleMethodCallAsync(MethodCallOptions methodCallOptions, Decoder decoder, CancellationToken cancellationToken);
        void EncodeProperty(Encoder sendBody, string propertyName);
    }
}
