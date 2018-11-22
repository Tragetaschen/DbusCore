using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IProxy : IDisposable
    {
        string InterfaceName { get; }
        void EncodeProperties(Encoder sendBody);
        Task HandleMethodCallAsync(MethodCallOptions methodCallOptions, ReceivedMessage message, CancellationToken cancellationToken);
        void EncodeProperty(Encoder sendBody, string propertyName);
    }
}
