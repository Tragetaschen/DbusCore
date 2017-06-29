using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IProxy : IDisposable
    {
        string InterfaceName { get; }
        void EncodeProperties(List<byte> sendBody, ref int index);
        Task HandleMethodCallAsync(uint replySerial, MessageHeader header, byte[] body, bool shouldSendReply);
        void EncodeProperty(List<byte> sendBody, ref int index, string propertyName);
    }
}
