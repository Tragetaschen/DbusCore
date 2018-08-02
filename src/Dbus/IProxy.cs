using System;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IProxy : IDisposable
    {
        string InterfaceName { get; }
        void EncodeProperties(Encoder sendBody);
        Task HandleMethodCallAsync(uint replySerial, MessageHeader header, Decoder body, bool shouldSendReply);
        void EncodeProperty(Encoder sendBody, string propertyName);
    }
}
