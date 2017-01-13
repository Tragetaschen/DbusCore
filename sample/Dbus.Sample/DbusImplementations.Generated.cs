namespace Dbus.Sample
{

    public static partial class DbusImplementations
    {
        static partial void DoInit()
        {
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.IOrgFreedesktopDbus>(OrgFreedesktopDbus.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.Sample.IOrgFreedesktopUpower>(OrgFreedesktopUpower.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.Sample.IOrgMprisMediaPlayer2Player>(OrgMprisMediaPlayer2Player.Factory);
            global::Dbus.Connection.AddPublishProxy<global::Dbus.Sample.SampleObject>(SampleObject_Proxy.Factory);
        }
    }

    public sealed class OrgFreedesktopDbus : global::Dbus.IOrgFreedesktopDbus
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private OrgFreedesktopDbus(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? "/org/freedesktop/DBus";
            this.destination = destination ?? "org.freedesktop.DBus";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus",
                "NameAcquired",
                handleNameAcquired
            ));

        }

        public static global::Dbus.IOrgFreedesktopDbus Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new OrgFreedesktopDbus(connection, path, destination);
        }


        public async global::System.Threading.Tasks.Task AddMatchAsync(global::System.String match)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, match);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "AddMatch",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task<global::System.String> HelloAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "Hello",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "s");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetString(receivedMessage.Body, ref decoderIndex);
            return result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::System.String>> ListNamesAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "ListNames",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "as");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetArray(receivedMessage.Body, ref decoderIndex, global::Dbus.Decoder.GetString);
            return result;
        }

        public async global::System.Threading.Tasks.Task RemoveMatchAsync(global::System.String match)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, match);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "RemoveMatch",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task<global::System.UInt32> RequestNameAsync(global::System.String name, global::System.UInt32 flags)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, name);
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, flags);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "RequestName",
                destination,
                sendBody,
                "su"
            );
            assertSignature(receivedMessage.Signature, "u");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetUInt32(receivedMessage.Body, ref decoderIndex);
            return result;
        }

        public event global::System.Action<global::System.String> NameAcquired;
        private void handleNameAcquired(global::Dbus.MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "s");
            var decoderIndex = 0;
            var decoded = global::Dbus.Decoder.GetString(body, ref decoderIndex);
            NameAcquired?.Invoke(decoded);
        }

        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got '{actual}', but expected '{expected}'");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class OrgFreedesktopUpower : global::Dbus.Sample.IOrgFreedesktopUpower
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private OrgFreedesktopUpower(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? "/org/freedesktop/UPower";
            this.destination = destination ?? "org.freedesktop.UPower";

        }

        public static global::Dbus.Sample.IOrgFreedesktopUpower Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new OrgFreedesktopUpower(connection, path, destination);
        }


        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>> GetAllAsync(global::System.String interfaceName)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, interfaceName);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.UPower",
                "GetAll",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "a{sv}");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetDictionary(receivedMessage.Body, ref decoderIndex, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            return result;
        }


        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got '{actual}', but expected '{expected}'");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class OrgMprisMediaPlayer2Player : global::Dbus.Sample.IOrgMprisMediaPlayer2Player
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private OrgMprisMediaPlayer2Player(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? "";
            this.destination = destination ?? "";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Seeked",
                handleSeeked
            ));

            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.Properties",
                "PropertiesChanged",
                handleProperties
            ));
            PropertyInitializationFinished = global::System.Threading.Tasks.Task.Run(initProperties);

        }

        public static global::Dbus.Sample.IOrgMprisMediaPlayer2Player Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new OrgMprisMediaPlayer2Player(connection, path, destination);
        }


        private void handleProperties(global::Dbus.MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "sa{sv}as");
            var index = 0;
            var interfaceName = global::Dbus.Decoder.GetString(body, ref index);
            var changed = global::Dbus.Decoder.GetDictionary(body, ref index, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            //var invalidated = global::Dbus.Decoder.GetArray(body, ref index, global::Dbus.Decoder.GetString);
            applyProperties(changed);
        }

        private async global::System.Threading.Tasks.Task initProperties()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "GetAll",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "a{sv}");
            var index = 0;
            var properties = global::Dbus.Decoder.GetDictionary(receivedMessage.Body, ref index, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            applyProperties(properties);
        }

        private void applyProperties(global::System.Collections.Generic.IDictionary<string, object> changed)
        {
            foreach (var entry in changed)
            {
                switch (entry.Key)
                {
                    case "PlaybackStatus":
                        PlaybackStatus = (global::System.String)entry.Value;
                        break;
                    case "LoopStatus":
                        LoopStatus = (global::System.String)entry.Value;
                        break;
                    case "Rate":
                        Rate = (global::System.Double)entry.Value;
                        break;
                    case "Shuffle":
                        Shuffle = (global::System.Boolean)entry.Value;
                        break;
                    case "Metadata":
                        Metadata = (global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>)entry.Value;
                        break;
                    case "Volume":
                        Volume = (global::System.Double)entry.Value;
                        break;
                    case "MinimumRate":
                        MinimumRate = (global::System.Double)entry.Value;
                        break;
                    case "MaximumRate":
                        MaximumRate = (global::System.Double)entry.Value;
                        break;
                    case "CanGoNext":
                        CanGoNext = (global::System.Boolean)entry.Value;
                        break;
                    case "CanGoPrevious":
                        CanGoPrevious = (global::System.Boolean)entry.Value;
                        break;
                    case "CanPlay":
                        CanPlay = (global::System.Boolean)entry.Value;
                        break;
                    case "CanPause":
                        CanPause = (global::System.Boolean)entry.Value;
                        break;
                    case "CanSeek":
                        CanSeek = (global::System.Boolean)entry.Value;
                        break;
                }
                PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(entry.Key));
            }
        }

        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public global::System.Threading.Tasks.Task PropertyInitializationFinished { get; }

        public global::System.String PlaybackStatus { get; private set; }
        public global::System.String LoopStatus { get; private set; }
        public global::System.Double Rate { get; private set; }
        public global::System.Boolean Shuffle { get; private set; }
        public global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object> Metadata { get; private set; }
        public global::System.Double Volume { get; private set; }
        public global::System.Double MinimumRate { get; private set; }
        public global::System.Double MaximumRate { get; private set; }
        public global::System.Boolean CanGoNext { get; private set; }
        public global::System.Boolean CanGoPrevious { get; private set; }
        public global::System.Boolean CanPlay { get; private set; }
        public global::System.Boolean CanPause { get; private set; }
        public global::System.Boolean CanSeek { get; private set; }
        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanControlAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanControl");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanGoNextAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanGoNext");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanGoPreviousAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanGoPrevious");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanPauseAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanPause");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanPlayAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanPlay");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanSeekAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "CanSeek");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.String> GetLoopStatusAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "LoopStatus");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.String)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetMaximumRateAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "MaximumRate");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Double)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>> GetMetadataAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Metadata");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetMinimumRateAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "MinimumRate");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Double)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.String> GetPlaybackStatusAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "PlaybackStatus");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.String)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Int64> GetPositionAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Position");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Int64)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetRateAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Rate");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Double)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetShuffleAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Shuffle");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Boolean)result;
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetVolumeAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Volume");

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Get",
                destination,
                sendBody,
                "ss"
            );
            assertSignature(receivedMessage.Signature, "v");
            var decoderIndex = 0;
            var result = global::Dbus.Decoder.GetObject(receivedMessage.Body, ref decoderIndex);
            return (global::System.Double)result;
        }

        public async global::System.Threading.Tasks.Task NextAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Next",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task OperUriAsync(global::System.String uri)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, uri);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "OperUri",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task PauseAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Pause",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task PlayAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Play",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task PlayPauseAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "PlayPause",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task PreviousAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Previous",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SeekAsync(global::System.Int64 offsetInUsec)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, offsetInUsec);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Seek",
                destination,
                sendBody,
                "x"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SetLoopStatusAsync(global::System.String status)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "LoopStatus");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, status);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Set",
                destination,
                sendBody,
                "sss"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SetPositionAsync(global::Dbus.ObjectPath track, global::System.Int64 startPosition)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, track);
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, startPosition);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "SetPosition",
                destination,
                sendBody,
                "ox"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SetRateAsync(global::System.Double rate)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Rate");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, rate);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Set",
                destination,
                sendBody,
                "ssd"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SetShuffleAsync(global::System.Boolean shuffle)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Shuffle");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, shuffle);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Set",
                destination,
                sendBody,
                "ssb"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task SetVolumeAsync(global::System.Double volume)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "org.mpris.MediaPlayer2.Player");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, "Volume");
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, volume);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus.Properties",
                "Set",
                destination,
                sendBody,
                "ssd"
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public async global::System.Threading.Tasks.Task StopAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.mpris.MediaPlayer2.Player",
                "Stop",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "");
            return;
        }

        public event global::System.Action<global::System.Int64> Seeked;
        private void handleSeeked(global::Dbus.MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "x");
            var decoderIndex = 0;
            var decoded = global::Dbus.Decoder.GetInt64(body, ref decoderIndex);
            Seeked?.Invoke(decoded);
        }

        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got '{actual}', but expected '{expected}'");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class SampleObject_Proxy : global::System.IDisposable
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.Sample.SampleObject target;

        private global::System.IDisposable registration;

        private SampleObject_Proxy(global::Dbus.Connection connection, global::Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                path ?? "/org/dbuscore/sample",
                "org.dbuscore.sample.interface",
                handleMethodCall
            );
        }

        public static SampleObject_Proxy Factory(global::Dbus.Connection connection, Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            return new SampleObject_Proxy(connection, target, path);
        }

        private System.Threading.Tasks.Task handleMethodCall(uint replySerial, global::Dbus.MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                case "MyComplexMethod":
                    return handleMyComplexMethodAsync(replySerial, header, body);
                case "MyEcho":
                    return handleMyEchoAsync(replySerial, header, body);
                case "MyVoid":
                    return handleMyVoidAsync(replySerial, header, body);
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async System.Threading.Tasks.Task handleMyComplexMethodAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "sii");
            var decoderIndex = 0;
            var p1 = global::Dbus.Decoder.GetString(receivedBody, ref decoderIndex);
            var p2 = global::Dbus.Decoder.GetInt32(receivedBody, ref decoderIndex);
            var p3 = global::Dbus.Decoder.GetInt32(receivedBody, ref decoderIndex);
            var result = await target.MyComplexMethodAsync(p1, p2, p3);
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item1);
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item2);
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, "si");
        }

        private async System.Threading.Tasks.Task handleMyEchoAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "s");
            var decoderIndex = 0;
            var message = global::Dbus.Decoder.GetString(receivedBody, ref decoderIndex);
            var result = await target.MyEchoAsync(message);
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result);
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, "s");
        }

        private async System.Threading.Tasks.Task handleMyVoidAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "");
            await target.MyVoidAsync();
            var sendBody = global::Dbus.Encoder.StartNew();
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, "");
        }


        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new global::Dbus.DbusException(
                    global::Dbus.DbusException.CreateErrorName("InvalidSignature"),
                    "Invalid signature"
                );
        }

        public void Dispose()
        {
            registration.Dispose();
        }
    }

}
