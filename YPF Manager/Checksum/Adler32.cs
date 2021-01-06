/*  Source: https://github.com/madler/zlib/blob/master/adler32.c
 *
 *  adler32.c -- compute the Adler-32 checksum of a data stream
 *
 *  Copyright (C) 1995-2011, 2016 Mark Adler
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty.  In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *
 *  1. The origin of this software must not be misrepresented; you must not
 *      claim that you wrote the original software. If you use this software
 *      in a product, an acknowledgment in the product documentation would be
 *      appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *      misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 *
 *  Jean-loup Gailly        Mark Adler
 *  jloup@gzip.org          madler@alumni.caltech.edu
 *
 *  The data format used by the zlib library is described by RFCs (Request for
 *  Comments) 1950 to 1952 in the files http://tools.ietf.org/html/rfc1950
 *  (zlib format), rfc1951 (deflate format) and rfc1952 (gzip format).
*/

using System;

namespace Ypf_Manager
{

    public class Adler32 : Checksum
    {
        public override string Name => "Adler32";

        /* largest prime smaller than 65536 */
        const UInt32 BASE = 65521;

        const Int32 NMAX = 5552;

        public override UInt32 ComputeHash(byte[] data)
        {
            UInt32 adler = 1;
            Int32 dataIndex = 0;
            Int32 len = data.Length;

            /* split Adler-32 into component sums */
            UInt32 sum2 = (adler >> 16) & 0xffff;
            adler &= 0xffff;

            /* in case short lengths are provided, keep it somewhat fast */
            if (len < 16)
            {
                while (0 != len--)
                {
                    adler += data[dataIndex++];
                    sum2 += adler;
                }
                if (adler >= BASE)
                    adler -= BASE;

                /* only added so many BASE's */
                sum2 %= BASE;
                return adler | (sum2 << 16);
            }

            /* do length NMAX blocks -- requires just one modulo operation */
            while (len >= NMAX)
            {
                len -= NMAX;

                /* NMAX is divisible by 16 */
                Int32 n = NMAX / 16;
                do
                {
                    /* 16 sums unrolled */

                    // DO16(buf);
                    adler += data[dataIndex]; sum2 += adler;
                    adler += data[dataIndex + 1]; sum2 += adler;
                    adler += data[dataIndex + 2]; sum2 += adler;
                    adler += data[dataIndex + 3]; sum2 += adler;
                    adler += data[dataIndex + 4]; sum2 += adler;
                    adler += data[dataIndex + 5]; sum2 += adler;
                    adler += data[dataIndex + 6]; sum2 += adler;
                    adler += data[dataIndex + 7]; sum2 += adler;
                    adler += data[dataIndex + 8]; sum2 += adler;
                    adler += data[dataIndex + 9]; sum2 += adler;
                    adler += data[dataIndex + 10]; sum2 += adler;
                    adler += data[dataIndex + 11]; sum2 += adler;
                    adler += data[dataIndex + 12]; sum2 += adler;
                    adler += data[dataIndex + 13]; sum2 += adler;
                    adler += data[dataIndex + 14]; sum2 += adler;
                    adler += data[dataIndex + 15]; sum2 += adler;

                    dataIndex += 16;
                } while (0 != --n);
                adler %= BASE;
                sum2 %= BASE;
            }

            /* do remaining bytes (less than NMAX, still just one modulo) */
            if (0 != len)
            {
                /* avoid modulos if none remaining */
                while (len >= 16)
                {
                    len -= 16;

                    //DO16(buf);
                    adler += data[dataIndex]; sum2 += adler;
                    adler += data[dataIndex + 1]; sum2 += adler;
                    adler += data[dataIndex + 2]; sum2 += adler;
                    adler += data[dataIndex + 3]; sum2 += adler;
                    adler += data[dataIndex + 4]; sum2 += adler;
                    adler += data[dataIndex + 5]; sum2 += adler;
                    adler += data[dataIndex + 6]; sum2 += adler;
                    adler += data[dataIndex + 7]; sum2 += adler;
                    adler += data[dataIndex + 8]; sum2 += adler;
                    adler += data[dataIndex + 9]; sum2 += adler;
                    adler += data[dataIndex + 10]; sum2 += adler;
                    adler += data[dataIndex + 11]; sum2 += adler;
                    adler += data[dataIndex + 12]; sum2 += adler;
                    adler += data[dataIndex + 13]; sum2 += adler;
                    adler += data[dataIndex + 14]; sum2 += adler;
                    adler += data[dataIndex + 15]; sum2 += adler;

                    dataIndex += 16;
                }
                while (0 != len--)
                {
                    adler += data[dataIndex++];
                    sum2 += adler;
                }
                adler %= BASE;
                sum2 %= BASE;
            }

            /* return recombined sums */
            return adler | (sum2 << 16);
        }
    }
}
