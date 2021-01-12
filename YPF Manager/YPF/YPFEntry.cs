using System;

namespace Ypf_Manager
{
    class YPFEntry
    {
        public String FileName { get; set; }

        public UInt32 NameChecksum { get; set; }

        public UInt32 DataChecksum { get; set; }

        public Int32 CompressedFileSize { get; set; }

        public Int32 RawFileSize { get; set; }

        public Boolean IsCompressed { get; set; }

        public FileType Type { get; set; }

        public Int64 Offset { get; set; }

        public enum FileType
        {
            text = 0,
            bmp = 1,
            png = 2,
            jpg = 3,
            gif = 4,
            wav = 5,
            ogg = 6,
            psd = 7,
            ycg = 8, //masked as .png
            psb = 9
        }

    }
}
