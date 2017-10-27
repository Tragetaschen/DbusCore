using System;
using System.Runtime.InteropServices;

namespace Dbus
{
    public interface ISocketOperations : IDisposable
    {
        int Uid { get; }

        string ReadLine();
        int Read(SafeHandle sockfd, byte[] buffer, int offset, int count);
        void WriteLine(string contents);

        unsafe bool ReceiveMessage(byte* header, int headerLength, byte* body, int bodyLength, byte* fixedLengthHeader, int fixedLengthHeaderLength, int* control, int controlLength);
        unsafe void ReceiveMessage(byte* fixedLengthHeader, int fixedLengthHeaderLength, int* control, int controlLength);
        void Send(byte[] messageArray);
        int Send(SafeHandle sockfd, byte[] buffer, int offset, int count);

        void SetNonblocking(SafeHandle sockfd);

        void Shutdown();
    }
}
