using System;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IProxy : IDisposable
    {
        string InterfaceName { get; }
        void EncodeProperties(Encoder sendBody);
        Task HandleMethodCallAsync(MethodCallOptions methodCallOptions, ReceivedMessage message);
        void EncodeProperty(Encoder sendBody, string propertyName);
    }
}
