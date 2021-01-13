using System;
using System.IO;

namespace Ypf_Manager
{
    public static class Util
    {

        //
        // Returns the one complement of a given byte
        //

        public static byte OneComplement(byte i)
        {
            return (byte)~i;
        }


        //
        // Copy data from input stream to ouput stream
        //

        public static void CopyStream(Stream input, Stream output, Int64 length)
        {
            int bufferSize = 4096;

            byte[] buffer = new byte[bufferSize];

            Int64 bytesToRead = length;

            while (bytesToRead > 0)
            {
                int bytesRead = input.Read(buffer, 0, (Int32)Math.Min(bufferSize, bytesToRead));

                output.Write(buffer, 0, bytesRead);

                bytesToRead -= bytesRead;
            }
        }

    }
}
