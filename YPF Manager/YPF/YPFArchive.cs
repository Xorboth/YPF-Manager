using System;
using System.IO;
using System.Linq;

namespace Ypf_Manager
{
    static class YPFArchive
    {

        //
        // Function(s)
        //

        // Create a new archive from a given folder
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

            // Set list capacity to improve performance
            header.ArchivedFiles.Capacity = filesToProcess.Length;

            // Skip the constant header section
            header.ArchivedFilesHeaderSize = 32;

            // Process files to get their data and accurately calculate the header size
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

                // Accurately calculate the entry header size
                header.ArchivedFilesHeaderSize += 23 + encodedName.Length + (header.Version >= 479 ? 4 : 0);

                header.ArchivedFiles.Add(entry);
            }

            // Order list by Name Checksum as expected by the archive format
            header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.NameChecksum).ToList();

            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            using (BinaryWriter outputBinaryWriter = new BinaryWriter(outputFileStream))
            {
                // Skip header to directly write the files
                outputFileStream.Position = header.ArchivedFilesHeaderSize;

                // Read the files and write them to the archive
                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

                    Console.WriteLine($"Adding {entry.FileName}");

                    using (MemoryStream rawMemoryStream = new MemoryStream())
                    {
                        String filePath = $@"{inputFolder}\{entry.FileName}{(entry.Type == YPFEntry.FileType.ycg ? ".ycg" : "")}";

                        // Copy the FileStream to a MemoryStream to improve performance
                        using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
                        {
                            // Check if the current file saturated the 32 filesize bits
                            if (inputFileStream.Length > Int32.MaxValue)
                            {
                                throw new Exception("Max filesize reached for the current YPF version");
                            }

                            // Check if the current file is empty
                            if (inputFileStream.Length == 0)
                            {
                                throw new Exception("Empty files (0 Byte) are not supported");
                            }

                            rawMemoryStream.Capacity = (Int32)inputFileStream.Length;
                            Util.CopyStream(inputFileStream, rawMemoryStream, inputFileStream.Length);
                        }

                        rawMemoryStream.Position = 0;

                        header.ArchivedFiles[i].Offset = outputFileStream.Position;
                        header.ArchivedFiles[i].RawFileSize = (Int32)rawMemoryStream.Length;

                        using (MemoryStream compressedFileStream = Util.CompressZlibStream(rawMemoryStream))
                        {
                            // Check if the compressed file is smaller than the original one
                            if (compressedFileStream.Length < rawMemoryStream.Length)
                            {
                                compressedFileStream.Position = 0;

                                UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(compressedFileStream, (Int32)compressedFileStream.Length);

                                YPFEntry duplicatedEntry = header.FindDuplicateEntry(calculatedDataChecksum, (Int32)rawMemoryStream.Length);

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
                                rawMemoryStream.Position = 0;

                                UInt32 calculatedDataChecksum = header.DataChecksum.ComputeHash(rawMemoryStream, (Int32)rawMemoryStream.Length);

                                YPFEntry duplicatedEntry = header.FindDuplicateEntry(calculatedDataChecksum, (Int32)rawMemoryStream.Length);

                                if (duplicatedEntry != null)
                                {
                                    header.ArchivedFiles[i].Offset = duplicatedEntry.Offset;
                                }
                                else
                                {
                                    rawMemoryStream.Position = 0;

                                    Util.CopyStream(rawMemoryStream, outputFileStream, rawMemoryStream.Length);
                                }

                                header.ArchivedFiles[i].CompressedFileSize = (Int32)rawMemoryStream.Length;
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


                //
                // Write the header since all the entry data is now known
                //

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


        // Extract an archive to the specified folder
        public static void Extract(String inputFile, String outputFolder)
        {
            Console.WriteLine("[*EXTRACT*]");
            Console.WriteLine($"File: {inputFile}");
            Console.WriteLine($"Folder: {outputFolder}");
            Console.WriteLine();

            using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                YPFHeader header = new YPFHeader(inputFileStream);

                // Order by offset to improve sequential read performance
                header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

                    Console.WriteLine($"[{i + 1}/{header.ArchivedFiles.Count}] Extracting {entry.FileName}");

                    String customExtension = (entry.Type == YPFEntry.FileType.ycg ? ".ycg" : "");
                    String outputFileName = $@"{outputFolder}\{entry.FileName}{customExtension}";

                    inputFileStream.Position = entry.Offset;

                    using (MemoryStream entryMemoryStream = new MemoryStream(entry.CompressedFileSize))
                    {
                        Util.CopyStream(inputFileStream, entryMemoryStream, entry.CompressedFileSize);

                        entryMemoryStream.Position = 0;

                        header.ValidateDataChecksum(entryMemoryStream, entry.CompressedFileSize, entry.DataChecksum);

                        Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

                        using (FileStream outputFileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.None))
                        {
                            if (entry.IsCompressed)
                            {
                                using (MemoryStream decompressedEntryMemoryStream = Util.DecompressZlibStream(entryMemoryStream, entry.RawFileSize))
                                {
                                    Util.CopyStream(decompressedEntryMemoryStream, outputFileStream, entry.RawFileSize);
                                }
                            }
                            else
                            {
                                Util.CopyStream(entryMemoryStream, outputFileStream, entry.RawFileSize);
                            }
                        }
                    }
                }
            }
        }


        // Print the info of a specified archive
        public static void PrintInfo(String inputFile, Boolean skipDataChecksum)
        {
            Console.WriteLine("[*PRINT INFO*]");
            Console.WriteLine($"File: {inputFile}");
            Console.WriteLine();

            using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                YPFHeader header = new YPFHeader(inputFileStream);

                // Print header info
                Console.WriteLine("[HEADER]");
                Console.WriteLine($"Version: {header.Version}");
                Console.WriteLine($"Files Count: {header.ArchivedFiles.Count}");
                Console.WriteLine($"Header Size: {header.ArchivedFilesHeaderSize}");
                Console.WriteLine($"Name Checksum Algorithm: {header.NameChecksum.Name}");
                Console.WriteLine($"Data Checksum Algorithm: {header.DataChecksum.Name}");
                Console.WriteLine($"Filename Encryption Key: {header.FileNameEncryptionKey:x2}");
                Console.WriteLine();

                Console.WriteLine("[FILES]");

                for (int i = 0; i < header.ArchivedFiles.Count; i++)
                {
                    YPFEntry entry = header.ArchivedFiles[i];

                    // Print entry info
                    Console.WriteLine($"[{i + 1}/{header.ArchivedFiles.Count}]");
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

                if (!skipDataChecksum)
                {
                    // Order by offset to improve sequential read performance
                    header.ArchivedFiles = header.ArchivedFiles.OrderBy(x => x.Offset).ToList();

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
