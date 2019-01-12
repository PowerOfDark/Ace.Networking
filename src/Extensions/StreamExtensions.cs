using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ace.Networking.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ToArray(this Stream stream)
        {
            var buf = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buf, 0, buf.Length);
            return buf;
        }

    }
}
