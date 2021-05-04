using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ypf_Manager
{
    static class YPFArchive
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

            YPFHeader header = new YPFHeader(version);

            String[] filesToProcess = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);

            header.ArchivedFilesHeaderSize = 32;

            foreach (String file in filesToProcess)
            {
                YPFEntry entry = new YPFEntry();

                String fileName = file.Substring(inputFolder.Length + 1);
                String fileExtension = Path.GetExtension(fileName).Substring(1);

                // Remove YCG extension from the filename
                if (fileExtension == "ycg")
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                }

                // Detect null filename
                // This can happen when using YCG files
                // e.g. fileName = ".ycg"
                if (!(fileName.Length > 0))
                {
                    throw new Exception("Filename can't be null");
                }

                // Detect duplicated filenames (O(n^2))
                // This can happen when using YCG and PNG files together
                // e.g. fileName1 = "test1.png"
                //      fileName2 = "test1.png.ycg"
                //
                //      since ".ycg" will be trimmed from the end, the two files will have the same name:
                //
                //      fileName1 = "test1.png" (PNG)
                //      fileName2 = "test1.png" (YCG)
                if (header.ArchivedFiles.FirstOrDefault(x => x.FileName == fileName) != null)
                {
                    throw new Exception("Filenames must be unique");
                }

                Byte[] encodedName = header.Encoding.GetBytes(fileName);

                entry.FileName = fileName;
                entry.NameChecksum = header.NameChecksum.ComputeHash(encodedName);
                entry.Type = Enum.IsDefined(typeof(YPFEntry.FileType), fileExtension) ? (YPFEntry.FileType)Enum.Parse(typeof(YPFEntry.FileType), fileExtension, true) : YPFEntry.FileType.text;

                header.ArchivedFilesHeaderSize += 23 + encodedName.Length + (header.Version >= 479 ? 4 : 0);

                header.ArchivedFiles.Add(entry);
            }

            header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.NameChecksum).ToList();

            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            using (BinaryWriter outputBinaryWriter = new BinaryWriter(outputFileStream))
            {
                outputFileStream.Position = header.ArchivedFilesHeaderSize;

                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

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

                        header.ArchivedFiles[i].Offset = outputFileStream.Position;
                        header.ArchivedFiles[i].RawFileSize = (Int32)rawFileStream.Length;

                        using (MemoryStream compressedFileStream = Util.CompressStream(rawFileStream))
                        {
                            // Check if the compressed file is smaller than the original one
                            if (compressedFileStream.Length < rawFileStream.Length)
                            {
                                compressedFileStream.Position = 0;

                                UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(compressedFileStream, (Int32)compressedFileStream.Length);

                                YPFEntry duplicatedEntry = header.FindDuplicateEntry(calculatedDataChecksum, (Int32)rawFileStream.Length);

                                if (duplicatedEntry != null)
                                {
                                    header.ArchivedFiles[i].Offset = duplicatedEntry.Offset;
                                }
                                else
                                {
                                    compressedFileStream.Position = 0;

                                    Util.CopyStream(compressedFileStream, outputFileStream, compressedFileStream.Length);
                                }

                                header.ArchivedFiles[i].CompressedFileSize = (Int32)compressedFileStream.Length;
                                header.ArchivedFiles[i].IsCompressed = true;
                                header.ArchivedFiles[i].DataChecksum = calculatedDataChecksum;
                            }
                            else
                            {
                                rawFileStream.Position = 0;

                                UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(rawFileStream, (Int32)rawFileStream.Length);

                                YPFEntry duplicatedEntry = header.FindDuplicateEntry(calculatedDataChecksum, (Int32)rawFileStream.Length);

                                if (duplicatedEntry != null)
                                {
                                    header.ArchivedFiles[i].Offset = duplicatedEntry.Offset;
                                }
                                else
                                {
                                    rawFileStream.Position = 0;

                                    Util.CopyStream(rawFileStream, outputFileStream, rawFileStream.Length);
                                }

                                header.ArchivedFiles[i].CompressedFileSize = (Int32)rawFileStream.Length;
                                header.ArchivedFiles[i].IsCompressed = false;
                                header.ArchivedFiles[i].DataChecksum = calculatedDataChecksum;
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

                outputBinaryWriter.Write(header.Signature);
                outputBinaryWriter.Write(header.Version);
                outputBinaryWriter.Write(header.ArchivedFiles.Count);
                outputBinaryWriter.Write(header.ArchivedFilesHeaderSize);

                outputFileStream.Position += 16;

                foreach (YPFEntry entry in header.ArchivedFiles)
                {
                    outputBinaryWriter.Write(entry.NameChecksum);

                    Byte[] encodedName = header.Encoding.GetBytes(entry.FileName);

                    if (encodedName.Length > 0xFF)
                    {
                        throw new Exception("File name length can't be longer than 255 bytes (Shift-JIS)");
                    }

                    byte lengthEncoded = Util.OneComplement((byte)header.LengthSwappingTable.ToList().IndexOf((byte)encodedName.Length));

                    outputBinaryWriter.Write(lengthEncoded);

                    for (int i = 0; i < encodedName.Length; i++)
                    {
                        encodedName[i] = Util.OneComplement((byte)(encodedName[i] ^ header.FileNameEncryptionKey));
                    }

                    outputBinaryWriter.Write(encodedName);
                    outputBinaryWriter.Write((byte)entry.Type);
                    outputBinaryWriter.Write(entry.IsCompressed);
                    outputBinaryWriter.Write(entry.RawFileSize);
                    outputBinaryWriter.Write(entry.CompressedFileSize);

                    if (header.Version < 479)
                    {
                        outputBinaryWriter.Write((Int32)entry.Offset);
                    }
                    else
                    {
                        outputBinaryWriter.Write(entry.Offset);
                    }

                    outputBinaryWriter.Write(entry.DataChecksum);
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

            using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                YPFHeader header = new YPFHeader(inputFileStream);

                // Order by offset to improve read performance
                header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

                    Console.WriteLine($"[{i + 1}/{header.ArchivedFiles.Count}] Extracting {entry.FileName}");

                    String customExtension = (entry.Type == YPFEntry.FileType.ycg ? ".ycg" : "");
                    String outputFileName = $@"{outputFolder}\{entry.FileName}{customExtension}";

                    inputFileStream.Position = entry.Offset;

                    using (MemoryStream ms = new MemoryStream(entry.CompressedFileSize))
                    {
                        Util.CopyStream(inputFileStream, ms, entry.CompressedFileSize);

                        ms.Position = 0;

                        header.ValidateDataChecksum(ms, entry.CompressedFileSize, entry.DataChecksum);

                        Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

                        using (FileStream outputFileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.None))
                        {
                            if (entry.IsCompressed)
                            {
                                using (MemoryStream decompressedFileStream = Util.DecompressStream(ms, entry.RawFileSize))
                                {
                                    Util.CopyStream(decompressedFileStream, outputFileStream, entry.RawFileSize);
                                }
                            }
                            else
                            {
                                Util.CopyStream(ms, outputFileStream, entry.RawFileSize);
                            }
                        }
                    }
                }
            }
        }


        //
        // Print the info of a specified archive
        //

        public static void PrintInfo(String inputFile, Boolean validateDataChecksum = true)
        {
            Console.WriteLine("[*PRINT INFO*]");
            Console.WriteLine($"File: {inputFile}");
            Console.WriteLine();

            using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                YPFHeader header = new YPFHeader(inputFileStream);

                Console.WriteLine("[HEADER]");
                Console.WriteLine($"Version: {header.Version}");
                Console.WriteLine($"Files Count: {header.ArchivedFiles.Capacity}");
                Console.WriteLine($"Header Size: {header.ArchivedFilesHeaderSize}");
                Console.WriteLine($"Name Checksum Algorithm: {header.NameChecksum.Name}");
                Console.WriteLine($"Data Checksum Algorithm: {header.DataChecksum.Name}");
                Console.WriteLine($"Filename Encryption Key: {header.FileNameEncryptionKey:x2}");
                Console.WriteLine();

                Console.WriteLine("[FILES]");

                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

                    Console.WriteLine($"[{i + 1}/{header.ArchivedFiles.Capacity}]");
                    Console.WriteLine($"\tFilename: {entry.FileName}");
                    Console.WriteLine($"\tCompressed: {entry.IsCompressed}");
                    Console.WriteLine($"\tSize: {entry.RawFileSize}");
                    Console.WriteLine($"\tCompressed Size: {entry.CompressedFileSize}");
                    Console.WriteLine($"\tOffset: {entry.Offset}");
                    Console.WriteLine($"\tType: {entry.Type}");
                    Console.WriteLine($"\tName Checksum: {entry.NameChecksum:x8}");
                    Console.WriteLine($"\tData Checksum: {entry.DataChecksum:x8}");
                    Console.WriteLine();
                }

                // Order by offset to improve read performance
                header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

                if (validateDataChecksum)
                {
                    Console.WriteLine();
                    Console.WriteLine("[DATA]");
                    Console.Write("Checking Data Checksum...");

                    foreach (YPFEntry entry in header.ArchivedFiles)
                    {
                        inputFileStream.Position = entry.Offset;

                        header.ValidateDataChecksum(inputFileStream, entry.CompressedFileSize, entry.DataChecksum);
                    }

                    Console.WriteLine(" Complete");
                }
            }
        }

    }
}
