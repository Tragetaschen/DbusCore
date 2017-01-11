using System.Threading.Tasks;

namespace Dbus
{
    public interface IDbusPropertyInitialization
    {
        Task PropertyInitializationFinished { get; }
    }
}
