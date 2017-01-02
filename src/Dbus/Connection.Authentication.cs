﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        private static async Task authenticate(Stream stream)
        {
            stream.WriteByte(0);
            using (var writer = new StreamWriter(stream, Encoding.ASCII, 32, true))
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 32, true))
            {
                writer.NewLine = "\r\n";

                await writer.WriteAsync("AUTH EXTERNAL ").ConfigureAwait(false);

                var uid = getuid();
                var stringUid = $"{uid}";
                var uidBytes = encoding.GetBytes(stringUid);
                foreach (var b in uidBytes)
                    await writer.WriteAsync($"{b:X}").ConfigureAwait(false);

                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                var response = await reader.ReadLineAsync().ConfigureAwait(false);
                if (!response.StartsWith("OK "))
                    throw new InvalidOperationException("Authentication failed: " + response);

                await writer.WriteLineAsync("BEGIN").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        [DllImport("c")]
        private static extern int getuid();
    }
}
