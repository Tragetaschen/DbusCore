namespace Dbus
{
    public static class Alignment
    {
        public static int Calculate(int position, int alignment)
        {
            var bytesIntoAlignment = position & alignment - 1;
            if (bytesIntoAlignment == 0)
                return 0;
            else
                return alignment - bytesIntoAlignment;
        }
    }
}
