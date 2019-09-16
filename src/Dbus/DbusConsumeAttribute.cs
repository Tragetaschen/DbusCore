using System;

namespace Dbus
{
    /// <summary>
    /// Marks interfaces for code generation to generate the appropriate implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class DbusConsumeAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="interfaceName">Name of the interface</param>
        public DbusConsumeAttribute(string interfaceName) => InterfaceName = interfaceName;

        /// <summary>
        /// Name of the interface
        /// </summary>
        public string InterfaceName { get; }
        /// <summary>
        /// Standard path if not overridden
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// Standard destination if not overridden
        /// </summary>
        public string? Destination { get; set; }
    }
}
