namespace Dbus
{
    public class MessageHeader
    {
        public MessageHeader(byte[] headerBytes)
        {
            var index = 0;
            while (index < headerBytes.Length)
            {
                var typeCode = Decoder.GetByte(headerBytes, ref index);
                var value = Decoder.GetVariant(headerBytes, ref index);
                switch (typeCode)
                {
                    case 1:
                        Path = (string)value;
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
                        ReplySerial = (int)value;
                        break;
                    case 6:
                        Destination = (string)value;
                        break;
                    case 7:
                        Sender = (string)value;
                        break;
                    case 8:
                        BodySignature = (string)value;
                        break;
                    case 9: /* UNIX_FDS: UINT32 */
                        break;
                }
                Alignment.Advance(ref index, 8);
            }
        }

        public string Path { get; }
        public string InterfaceName { get; }
        public string Member { get; }
        public string ErrorName { get; }
        public int ReplySerial { get; }
        public string Destination { get; }
        public string Sender { get; }
        public string BodySignature { get; }
        //public int UnixFds { get; }

        public override string ToString()
        {
            return $"P: {Path}, I: {InterfaceName}, M: {Member}, E: {ErrorName}, R: {ReplySerial}, D: {Destination}, S: {Sender}, B: {BodySignature}";
        }
    }
}
