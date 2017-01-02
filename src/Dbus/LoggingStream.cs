using System;
using System.IO;

namespace Dbus
{
    class LoggingStream : Stream
    {
        private readonly Stream baseStream;

        public LoggingStream(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => baseStream.CanSeek;
        public override bool CanWrite => baseStream.CanWrite;
        public override long Length => baseStream.Length;
        public override long Position
        {
            get { return baseStream.Position; }
            set { baseStream.Position = value; }
        }

        public override void Flush()
        {
            baseStream.Flush();
            Console.WriteLine("Flush");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Console.Write($"r[{offset} {count}] ");
            var result = baseStream.Read(buffer, offset, count);
            if (result >= 0)
                dump(buffer, offset, result);
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Console.Write($"w[{offset} {count}] ");
            dump(buffer, offset, count);
            baseStream.Write(buffer, offset, count);
        }

        private void dump(byte[] buffer, int offset, int length)
        {
            for (var i = offset; i < length; ++i)
                if (char.IsLetterOrDigit((char)buffer[i]))
                    Console.Write($"{(char)buffer[i]} ");
                else
                    Console.Write($"x{buffer[i]:X} ");
            Console.WriteLine();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                baseStream.Dispose();
        }
    }
}
