﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ypf_Manager
{
    class YPFHeader
    {

        //
        // Variable(s)
        //

        // YPF\0
        public byte[] Signature => new byte[] { 0x59, 0x50, 0x46, 0x00 };

        private Int32 _version;

        public Int32 Version
        {
            get { return _version; }
            set
            {
                if (value < 234 || value > 500)
                {
                    throw new Exception($"Version {value} not supported");
                }

                _version = value;

                SetLengthSwappingTable();
                SetChecksum();
                AssumeFileNameEncryptionKey();
            }
        }

        public Int32 ArchivedFilesHeaderSize { get; set; }

        //Shift-JIS
        public Encoding Encoding => Encoding.GetEncoding(932);

        public List<YPFEntry> ArchivedFiles { get; set; }

        public Checksum NameChecksum { get; set; }

        public Checksum DataChecksum { get; set; }

        public Byte FileNameEncryptionKey { get; set; }

        public Byte[] LengthSwappingTable;


        //
        // Constructor(s)
        //

        public YPFHeader()
        {
            ArchivedFiles = new List<YPFEntry>();
        }


        public YPFHeader(int version)
        {
            ArchivedFiles = new List<YPFEntry>();

            Version = version;
        }


        public YPFHeader(Stream inputStream)
        {
            BinaryReader inputBinaryReader = new BinaryReader(inputStream);

            ArchivedFiles = new List<YPFEntry>();

            if (!Enumerable.SequenceEqual(Signature, inputBinaryReader.ReadBytes(4)))
            {
                throw new Exception("Invalid Archive Signature");
            }

            Version = inputBinaryReader.ReadInt32();
            int filesCount = inputBinaryReader.ReadInt32();
            ArchivedFilesHeaderSize = inputBinaryReader.ReadInt32();

            inputBinaryReader.BaseStream.Position += 16;

            if (filesCount <= 0)
            {
                throw new Exception("Invalid Files Count");
            }

            if (ArchivedFilesHeaderSize <= 0)
            {
                throw new Exception("Invalid Archived Files Header Size");
            }

            ArchivedFiles.Capacity = filesCount;

            for (int i = 0; i < filesCount; i++)
            {
                ArchivedFiles.Add(ReadNextEntry(inputBinaryReader));
            }
        }


        //
        // Function(s)
        //

        // Set length swapping table
        public void SetLengthSwappingTable()
        {
            if (Version >= 500)
            {
                LengthSwappingTable = new byte[] { 0x00, 0x01, 0x02, 0x0A, 0x04, 0x05, 0x35, 0x07, 0x08, 0x0B, 0x03, 0x09, 0x10, 0x13, 0x0E, 0x0F, 0x0C, 0x18, 0x12, 0x0D, 0x2E, 0x1B, 0x16, 0x17, 0x11, 0x19, 0x1A, 0x15, 0x1E, 0x1D, 0x1C, 0x1F, 0x23, 0x21, 0x22, 0x20, 0x24, 0x25, 0x29, 0x27, 0x28, 0x26, 0x2A, 0x2B, 0x2F, 0x2D, 0x14, 0x2C, 0x30, 0x31, 0x32, 0x33, 0x34, 0x06, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF };
            }
            else
            {
                LengthSwappingTable = new byte[] { 0x00, 0x01, 0x02, 0x48, 0x04, 0x05, 0x35, 0x07, 0x08, 0x0B, 0x0A, 0x09, 0x10, 0x13, 0x0E, 0x0F, 0x0C, 0x19, 0x12, 0x0D, 0x14, 0x1B, 0x16, 0x17, 0x18, 0x11, 0x1A, 0x15, 0x1E, 0x1D, 0x1C, 0x1F, 0x23, 0x21, 0x22, 0x20, 0x24, 0x25, 0x29, 0x27, 0x28, 0x26, 0x2A, 0x2B, 0x2F, 0x2D, 0x32, 0x2C, 0x30, 0x31, 0x2E, 0x33, 0x34, 0x06, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x03, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF };
            }
        }


        // Set the checksum algorithms
        public void SetChecksum()
        {
            if (Version < 479)
            {
                DataChecksum = new Adler32();
                NameChecksum = new Crc32();
            }
            else
            {
                DataChecksum = new MurmurHash2();
                NameChecksum = new MurmurHash2();
            }
        }


        // Assume filename xor encryption key (since some versions may have multiple keys)
        public void AssumeFileNameEncryptionKey()
        {
            if (Version == 290)
            {
                FileNameEncryptionKey = 0x40;
            }
            else if (Version >= 500)
            {
                FileNameEncryptionKey = 0x36;
            }
            else
            {
                FileNameEncryptionKey = 0x00;
            }
        }


        // Check if data matches the providen checksum
        public void ValidateDataChecksum(Stream inputStream, Int32 length, UInt32 checksum)
        {
            UInt32 calculatedDataChecksum = DataChecksum.ComputeHash(inputStream, length);

            if (checksum != calculatedDataChecksum)
            {
                throw new Exception("Invalid Data Checksum");
            }

            inputStream.Position = 0;
        }


        // Check if name matches the providen checksum
        public void ValidateNameChecksum(Byte[] inputArray, UInt32 checksum)
        {
            UInt32 calculatedNameChecksum = NameChecksum.ComputeHash(inputArray);

            if (calculatedNameChecksum != checksum)
            {
                throw new Exception("Invalid Name Checksum");
            }
        }


        // Read the next entry from the provided file
        public YPFEntry ReadNextEntry(BinaryReader inputBinaryReader)
        {
            YPFEntry entry = new YPFEntry();

            entry.NameChecksum = inputBinaryReader.ReadUInt32();

            Byte fileNameLengthEncoded = Util.OneComplement(inputBinaryReader.ReadByte());
            Byte fileNameLengthDecoded = LengthSwappingTable[fileNameLengthEncoded];

            Byte[] fileNameEncoded = inputBinaryReader.ReadBytes(fileNameLengthDecoded);

            for (int j = 0; j < fileNameLengthDecoded; j++)
            {
                fileNameEncoded[j] = (byte)(Util.OneComplement(fileNameEncoded[j]) ^ FileNameEncryptionKey);
            }

            entry.FileName = Encoding.GetString(fileNameEncoded);
            entry.Type = (YPFEntry.FileType)inputBinaryReader.ReadByte();
            entry.IsCompressed = (inputBinaryReader.ReadByte() == 1);
            entry.RawFileSize = inputBinaryReader.ReadInt32();
            entry.CompressedFileSize = inputBinaryReader.ReadInt32();

            if (Version < 479)
            {
                entry.Offset = inputBinaryReader.ReadInt32();
            }
            else
            {
                entry.Offset = inputBinaryReader.ReadInt64();
            }

            entry.DataChecksum = inputBinaryReader.ReadUInt32();

            ValidateNameChecksum(fileNameEncoded, entry.NameChecksum);

            if (!Enum.IsDefined(typeof(YPFEntry.FileType), entry.Type))
            {
                throw new Exception("Unexpected File Type");
            }

            return entry;
        }


        // Find a duplicate file entry with the same checksum and size
        public YPFEntry FindDuplicateEntry(UInt32 fileChecksum, Int32 fileSize)
        {
            return ArchivedFiles.FirstOrDefault(x => x.DataChecksum == fileChecksum && x.RawFileSize == fileSize);
        }

    }
}
