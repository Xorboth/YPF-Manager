/*  Source: https://github.com/abrandoned/murmur2/blob/master/MurmurHash2.c
 *
 *  MurmurHash2, by Austin Appleby
 *  Note - This code makes a few assumptions about how your machine behaves -
 *  1. We can read a 4-byte value from any address without crashing
 *  2. sizeof(int) == 4
 *   
 *  And it has a few limitations -
 *   
 *  1.  It will not work incrementally.
 *  2.  It will not produce the same results on little-endian and big-endian
 *      machines.
*/

using System;

namespace Ypf_Manager
{
    class MurmurHash2 : Checksum
    {

        //
        // Variables
        //

        public override string Name => "MurmurHash2";


        //
        // Compute byte array hash with MurmurHash2 algorithm
        //

        public override UInt32 ComputeHash(byte[] data)
        {
            //
            // Original function parameters
            //
            UInt32 seed = 0;
            Int32 len = data.Length;


            //
            // MurmurHash2 compute hash
            //

            // 'm' and 'r' are mixing constants generated offline.
            // They're not really 'magic', they just happen to work well.
            const UInt32 m = 0x5bd1e995;
            const Int32 r = 24;


            // Initialize the hash to a 'random' value
            UInt32 h = seed ^ (UInt32)len;


            // Mix 4 bytes at a time into the hash
            Int32 dataIndex = 0;

            while (len >= 4)
            {
                UInt32 k = BitConverter.ToUInt32(data, dataIndex);

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                dataIndex += 4;
                len -= 4;
            }


            // Handle the last few bytes of the input array
            if (len == 3)
            {
                h ^= (UInt32)(data[dataIndex + 2] << 16);
                len--;
            }

            if (len == 2)
            {
                h ^= (UInt32)(data[dataIndex + 1] << 8);
                len--;
            }

            if (len == 1)
            {
                h ^= data[dataIndex];

                h *= m;
            }

            // Do a few final mixes of the hash to ensure the last few bytes are well-incorporated.
            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }

    }
}
