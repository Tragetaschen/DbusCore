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

            var header = createHeader(
                DbusMessageType.Signal,
                DbusMessageFlags.None,
                bodyLength,
                e =>
                {
                    addHeader(e, path);
                    addHeader(e, DbusHeaderType.InterfaceName, interfaceName);
                    addHeader(e, DbusHeaderType.Member, methodName);
                    if (bodyLength > 0)
                        addHeader(e, signature);
                }
            );

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }
    }
}
