using Ionic.Zlib;
using System;
using System.IO;

namespace Ypf_Manager
{
    public static class Util
    {

        //
        // Function(s)
        //

        // Returns the one complement of a given byte
        public static Byte OneComplement(Byte i)
        {
            return (Byte)~i;
        }


        // Copy data from input stream to ouput stream
        public static void CopyStream(Stream inputStream, Stream outputStream, Int64 length)
        {
            Int32 bufferSize = 4096;

            Byte[] buffer = new Byte[bufferSize];

            Int64 bytesToRead = length;

            while (bytesToRead > 0)
            {
                Int32 bytesRead = inputStream.Read(buffer, 0, (Int32)Math.Min(bufferSize, bytesToRead));

                outputStream.Write(buffer, 0, bytesRead);

                bytesToRead -= bytesRead;
            }
        }


        // Decompress from input stream to memory stream
        public static MemoryStream DecompressZlibStream(Stream inputStream, int decompressedSize)
        {
            MemoryStream decompressedFileStream = new MemoryStream(decompressedSize);

            using (ZlibStream decompressionStream = new ZlibStream(inputStream, Ionic.Zlib.CompressionMode.Decompress, true))
            {
                decompressionStream.CopyTo(decompressedFileStream);
            }

            decompressedFileStream.Position = 0;

            if (decompressedFileStream.Length != decompressedSize)
            {
                throw new Exception("Invalid Decompressed File Size");
            }

            return decompressedFileStream;
        }


        // Compress from input stream to memory stream
        public static MemoryStream CompressZlibStream(Stream inputStream)
        {
            MemoryStream compressedFileStream = new MemoryStream();

            using (ZlibStream compressionStream = new ZlibStream(compressedFileStream, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.Level9, true))
            {
                CopyStream(inputStream, compressionStream, inputStream.Length);
            }

            return compressedFileStream;
        }

    }
}
