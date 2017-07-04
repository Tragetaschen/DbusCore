using System;

namespace Dbus
{
    /// <summary>
    /// Marks interfaces for code generation to generate the appropriate implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
    public class DbusProvideAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="interfaceName">Name of the interface</param>
        public DbusProvideAttribute(string interfaceName) => InterfaceName = interfaceName;

        /// <summary>
        /// Name of the interface
        /// </summary>
        public string InterfaceName { get; }
        /// <summary>
        /// Default path
        /// </summary>
        public string Path { get; set; }
    }
}
