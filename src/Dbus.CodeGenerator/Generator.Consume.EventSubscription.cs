using System;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consumeEventSubscription(EventInfo eventInfo, string interfaceName)
        {
            if (eventInfo.EventHandlerType == null)
                throw new InvalidOperationException("EventInfo has no handler type");

            return new StringBuilder()
                .Append(@"
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                """)
                .Append(interfaceName)
                .Append(@""",
                """)
                .Append(eventInfo.Name)
                .Append(@""",
                this.handle")
                .Append(eventInfo.Name)
                .Append(@"
            ));")
            ;
        }
    }
}
