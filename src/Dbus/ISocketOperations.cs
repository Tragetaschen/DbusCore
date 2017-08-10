using System;

namespace Dbus
{
    public interface ISocketOperations : IDisposable
    {
        int Uid { get; }

        string ReadLine();
        void WriteLine(string contents);

        unsafe bool ReceiveMessage(byte* header, int headerLength, byte* body, int bodyLength, byte* fixedLengthHeader, int fixedLengthHeaderLength, int* control, int controlLength);
        unsafe void ReceiveMessage(byte* fixedLengthHeader, int fixedLengthHeaderLength, int* control, int controlLength);
        void Send(byte[] messageArray);

        void Shutdown();
    }
}
