using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public class Encoder
    {
        public delegate void ElementWriter<T>(T value);
        public delegate void ElementWriter();

        private readonly Pipe pipe = new Pipe(new PipeOptions(
            // There's no reader until the entire message is built in memory.
            // That means we cannot tolerate a pause for the writer to wait for the non-existant reader
            pauseWriterThreshold: long.MaxValue
        ));
        private int index;

        private Span<byte> reserve(int size)
        {
            var result = pipe.Writer.GetSpan(size);
            pipe.Writer.Advance(size);
            index += size;
            return result;
        }

        public async Task<ReadOnlySequence<byte>> CompleteWritingAsync()
        {
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();
            var readResult = await pipe.Reader.ReadAsync();
            return readResult.Buffer;
        }

        public void Add(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Add(bytes.Length); // Actually uint
            var span = reserve(bytes.Length + 1);
            bytes.CopyTo(span);
            span[bytes.Length] = 0;
        }

        public void Add(Signature signature)
        {
            var bytes = Encoding.UTF8.GetBytes(signature.ToString());
            Add((byte)bytes.Length);
            var span = reserve(bytes.Length + 1);
            bytes.CopyTo(span);
            span[bytes.Length] = 0;
        }

        public void Add(ObjectPath value)
            => Add(value.ToString());

        private void addPrimitive<T>(T value, int size) where T : struct
        {
            EnsureAlignment(size);
            var span = MemoryMarshal.Cast<byte, T>(reserve(size));
            span[0] = value;
        }

        public void Add(short value)
            => addPrimitive(value, sizeof(short));

        public void Add(ushort value)
            => addPrimitive(value, sizeof(ushort));

        public void Add(int value)
            => addPrimitive(value, sizeof(int));

        public void Add(uint value)
            => addPrimitive(value, sizeof(uint));

        public void Add(ulong value)
            => addPrimitive(value, sizeof(ulong));

        public void Add(long value)
            => addPrimitive(value, sizeof(long));

        public void Add(double value)
            => addPrimitive(value, sizeof(double));

        public void Add(byte value)
            => addPrimitive(value, sizeof(byte));

        public void Add(bool value)
            => addPrimitive(value ? 1 : 0, sizeof(int));

        public void Add<T>(IEnumerable<T> values, ElementWriter<T> writer)
            => AddArray(() =>
            {
                foreach (var value in values)
                    writer(value);
            }, false);

        public void Add<TKey, TValue>(IDictionary<TKey, TValue> values, ElementWriter<TKey> keyWriter, ElementWriter<TValue> valueWriter)
            => AddArray(() =>
            {
                foreach (var value in values)
                {
                    EnsureAlignment(8);
                    keyWriter(value.Key);
                    valueWriter(value.Value);
                }
            }, true);

        public void AddArray(ElementWriter writer, bool alignment_8 = false)
        {
            EnsureAlignment(4);
            var lengthSpan = MemoryMarshal.Cast<byte, int>(reserve(4));
            EnsureAlignment(alignment_8 ? 8 : 4);
            var arrayStart = index;

            writer();

            var arrayLength = index - arrayStart;
            lengthSpan[0] = arrayLength;
        }

        public void AddVariant(string value)
        {
            Add((Signature)"s");
            Add(value);
        }

        public void AddVariant(ObjectPath value)
        {
            Add((Signature)"o");
            Add(value.ToString());
        }

        public void AddVariant(uint value)
        {
            Add((Signature)"u");
            Add(value);
        }

        public void AddVariant(Signature signature)
        {
            Add((Signature)"g");
            Add(signature);
        }

        public void AddVariant(bool value)
        {
            Add((Signature)"b");
            Add(value);
        }

        public void AddVariant(ushort value)
        {
            Add((Signature)"q");
            Add(value);
        }

        public void AddVariant(IEnumerable<string> value)
        {
            Add((Signature)"as");
            Add(value, Add);
        }

        public void AddVariant(object value)
        {
            switch (value)
            {
                case bool v:
                    AddVariant(v);
                    break;
                case string v:
                    AddVariant(v);
                    break;
                case ObjectPath v:
                    AddVariant(v);
                    break;
                case uint v:
                    AddVariant(v);
                    break;
                case Signature v:
                    AddVariant(v);
                    break;
                case ushort v:
                    AddVariant(v);
                    break;
                case IEnumerable<string> v:
                    AddVariant(v);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Type for Variant");
            }
        }

        public void AddVariant(IDictionary<string, object> value)
        {
            Add((Signature)"a{sv}");
            Add(value, Add, AddVariant);
        }

        public void EnsureAlignment(int alignment)
        {
            var bytesToAdd = Alignment.Calculate(index, alignment);
            var span = reserve(bytesToAdd);
            span.Clear();
        }

        public void CompleteReading(ReadOnlySequence<byte> buffer)
            => pipe.Reader.AdvanceTo(buffer.End);
    }
}
