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

        public String ComputeHashString(Stream inputStream, int length)
        {
            return ComputeHash(inputStream, length).ToString("x8");
        }

        public String ComputeHashString(Byte[] inputBytes)
        {
            return ComputeHash(inputBytes).ToString("x8");
        }
    }
}
