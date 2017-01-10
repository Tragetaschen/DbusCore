using Microsoft.Extensions.DependencyInjection;

namespace WebApplication.Dbus
{
    public static partial class DbusImplementations
    {
        static partial void DoAddDbus(IServiceCollection services);

        public static IServiceCollection AddDbus(this IServiceCollection services)
        {
            DoAddDbus(services);
            return services;
        }
    }
}
