using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
            // Called e.g. from StreamWriter's Dispose
            baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Don't do synchronous reads");
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
            throw new InvalidOperationException("Don't do synchronous writes");
        }

        public override void WriteByte(byte value)
        {
            throw new InvalidOperationException("Don't synchronously write bytes");
        }

        private void dump(byte[] buffer, int offset, int length)
        {
            for (var i = offset; i < length; ++i)
            {
                var isDigitOrAsciiLetter = false;
                isDigitOrAsciiLetter |= 48 <= buffer[i] && buffer[i] <= 57;
                isDigitOrAsciiLetter |= 65 <= buffer[i] && buffer[i] <= 90;
                isDigitOrAsciiLetter |= 97 <= buffer[i] && buffer[i] <= 122;
                if (isDigitOrAsciiLetter)
                    Console.Write($"{(char)buffer[i]} ");
                else
                    Console.Write($"x{buffer[i]:X} ");
        }
            Console.WriteLine();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Console.Write($"ra[{offset} {count}] ");
            var result = await baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (result >= 0)
                dump(buffer, offset, result);
            return result;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Console.Write($"wa[{offset} {count}] ");
            dump(buffer, offset, count);
            return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"fa");
            return baseStream.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                baseStream.Dispose();
        }
    }
}
