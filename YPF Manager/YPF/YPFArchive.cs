using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Ypf_Manager
{
    class YPFArchive
    {

        //
        // Create a new archive from a given folder
        //

        public static void Create(String inputFolder, String outputFile, Int32 version)
        {
            Console.WriteLine("[*COMPRESS*]");
            Console.WriteLine($"Folder: {inputFolder}");
            Console.WriteLine($"File: {outputFile}");
            Console.WriteLine($"Version: {version}");
            Console.WriteLine();

            Console.WriteLine("Initializing header");

            if (version < 234 || version > 500)
            {
                throw new Exception($"Version {version} Not Supported");
            }

            YPFHeader header = new YPFHeader();
            header.Version = version;

            header.SetChecksum();
            header.SetFileNameEncryptionKey();

            String[] filesToProcess = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);

            header.ArchivedFilesHeaderSize = 32;

            foreach (String s in filesToProcess)
            {
                YPFEntry entry = new YPFEntry();

                String fileName = s.Substring(inputFolder.Length + 1);
                String fileExtension = Path.GetExtension(fileName).Substring(1);

                entry.Type = Enum.IsDefined(typeof(YPFEntry.FileType), fileExtension) ? (YPFEntry.FileType)Enum.Parse(typeof(YPFEntry.FileType), fileExtension, true) : YPFEntry.FileType.text;

                if (fileName.EndsWith(".ycg"))
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                }

                Byte[] encodedName = header.Encoding.GetBytes(fileName);

                entry.FileName = fileName;
                entry.NameChecksum = header.NameChecksum.ComputeHash(encodedName);

                header.ArchivedFilesHeaderSize += 23 + encodedName.Length + (header.Version >= 479 ? 4 : 0);

                header.ArchivedFiles.Add(entry);
            }

            header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.NameChecksum).ToList();

            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            using (BinaryWriter bw = new BinaryWriter(outputFileStream))
            {
                outputFileStream.Position = header.ArchivedFilesHeaderSize;

                for (int j = 0; j < header.ArchivedFiles.Count; j++)
                {
                    YPFEntry entry = header.ArchivedFiles[j];

                    Console.WriteLine($"Adding {entry.FileName}");

                    using (MemoryStream rawFileStream = new MemoryStream())
                    {
                        using (FileStream inputFileStream = new FileStream($@"{inputFolder}\{entry.FileName}{(entry.Type == YPFEntry.FileType.ycg ? ".ycg" : "")}", FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
                        {
                            // Check if the current file saturated the 32 filesize bits
                            if (inputFileStream.Length > Int32.MaxValue)
                            {
                                throw new Exception("Max filesize reached for the current YPF version");
                            }

                            rawFileStream.Capacity = (Int32)inputFileStream.Length;
                            Util.CopyStream(inputFileStream, rawFileStream, inputFileStream.Length);
                        }

                        rawFileStream.Position = 0;

                        header.ArchivedFiles[j].Offset = outputFileStream.Position;
                        header.ArchivedFiles[j].RawFileSize = (Int32)rawFileStream.Length;

                        if (header.Version < 490)
                        {
                            UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(rawFileStream, (Int32)rawFileStream.Length);

                            YPFEntry checkDuplicate = header.ArchivedFiles.FirstOrDefault(x => x.DataChecksum == calculatedDataChecksum);

                            if (checkDuplicate != null)
                            {
                                header.ArchivedFiles[j].Offset = checkDuplicate.Offset;
                            }
                            else
                            {
                                rawFileStream.Position = 0;

                                Util.CopyStream(rawFileStream, outputFileStream, rawFileStream.Length);
                            }

                            header.ArchivedFiles[j].CompressedFileSize = (Int32)rawFileStream.Length;
                            header.ArchivedFiles[j].IsCompressed = false;
                            header.ArchivedFiles[j].DataChecksum = calculatedDataChecksum;
                        }
                        else
                        {
                            using (MemoryStream compressedFileStream = new MemoryStream())
                            {
                                // Best compression
                                compressedFileStream.WriteByte(0x78);
                                compressedFileStream.WriteByte(0xDA);

                                using (var compressionStream = new DeflateStream(compressedFileStream, CompressionLevel.Optimal, true))
                                {
                                    Util.CopyStream(rawFileStream, compressionStream, rawFileStream.Length);
                                }

                                if (compressedFileStream.Length < rawFileStream.Length)
                                {
                                    compressedFileStream.Position = 0;

                                    UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(compressedFileStream, (Int32)compressedFileStream.Length);

                                    YPFEntry checkDuplicate = header.ArchivedFiles.FirstOrDefault(x => x.DataChecksum == calculatedDataChecksum);

                                    if (checkDuplicate != null)
                                    {
                                        header.ArchivedFiles[j].Offset = checkDuplicate.Offset;
                                    }
                                    else
                                    {
                                        compressedFileStream.Position = 0;

                                        Util.CopyStream(compressedFileStream, outputFileStream, compressedFileStream.Length);
                                    }

                                    header.ArchivedFiles[j].CompressedFileSize = (Int32)compressedFileStream.Length;
                                    header.ArchivedFiles[j].IsCompressed = true;
                                    header.ArchivedFiles[j].DataChecksum = calculatedDataChecksum;
                                }
                                else
                                {
                                    rawFileStream.Position = 0;

                                    UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(rawFileStream, (Int32)rawFileStream.Length);

                                    YPFEntry checkDuplicate = header.ArchivedFiles.FirstOrDefault(x => x.DataChecksum == calculatedDataChecksum);

                                    if (checkDuplicate != null)
                                    {
                                        header.ArchivedFiles[j].Offset = checkDuplicate.Offset;
                                    }
                                    else
                                    {
                                        rawFileStream.Position = 0;

                                        Util.CopyStream(rawFileStream, outputFileStream, rawFileStream.Length);
                                    }

                                    header.ArchivedFiles[j].CompressedFileSize = (Int32)rawFileStream.Length;
                                    header.ArchivedFiles[j].IsCompressed = false;
                                    header.ArchivedFiles[j].DataChecksum = calculatedDataChecksum;
                                }
                            }
                        }

                        // Check if the current file saturated the 32 offset bits
                        if (header.Version < 479 && outputFileStream.Position > Int32.MaxValue)
                        {
                            throw new Exception("Max offset reached for the current YPF version");
                        }
                    }
                }

                Console.WriteLine($"Finalizing header");

                outputFileStream.Position = 0;

                bw.Write(header.Signature);
                bw.Write(header.Version);
                bw.Write(header.ArchivedFiles.Count);
                bw.Write(header.ArchivedFilesHeaderSize);

                outputFileStream.Position += 16;

                foreach (YPFEntry entry in header.ArchivedFiles)
                {
                    bw.Write(entry.NameChecksum);

                    Byte[] encodedName = header.Encoding.GetBytes(entry.FileName);

                    if (encodedName.Length > 0xFF)
                    {
                        throw new Exception("File name length can't be longer than 255 bytes (Shift-JIS)");
                    }

                    byte lengthEncoded = Util.OneComplement((byte)header.GetLengthSwappingTable().ToList().IndexOf((byte)encodedName.Length));

                    bw.Write(lengthEncoded);

                    for (int j = 0; j < encodedName.Length; j++)
                    {
                        encodedName[j] = Util.OneComplement((byte)(encodedName[j] ^ header.FileNameEncryptionKey));
                    }

                    bw.Write(encodedName);
                    bw.Write((byte)entry.Type);
                    bw.Write(entry.IsCompressed);
                    bw.Write(entry.RawFileSize);
                    bw.Write(entry.CompressedFileSize);

                    if (header.Version < 479)
                    {
                        bw.Write((Int32)entry.Offset);
                    }
                    else
                    {
                        bw.Write(entry.Offset);
                    }

                    bw.Write(entry.DataChecksum);
                }

                if (outputFileStream.Position != header.ArchivedFilesHeaderSize)
                {
                    throw new Exception("Unexpected Header Size");
                }
            }
        }


        //
        // Extract an archive to the specified folder
        //

        public static void Extract(String inputFile, String outputFolder)
        {
            Console.WriteLine("[*EXTRACT*]");
            Console.WriteLine($"File: {inputFile}");
            Console.WriteLine($"Folder: {outputFolder}");
            Console.WriteLine();

            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    YPFHeader header = new YPFHeader();

                    if (!Enumerable.SequenceEqual(header.Signature, br.ReadBytes(4)))
                    {
                        throw new Exception("Invalid Archive Signature");
                    }

                    header.Version = br.ReadInt32();
                    int filesCount = br.ReadInt32();
                    header.ArchivedFilesHeaderSize = br.ReadInt32();

                    fs.Position += 16;

                    if (header.Version < 0)
                    {
                        throw new Exception("Invalid Version");
                    }
                    else if (header.Version < 234 || header.Version > 500)
                    {
                        throw new Exception($"Version {header.Version} Not Supported");
                    }

                    if (filesCount <= 0)
                    {
                        throw new Exception("Invalid Files Count");
                    }

                    if (header.ArchivedFilesHeaderSize <= 0)
                    {
                        throw new Exception("Invalid Archived Files Header Size");
                    }

                    header.SetChecksum();
                    header.SetFileNameEncryptionKey();

                    for (int i = 0; i < filesCount; i++)
                    {
                        YPFEntry af = new YPFEntry();

                        af.NameChecksum = br.ReadUInt32();

                        Byte fileNameLengthEncoded = Util.OneComplement(br.ReadByte());
                        Byte fileNameLengthDecoded = header.GetLengthSwappingTable()[fileNameLengthEncoded];

                        Byte[] fileNameEncoded = br.ReadBytes(fileNameLengthDecoded);

                        for (int j = 0; j < fileNameLengthDecoded; j++)
                        {
                            fileNameEncoded[j] = (byte)(Util.OneComplement(fileNameEncoded[j]) ^ header.FileNameEncryptionKey);
                        }

                        af.FileName = header.Encoding.GetString(fileNameEncoded);
                        af.Type = (YPFEntry.FileType)br.ReadByte();
                        af.IsCompressed = (br.ReadByte() == 1);
                        af.RawFileSize = br.ReadInt32();
                        af.CompressedFileSize = br.ReadInt32();

                        if (header.Version < 479)
                        {
                            af.Offset = br.ReadInt32();
                        }
                        else
                        {
                            af.Offset = br.ReadInt64();
                        }

                        af.DataChecksum = br.ReadUInt32();

                        UInt32 calculatedNameChecksum = header.NameChecksum.ComputeHash(fileNameEncoded);

                        if (calculatedNameChecksum != af.NameChecksum)
                        {
                            throw new Exception("Invalid Name Checksum");
                        }

                        if (!Enum.IsDefined(typeof(YPFEntry.FileType), af.Type))
                        {
                            throw new Exception("Unexpected File Type");
                        }

                        header.ArchivedFiles.Add(af);
                    }

                    // Order by offset to improve read performance
                    header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

                    foreach (YPFEntry af in header.ArchivedFiles)
                    {
                        Console.WriteLine($"Extracting {af.FileName}");

                        String outputFileName = $@"{outputFolder}\{af.FileName}";

                        if (af.Type == YPFEntry.FileType.ycg)
                        {
                            outputFileName += ".ycg";
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

                        fs.Position = af.Offset;

                        using (FileStream fs1 = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.None))
                        {
                            using (MemoryStream ms = new MemoryStream(af.CompressedFileSize))
                            {
                                Util.CopyStream(fs, ms, af.CompressedFileSize);

                                ms.Position = 0;

                                UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(ms, (Int32)ms.Length);

                                if (af.DataChecksum != calculatedDataChecksum)
                                {
                                    throw new Exception("Invalid Data Checksum");
                                }

                                ms.Position = 0;

                                if (af.IsCompressed)
                                {
                                    using (MemoryStream decompressedFileStream = new MemoryStream(af.RawFileSize))
                                    {
                                        using (GZipStream decompressionStream = new GZipStream(ms, CompressionMode.Decompress))
                                        {
                                            decompressionStream.CopyTo(decompressedFileStream);
                                        }

                                        if (decompressedFileStream.Length != af.RawFileSize)
                                        {
                                            throw new Exception("Invalid Decompressed File Size");
                                        }

                                        decompressedFileStream.Position = 0;

                                        Util.CopyStream(decompressedFileStream, fs1, af.RawFileSize);
                                    }
                                }
                                else
                                {
                                    Util.CopyStream(ms, fs1, af.RawFileSize);
                                }
                            }
                        }
                    }
                }
            }
        }


        //
        // Print the info of a specified archive
        //

        public static void PrintInfo(String inputFile)
        {
            Console.WriteLine("[*PRINT INFO*]");
            Console.WriteLine($"File: {inputFile}");
            Console.WriteLine();

            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    YPFHeader header = new YPFHeader();

                    if (!Enumerable.SequenceEqual(header.Signature, br.ReadBytes(4)))
                    {
                        throw new Exception("Invalid Archive Signature");
                    }

                    header.Version = br.ReadInt32();
                    int filesCount = br.ReadInt32();
                    header.ArchivedFilesHeaderSize = br.ReadInt32();

                    fs.Position += 16;

                    if (header.Version < 0)
                    {
                        throw new Exception("Invalid Version");
                    }
                    else if (header.Version < 234 || header.Version > 500)
                    {
                        throw new Exception($"Version {header.Version} Not Supported");
                    }

                    if (filesCount <= 0)
                    {
                        throw new Exception("Invalid Files Count");
                    }

                    if (header.ArchivedFilesHeaderSize <= 0)
                    {
                        throw new Exception("Invalid Archived Files Header Size");
                    }

                    header.SetChecksum();
                    header.SetFileNameEncryptionKey();

                    Console.WriteLine("[HEADER]");
                    Console.WriteLine($"Version: {header.Version}");
                    Console.WriteLine($"Files Count: {filesCount}");
                    Console.WriteLine($"Header Size: {header.ArchivedFilesHeaderSize}");
                    Console.WriteLine($"Name Checksum Algorithm: {header.NameChecksum.Name}");
                    Console.WriteLine($"Data Checksum Algorithm: {header.DataChecksum.Name}");
                    Console.WriteLine($"Filename Encryption Key: {header.FileNameEncryptionKey.ToString("x2")}");
                    Console.WriteLine();

                    Console.WriteLine("[FILES]");

                    for (int i = 0; i < filesCount; i++)
                    {
                        YPFEntry af = new YPFEntry();

                        af.NameChecksum = br.ReadUInt32();

                        Byte fileNameLengthEncoded = Util.OneComplement(br.ReadByte());
                        Byte fileNameLengthDecoded = header.GetLengthSwappingTable()[fileNameLengthEncoded];

                        Byte[] fileNameEncoded = br.ReadBytes(fileNameLengthDecoded);

                        for (int j = 0; j < fileNameLengthDecoded; j++)
                        {
                            fileNameEncoded[j] = (byte)(Util.OneComplement(fileNameEncoded[j]) ^ header.FileNameEncryptionKey);
                        }

                        af.FileName = header.Encoding.GetString(fileNameEncoded);

                        af.Type = (YPFEntry.FileType)br.ReadByte();
                        af.IsCompressed = (br.ReadByte() == 1);
                        af.RawFileSize = br.ReadInt32();
                        af.CompressedFileSize = br.ReadInt32();

                        if (header.Version < 479)
                        {
                            af.Offset = br.ReadInt32();
                        }
                        else
                        {
                            af.Offset = br.ReadInt64();
                        }

                        af.DataChecksum = br.ReadUInt32();

                        UInt32 calculatedNameChecksum = header.NameChecksum.ComputeHash(fileNameEncoded);

                        if (calculatedNameChecksum != af.NameChecksum)
                        {
                            throw new Exception("Invalid Name Checksum");
                        }

                        if (!Enum.IsDefined(typeof(YPFEntry.FileType), af.Type))
                        {
                            throw new Exception("Unexpected File Type");
                        }

                        Console.WriteLine($"[{i + 1}/{filesCount}]");
                        Console.WriteLine($"\tFilename: {af.FileName}");
                        Console.WriteLine($"\tCompressed: {af.IsCompressed}");
                        Console.WriteLine($"\tSize: {af.RawFileSize}");
                        Console.WriteLine($"\tCompressed Size: {af.CompressedFileSize}");
                        Console.WriteLine($"\tOffset: {af.Offset}");
                        Console.WriteLine($"\tType: {af.Type}");
                        Console.WriteLine($"\tName Checksum: {af.NameChecksum.ToString("x8")}");
                        Console.WriteLine($"\tData Checksum: {af.DataChecksum.ToString("x8")}");
                        Console.WriteLine();

                        header.ArchivedFiles.Add(af);
                    }

                    // Order by offset to improve read performance
                    header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

                    Console.WriteLine();
                    Console.WriteLine("[DATA]");
                    Console.Write("Checking Data Checksum...");

                    foreach (YPFEntry af in header.ArchivedFiles)
                    {
                        fs.Position = af.Offset;

                        using (MemoryStream ms = new MemoryStream(af.CompressedFileSize))
                        {
                            Util.CopyStream(fs, ms, af.CompressedFileSize);

                            ms.Position = 0;

                            UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(ms, (Int32)ms.Length);

                            if (af.DataChecksum != calculatedDataChecksum)
                            {
                                throw new Exception("Invalid Data Checksum");
                            }

                            ms.Position = 0;

                            if (af.IsCompressed)
                            {
                                using (MemoryStream decompressedFileStream = new MemoryStream(af.RawFileSize))
                                {
                                    using (GZipStream decompressionStream = new GZipStream(ms, CompressionMode.Decompress))
                                    {
                                        decompressionStream.CopyTo(decompressedFileStream);
                                    }

                                    if (decompressedFileStream.Length != af.RawFileSize)
                                    {
                                        throw new Exception("Invalid Decompressed File Size");
                                    }

                                }
                            }
                        }
                    }

                    Console.WriteLine(" Complete");
                }
            }
        }

    }
}
