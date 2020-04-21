using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consumeSkeleton(string interfaceName)
            => new StringBuilder()
                .Append(@"
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        public override string ToString()
        {
            return """)
                .Append(interfaceName)
                .AppendLine(@"@"" + this.path;
        }

        public void Dispose() => global::System.Threading.Tasks.Task.Run(DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }");
    }
}
