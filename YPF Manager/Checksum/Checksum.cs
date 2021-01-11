using System;
using System.IO;

namespace Ypf_Manager
{
    public abstract class Checksum
    {
        public abstract String Name { get; }

        public abstract UInt32 ComputeHash(Byte[] inputBytes);

        public UInt32 ComputeHash(Stream inputStream, int length)
        {
            Byte[] buf = new byte[length];

            inputStream.Read(buf, 0, length);

            return ComputeHash(buf);
        }

    }
}
