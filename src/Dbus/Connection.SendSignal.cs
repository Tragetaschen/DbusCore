using System;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        public async Task SendSignalAsync(
            ObjectPath path,
            string interfaceName,
            string methodName,
            Encoder body,
            Signature signature
            )
        {
            var bodySegments = await body.CompleteWritingAsync().ConfigureAwait(false);

            if (path.ToString() == "")
                throw new ArgumentException("Signal path must not be empty", nameof(path));
            if (interfaceName == "")
                throw new ArgumentException("Signal interface must not be empty", nameof(interfaceName));
            if (methodName == "")
                throw new ArgumentException("Signal member must not be empty", nameof(methodName));

            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = new Encoder();
            header.Add((byte)dbusEndianess.LittleEndian);
            header.Add((byte)dbusMessageType.Signal);
            header.Add((byte)dbusFlags.None);
            header.Add((byte)dbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(getSerial());

            header.AddArray(() =>
            {
                addHeader(header, path);
                addHeader(header, 2, interfaceName);
                addHeader(header, 3, methodName);
                if (bodyLength > 0)
                    addHeader(header, signature);
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }
    }
}
