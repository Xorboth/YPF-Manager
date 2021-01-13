using System;
using System.IO;

namespace Ypf_Manager
{
    public static class Util
    {

        //
        // Returns the one complement of a given byte
        //

        public static Byte OneComplement(Byte i)
        {
            return (Byte)~i;
        }


        //
        // Copy data from input stream to ouput stream
        //

        public static void CopyStream(Stream input, Stream output, Int64 length)
        {
            Int32 bufferSize = 4096;

            Byte[] buffer = new Byte[bufferSize];

            Int64 bytesToRead = length;

            while (bytesToRead > 0)
            {
                Int32 bytesRead = input.Read(buffer, 0, (Int32)Math.Min(bufferSize, bytesToRead));

                output.Write(buffer, 0, bytesRead);

                bytesToRead -= bytesRead;
            }
        }

    }
}
