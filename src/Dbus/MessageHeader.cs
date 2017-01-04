using System;
using System.Text;

namespace Dbus
{
    public class MessageHeader
    {
        public MessageHeader(byte[] headerBytes)
        {
            var index = 0;
            while (index < headerBytes.Length)
            {
                var headerFieldTypeCode = headerBytes[index];
                index += 4;
                switch (headerFieldTypeCode)
                {
                    case 8: /* SIGNATURE: SIGNATURE */
                        BodySignature = Decoder.GetSignature(headerBytes, ref index);
                        break;
                    case 5: /* REPLY_SERIAL: UINT32 */
                        ReplySerial = Decoder.GetInt32(headerBytes, ref index);
                        break;
                    case 9: /* UNIX_FDS: UINT32 */
                        Decoder.GetInt32(headerBytes, ref index);
                        break;
                    default:
                        var value = Decoder.GetString(headerBytes, ref index);
                        switch (headerFieldTypeCode)
                        {
                            case 1: /* PATH: OBJECT_PATH */
                                Path = value;
                                break;
                            case 2: /* INTERFACE: STRING */
                                InterfaceName = value;
                                break;
                            case 3: /* MEMBER: STRING */
                                Member = value;
                                break;
                            case 4: /* ERROR_NAME: STRING */
                                ErrorName = value;
                                break;
                            case 6: /* DESTINATION: STRING */
                                Destination = value;
                                break;
                            case 7: /* SENDER: STRING */
                                Sender = value;
                                break;
                        }
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
