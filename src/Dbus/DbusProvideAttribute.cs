using System;

namespace Dbus;

/// <summary>
/// Marks interfaces for code generation to generate the appropriate implementation
/// </summary>
/// <param name="interfaceName">Name of the interface</param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
public class DbusProvideAttribute(string interfaceName) : Attribute
{
    /// <summary>
    /// Name of the interface
    /// </summary>
    public string InterfaceName { get; } = interfaceName;
    /// <summary>
    /// Default path
    /// </summary>
    public string? Path { get; set; }
}
