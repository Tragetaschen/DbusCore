using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        public Task SendSignalAsync(
            ObjectPath path,
            string interfaceName,
            string methodName,
            List<byte> body,
            Signature signature
            )
        {
            if (path.ToString() == "")
                throw new ArgumentException("Signal path must not be empty", nameof(path));
            if (interfaceName == "")
                throw new ArgumentException("Signal interface must not be empty", nameof(interfaceName));
            if (methodName == "")
                throw new ArgumentException("Signal member must not be empty", nameof(methodName));

            var serial = getSerial();
            var messageHeader = Encoder.StartNew();
            var index = 0;
            Encoder.Add(messageHeader, ref index, (byte)dbusEndianess.LittleEndian);
            Encoder.Add(messageHeader, ref index, (byte)dbusMessageType.Signal);
            Encoder.Add(messageHeader, ref index, (byte)dbusFlags.None);
            Encoder.Add(messageHeader, ref index, (byte)dbusProtocolVersion.Default);
            Encoder.Add(messageHeader, ref index, body.Count); // Actually uint
            Encoder.Add(messageHeader, ref index, serial);

            Encoder.AddArray(messageHeader, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                addHeader(buffer, ref localIndex, path);
                addHeader(buffer, ref localIndex, 2, interfaceName);
                addHeader(buffer, ref localIndex, 3, methodName);
                if (body.Count > 0)
                    addHeader(buffer, ref localIndex, signature);
            });
            Encoder.EnsureAlignment(messageHeader, ref index, 8);

            return serializedWriteToStream(messageHeader.ToArray(), body.ToArray());
        }
    }
}
