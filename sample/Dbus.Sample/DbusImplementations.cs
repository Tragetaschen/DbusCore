namespace Dbus.Sample
{
    public static partial class DbusImplementations
    {
        static partial void DoInit();

        public static void Init() => DoInit();
    }
}
