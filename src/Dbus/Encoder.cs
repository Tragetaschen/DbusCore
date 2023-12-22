using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public class Encoder
{
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

    public async Task Dump(CancellationToken cancellationToken)
    {
        await pipe.Writer.FlushAsync(cancellationToken);
        pipe.Writer.Complete();
        var readResult = await pipe.Reader.ReadAsync(cancellationToken);
        readResult.Buffer.Dump();
    }

    public async Task<ReadOnlySequence<byte>> CompleteWritingAsync(CancellationToken cancellationToken)
    {
        await pipe.Writer.FlushAsync(cancellationToken);
        pipe.Writer.Complete();
        var readResult = await pipe.Reader.ReadAsync(cancellationToken);
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
        ensureAlignment(size);
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

    public void Add(long value)
        => addPrimitive(value, sizeof(long));

    public void Add(ulong value)
        => addPrimitive(value, sizeof(ulong));

    public void Add(double value)
        => addPrimitive(value, sizeof(double));

    public void Add(byte value)
    {
        var span = reserve(1);
        span[0] = value;
    }

    public void Add(bool value)
        => addPrimitive(value ? 1 : 0, sizeof(int));

    public EncoderArrayState StartArray(bool storesCompoundValues)
    {
        ensureAlignment(4);
        var lengthSpan = reserve(4);
        if (storesCompoundValues)
            StartCompoundValue();
        var arrayStart = index;

        return new EncoderArrayState(arrayStart, lengthSpan);
    }

    public void FinishArray(EncoderArrayState encoderArrayState)
    {
        var arrayLength = index - encoderArrayState.ArrayStart;
        var lengthSpan = MemoryMarshal.Cast<byte, int>(encoderArrayState.LengthSpan);
        lengthSpan[0] = arrayLength;
    }

    public void Add(object value)
    {
        switch (value)
        {
            case string v:
                Add((Signature)"s");
                Add(v);
                break;
            case Signature v:
                Add((Signature)"g");
                Add(v);
                break;
            case ObjectPath v:
                Add((Signature)"o");
                Add(v);
                break;
            case short v:
                Add((Signature)"n");
                Add(v);
                break;
            case ushort v:
                Add((Signature)"q");
                Add(v);
                break;
            case int v:
                Add((Signature)"i");
                Add(v);
                break;
            case uint v:
                Add((Signature)"u");
                Add(v);
                break;
            case long v:
                Add((Signature)"x");
                Add(v);
                break;
            case ulong v:
                Add((Signature)"t");
                Add(v);
                break;
            case double v:
                Add((Signature)"d");
                Add(v);
                break;
            case byte v:
                Add((Signature)"y");
                Add(v);
                break;
            case bool v:
                Add((Signature)"b");
                Add(v);
                break;
            case List<string> v:
                Add((Signature)"as");
                var state = StartArray(storesCompoundValues: false);
                foreach (var element in v)
                    Add(element);
                FinishArray(state);
                break;
            default:
                throw new InvalidOperationException("Unsupported Type for Variant");
        }
    }

    public void FinishHeader() => ensureAlignment(8);
    public void StartCompoundValue() => ensureAlignment(8);

    private void ensureAlignment(int alignment)
    {
        var bytesToAdd = Alignment.Calculate(index, alignment);
        var span = reserve(bytesToAdd);
        span.Clear();
    }

    public void CompleteReading(ReadOnlySequence<byte> buffer)
        => pipe.Reader.AdvanceTo(buffer.End);
}
