namespace Dbus
{
    public class MessageHeader
    {
        public MessageHeader(byte[] headerBytes)
        {
            BodySignature = "";
            var index = 0;
            while (index < headerBytes.Length)
            {
                var typeCode = Decoder.GetByte(headerBytes, ref index);
                var value = Decoder.GetObject(headerBytes, ref index);
                switch (typeCode)
                {
                    case 1:
                        Path = (ObjectPath)value;
                        break;
                    case 2:
                        InterfaceName = (string)value;
                        break;
                    case 3:
                        Member = (string)value;
                        break;
                    case 4:
                        ErrorName = (string)value;
                        break;
                    case 5:
                        ReplySerial = (uint)value;
                        break;
                    case 6:
                        Destination = (string)value;
                        break;
                    case 7:
                        Sender = (string)value;
                        break;
                    case 8:
                        BodySignature = (Signature)value;
                        break;
                    case 9: /* UNIX_FDS: UINT32 */
                        break;
                }
                Alignment.Advance(ref index, 8);
            }
        }

        public ObjectPath Path { get; }
        public string InterfaceName { get; }
        public string Member { get; }
        public string ErrorName { get; }
        public uint ReplySerial { get; }
        public string Destination { get; }
        public string Sender { get; }
        public Signature BodySignature { get; }
        //public uint UnixFds { get; }

        public override string ToString()
        {
            return $"P: {Path}, I: {InterfaceName}, M: {Member}, E: {ErrorName}, R: {ReplySerial}, D: {Destination}, S: {Sender}, B: {BodySignature}";
        }
    }
}
