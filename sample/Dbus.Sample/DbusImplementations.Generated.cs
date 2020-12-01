namespace Dbus.Sample
{

    public static partial class DbusImplementations
    {
        private static void initRegistrations()
        {
            global::Dbus.Connection.AddPublishProxy<global::Dbus.IOrgFreedesktopDbusObjectManagerProvide>((global::System.Func<global::Dbus.Connection, global::Dbus.IOrgFreedesktopDbusObjectManagerProvide, global::Dbus.ObjectPath, global::Dbus.IProxy>)global::Dbus.OrgFreedesktopDbusObjectManager_Proxy.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.IOrgFreedesktopDbusObjectManager>((global::System.Func<global::Dbus.Connection, global::Dbus.ObjectPath, string, global::System.Threading.CancellationToken, object>)IOrgFreedesktopDbusObjectManager_Implementation.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.Sample.IOrgFreedesktopUpower>((global::System.Func<global::Dbus.Connection, global::Dbus.ObjectPath, string, global::System.Threading.CancellationToken, object>)IOrgFreedesktopUpower_Implementation.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.Sample.IOrgMprisMediaPlayer2Player>((global::System.Func<global::Dbus.Connection, global::Dbus.ObjectPath, string, global::System.Threading.CancellationToken, object>)IOrgMprisMediaPlayer2Player_Implementation.Factory);
            global::Dbus.Connection.AddPublishProxy((global::System.Func<global::Dbus.Connection, global::Dbus.Sample.SampleObject, global::Dbus.ObjectPath, global::Dbus.IProxy>)SampleObject_Proxy.Factory);
        }

        static partial void DoInit() => initRegistrations();
    }

    public sealed class IOrgFreedesktopDbusObjectManager_Implementation : global::Dbus.IOrgFreedesktopDbusObjectManager
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        public override string ToString()
        {
            return "org.freedesktop.DBus.ObjectManager@" + this.path;
        }

        public void Dispose() => global::System.Threading.Tasks.Task.Run((global::System.Func<global::System.Threading.Tasks.ValueTask>)DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }

        private IOrgFreedesktopDbusObjectManager_Implementation(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? "";
            this.destination = destination ?? "";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "InterfacesAdded",
                (global::Dbus.Connection.SignalHandler)this.handleInterfacesAdded
            ));
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "InterfacesRemoved",
                (global::Dbus.Connection.SignalHandler)this.handleInterfacesRemoved
            ));
        }

        public static IOrgFreedesktopDbusObjectManager_Implementation Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new IOrgFreedesktopDbusObjectManager_Implementation(connection, path, destination, cancellationToken);
        }

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> decode_result_GetManagedObjectsAsync_v_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> decode_result_GetManagedObjectsAsync_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, decode_result_GetManagedObjectsAsync_v_v);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>>> decode_result_GetManagedObjectsAsync = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetObjectPath, decode_result_GetManagedObjectsAsync_v);

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>>> GetManagedObjectsAsync(global::System.Threading.CancellationToken cancellationToken)
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "GetManagedObjects",
                this.destination,
                null,
                "",
                cancellationToken
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("a{oa{sa{sv}}}");
                return decode_result_GetManagedObjectsAsync(decoder);
            }
        }

        public event global::System.Action<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> InterfacesAdded;

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> decode_decoded1_InterfacesAdded_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> decode_decoded1_InterfacesAdded = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, decode_decoded1_InterfacesAdded_v);

        private void handleInterfacesAdded(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("oa{sa{sv}}");
            var decoded0_InterfacesAdded = global::Dbus.Decoder.GetObjectPath(decoder);
            var decoded1_InterfacesAdded = decode_decoded1_InterfacesAdded(decoder);
            InterfacesAdded?.Invoke(decoded0_InterfacesAdded, decoded1_InterfacesAdded);
        }

        public event global::System.Action<global::Dbus.ObjectPath, global::System.Collections.Generic.List<global::System.String>> InterfacesRemoved;

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.List<global::System.String>> decode_decoded1_InterfacesRemoved = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetArray(decoder, global::Dbus.Decoder.GetString, false);

        private void handleInterfacesRemoved(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("oas");
            var decoded0_InterfacesRemoved = global::Dbus.Decoder.GetObjectPath(decoder);
            var decoded1_InterfacesRemoved = decode_decoded1_InterfacesRemoved(decoder);
            InterfacesRemoved?.Invoke(decoded0_InterfacesRemoved, decoded1_InterfacesRemoved);
        }

    }

    public sealed class IOrgFreedesktopUpower_Implementation : global::Dbus.Sample.IOrgFreedesktopUpower
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        public override string ToString()
        {
            return "org.freedesktop.UPower@" + this.path;
        }

        public void Dispose() => global::System.Threading.Tasks.Task.Run((global::System.Func<global::System.Threading.Tasks.ValueTask>)DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }

        private IOrgFreedesktopUpower_Implementation(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? "/org/freedesktop/UPower";
            this.destination = destination ?? "org.freedesktop.UPower";
        }

        public static IOrgFreedesktopUpower_Implementation Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new IOrgFreedesktopUpower_Implementation(connection, path, destination, cancellationToken);
        }

        private static void encode_GetAllAsync(global::Dbus.Encoder sendBody, global::System.String interfaceName)
        {
            sendBody.Add(interfaceName);
        }

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> decode_result_GetAllAsync = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> GetAllAsync(global::System.String interfaceName)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetAllAsync(sendBody, interfaceName);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.UPower",
                "GetAll",
                this.destination,
                sendBody,
                "s",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("a{sv}");
                return decode_result_GetAllAsync(decoder);
            }
        }

    }

    public sealed class IOrgMprisMediaPlayer2Player_Implementation : global::Dbus.Sample.IOrgMprisMediaPlayer2Player
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        public override string ToString()
        {
            return "org.mpris.MediaPlayer2.Player@" + this.path;
        }

        public void Dispose() => global::System.Threading.Tasks.Task.Run((global::System.Func<global::System.Threading.Tasks.ValueTask>)DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }

        private IOrgMprisMediaPlayer2Player_Implementation(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? "";
            this.destination = destination ?? "";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.Properties",
                "PropertiesChanged",
                (global::Dbus.Connection.SignalHandler)this.handleProperties
            ));
            PropertyInitializationFinished = initProperties(cancellationToken);
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Seeked",
                (global::Dbus.Connection.SignalHandler)this.handleSeeked
            ));
        }

        public static IOrgMprisMediaPlayer2Player_Implementation Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new IOrgMprisMediaPlayer2Player_Implementation(connection, path, destination, cancellationToken);
        }

        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public global::System.Threading.Tasks.Task PropertyInitializationFinished { get; }

        private void handleProperties(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("sa{sv}as");
            var interfaceName = global::Dbus.Decoder.GetString(decoder);
            if (interfaceName != "org.mpris.MediaPlayer2.Player")
                return;
            var changed = global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            applyProperties(changed);
        }

        private async global::System.Threading.Tasks.Task initProperties(global::System.Threading.CancellationToken cancellationToken)
        {
            var sendBody = new global::Dbus.Encoder();
            sendBody.Add("org.mpris.MediaPlayer2.Player");

            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "GetAll",
                this.destination,
                sendBody,
                "s",
                cancellationToken
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("a{sv}");
                var properties = global::Dbus.Decoder.GetDictionary(
                    decoder,
                    global::Dbus.Decoder.GetString,
                    global::Dbus.Decoder.GetObject
                );
                applyProperties(properties);
            }
        }

        private void applyProperties(global::System.Collections.Generic.Dictionary<string, object> changed)
        {
            foreach (var (name, value) in changed)
            {
                switch (name)
                {
                    case "PlaybackStatus":
                        PlaybackStatus = (global::System.String)value;
                        break;
                    case "LoopStatus":
                        LoopStatus = (global::System.String)value;
                        break;
                    case "Rate":
                        Rate = (global::System.Double)value;
                        break;
                    case "Shuffle":
                        Shuffle = (global::System.Boolean)value;
                        break;
                    case "Metadata":
                        Metadata = (global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>)value;
                        break;
                    case "Volume":
                        Volume = (global::System.Double)value;
                        break;
                    case "MinimumRate":
                        MinimumRate = (global::System.Double)value;
                        break;
                    case "MaximumRate":
                        MaximumRate = (global::System.Double)value;
                        break;
                    case "CanGoNext":
                        CanGoNext = (global::System.Boolean)value;
                        break;
                    case "CanGoPrevious":
                        CanGoPrevious = (global::System.Boolean)value;
                        break;
                    case "CanPlay":
                        CanPlay = (global::System.Boolean)value;
                        break;
                    case "CanPause":
                        CanPause = (global::System.Boolean)value;
                        break;
                    case "CanSeek":
                        CanSeek = (global::System.Boolean)value;
                        break;
                }
            }
            foreach (var key in changed.Keys)
                PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(key));
        }

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

        private static void encode_GetCanControlAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanControl");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanControlAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanControlAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetCanGoNextAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanGoNext");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanGoNextAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanGoNextAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetCanGoPreviousAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanGoPrevious");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanGoPreviousAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanGoPreviousAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetCanPauseAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanPause");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanPauseAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanPauseAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetCanPlayAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanPlay");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanPlayAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanPlayAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetCanSeekAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("CanSeek");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetCanSeekAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetCanSeekAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetLoopStatusAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("LoopStatus");
        }

        public async global::System.Threading.Tasks.Task<global::System.String> GetLoopStatusAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetLoopStatusAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.String)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetMaximumRateAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("MaximumRate");
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetMaximumRateAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetMaximumRateAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Double)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetMetadataAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Metadata");
        }

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>> GetMetadataAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetMetadataAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Collections.Generic.IDictionary<global::System.String, global::System.Object>)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetMinimumRateAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("MinimumRate");
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetMinimumRateAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetMinimumRateAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Double)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetPlaybackStatusAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("PlaybackStatus");
        }

        public async global::System.Threading.Tasks.Task<global::System.String> GetPlaybackStatusAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetPlaybackStatusAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.String)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetPositionAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Position");
        }

        public async global::System.Threading.Tasks.Task<global::System.Int64> GetPositionAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetPositionAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Int64)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetRateAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Rate");
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetRateAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetRateAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Double)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetShuffleAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Shuffle");
        }

        public async global::System.Threading.Tasks.Task<global::System.Boolean> GetShuffleAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetShuffleAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Boolean)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        private static void encode_GetVolumeAsync(global::Dbus.Encoder sendBody)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Volume");
        }

        public async global::System.Threading.Tasks.Task<global::System.Double> GetVolumeAsync()
        {
            var sendBody = new global::Dbus.Encoder();
            encode_GetVolumeAsync(sendBody);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Get",
                this.destination,
                sendBody,
                "ss",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("v");
                return (global::System.Double)global::Dbus.Decoder.GetObject(decoder);
            }
        }

        public async global::System.Threading.Tasks.Task NextAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Next",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_OperUriAsync(global::Dbus.Encoder sendBody, global::System.String uri)
        {
            sendBody.Add(uri);
        }

        public async global::System.Threading.Tasks.Task OperUriAsync(global::System.String uri)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_OperUriAsync(sendBody, uri);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "OperUri",
                this.destination,
                sendBody,
                "s",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public async global::System.Threading.Tasks.Task PauseAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Pause",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public async global::System.Threading.Tasks.Task PlayAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Play",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public async global::System.Threading.Tasks.Task PlayPauseAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "PlayPause",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public async global::System.Threading.Tasks.Task PreviousAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Previous",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SeekAsync(global::Dbus.Encoder sendBody, global::System.Int64 offsetInUsec)
        {
            sendBody.Add(offsetInUsec);
        }

        public async global::System.Threading.Tasks.Task SeekAsync(global::System.Int64 offsetInUsec)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SeekAsync(sendBody, offsetInUsec);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Seek",
                this.destination,
                sendBody,
                "x",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SetLoopStatusAsync(global::Dbus.Encoder sendBody, global::System.String status)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("LoopStatus");
            sendBody.Add((global::Dbus.Signature)"s");
            sendBody.Add(status);
        }

        public async global::System.Threading.Tasks.Task SetLoopStatusAsync(global::System.String status)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SetLoopStatusAsync(sendBody, status);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Set",
                this.destination,
                sendBody,
                "ssv",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SetPositionAsync(global::Dbus.Encoder sendBody, global::Dbus.ObjectPath track, global::System.Int64 startPosition)
        {
            sendBody.Add(track);
            sendBody.Add(startPosition);
        }

        public async global::System.Threading.Tasks.Task SetPositionAsync(global::Dbus.ObjectPath track, global::System.Int64 startPosition)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SetPositionAsync(sendBody, track, startPosition);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "SetPosition",
                this.destination,
                sendBody,
                "ox",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SetRateAsync(global::Dbus.Encoder sendBody, global::System.Double rate)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Rate");
            sendBody.Add((global::Dbus.Signature)"d");
            sendBody.Add(rate);
        }

        public async global::System.Threading.Tasks.Task SetRateAsync(global::System.Double rate)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SetRateAsync(sendBody, rate);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Set",
                this.destination,
                sendBody,
                "ssv",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SetShuffleAsync(global::Dbus.Encoder sendBody, global::System.Boolean shuffle)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Shuffle");
            sendBody.Add((global::Dbus.Signature)"b");
            sendBody.Add(shuffle);
        }

        public async global::System.Threading.Tasks.Task SetShuffleAsync(global::System.Boolean shuffle)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SetShuffleAsync(sendBody, shuffle);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Set",
                this.destination,
                sendBody,
                "ssv",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        private static void encode_SetVolumeAsync(global::Dbus.Encoder sendBody, global::System.Double volume)
        {
            sendBody.Add("org.mpris.MediaPlayer2.Player");
            sendBody.Add("Volume");
            sendBody.Add((global::Dbus.Signature)"d");
            sendBody.Add(volume);
        }

        public async global::System.Threading.Tasks.Task SetVolumeAsync(global::System.Double volume)
        {
            var sendBody = new global::Dbus.Encoder();
            encode_SetVolumeAsync(sendBody, volume);
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.Properties",
                "Set",
                this.destination,
                sendBody,
                "ssv",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public async global::System.Threading.Tasks.Task StopAsync()
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.mpris.MediaPlayer2.Player",
                "Stop",
                this.destination,
                null,
                "",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("");
                return;
            }
        }

        public event global::System.Action<global::System.Int64> Seeked;

        private void handleSeeked(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("x");
            var decoded0_Seeked = global::Dbus.Decoder.GetInt64(decoder);
            Seeked?.Invoke(decoded0_Seeked);
        }

    }

    public sealed class SampleObject_Proxy : global::Dbus.IProxy
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.Sample.SampleObject target;
        private readonly global::Dbus.ObjectPath path;
        private readonly global::System.IDisposable registration;

        private SampleObject_Proxy(global::Dbus.Connection connection, global::Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            this.path = path;
            InterfaceName = "org.dbuscore.sample.interface";
            registration = connection.RegisterObjectProxy(
                path ?? "/org/dbuscore/sample",
                InterfaceName,
                this
            );
        }

        public object Target => target;
        public string InterfaceName { get; }

        public override string ToString()
        {
            return this.InterfaceName + "@" + this.path;
        }

        public static SampleObject_Proxy Factory(global::Dbus.Connection connection, Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            return new SampleObject_Proxy(connection, target, path);
        }

        public void Dispose()
        {
            registration.Dispose();
        }

        public void EncodeProperties(global::Dbus.Encoder sendBody)
        {
            var state = sendBody.StartArray(storesCompoundValues: true);

            sendBody.FinishArray(state);
        }

        public void EncodeProperty(global::Dbus.Encoder sendBody, string propertyName)
        {
            switch (propertyName)
            {
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName("UnknownProperty"),
                        "No such Property: " + propertyName
                    );
            }
        }

        public void SetProperty(string propertyName, global::Dbus.Decoder decoder)
        {
            switch (propertyName)
            {
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName("UnknownProperty"),
                        "No such Property: " + propertyName
                    );
            }
        }

        private static void encode_MyComplexMethodAsync(global::Dbus.Encoder sendBody, global::System.Tuple<global::System.String, global::System.Int32> methodResult)
        {
            sendBody.StartCompoundValue();
            sendBody.Add(methodResult.Item1);
            sendBody.Add(methodResult.Item2);
        }

        private async global::System.Threading.Tasks.Task handleMyComplexMethodAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            decoder.AssertSignature("sii");
            var p1 = global::Dbus.Decoder.GetString(decoder);
            var p2 = global::Dbus.Decoder.GetInt32(decoder);
            var p3 = global::Dbus.Decoder.GetInt32(decoder);
            var methodResult = await target.MyComplexMethodAsync(p1, p2, p3);
            if (methodCallOptions.NoReplyExpected)
                return;
            var sendBody = new global::Dbus.Encoder();
            encode_MyComplexMethodAsync(sendBody, methodResult);
            await connection.SendMethodReturnAsync(methodCallOptions, sendBody, "(si)", cancellationToken).ConfigureAwait(false);
        }

        private static void encode_MyEchoAsync(global::Dbus.Encoder sendBody, global::System.String methodResult)
        {
            sendBody.Add(methodResult);
        }

        private async global::System.Threading.Tasks.Task handleMyEchoAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            decoder.AssertSignature("s");
            var message = global::Dbus.Decoder.GetString(decoder);
            var methodResult = await target.MyEchoAsync(message);
            if (methodCallOptions.NoReplyExpected)
                return;
            var sendBody = new global::Dbus.Encoder();
            encode_MyEchoAsync(sendBody, methodResult);
            await connection.SendMethodReturnAsync(methodCallOptions, sendBody, "s", cancellationToken).ConfigureAwait(false);
        }

        private async global::System.Threading.Tasks.Task handleMyVoidAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            decoder.AssertSignature("");
            await target.MyVoidAsync();
            if (methodCallOptions.NoReplyExpected)
                return;
            await connection.SendMethodReturnAsync(methodCallOptions, null, "", cancellationToken).ConfigureAwait(false);
        }

        public global::System.Threading.Tasks.Task HandleMethodCallAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            switch (methodCallOptions.Member)
            {
                case "MyComplexMethod":
                    return handleMyComplexMethodAsync(methodCallOptions, decoder, cancellationToken);
                case "MyEcho":
                    return handleMyEchoAsync(methodCallOptions, decoder, cancellationToken);
                case "MyVoid":
                    return handleMyVoidAsync(methodCallOptions, decoder, cancellationToken);
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

    }

}
