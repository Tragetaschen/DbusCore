using System;

namespace Dbus;

/// <summary>
/// Marks interfaces for code generation to generate the appropriate implementation
/// </summary>
/// <param name="interfaceName">Name of the interface</param>
[AttributeUsage(AttributeTargets.Interface)]
public class DbusConsumeAttribute(string interfaceName) : Attribute
{
    /// <summary>
    /// Name of the interface
    /// </summary>
    public string InterfaceName { get; } = interfaceName;
    /// <summary>
    /// Standard path if not overridden
    /// </summary>
    public string? Path { get; set; }
    /// <summary>
    /// Standard destination if not overridden
    /// </summary>
    public string? Destination { get; set; }
}
