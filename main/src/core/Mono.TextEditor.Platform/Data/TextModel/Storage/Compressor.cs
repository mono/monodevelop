using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal static class Compressor
    {
#if COMPRESSOR_TIMING
        static int compressCount;
        static int decompressCount;
        static long compressMilliseconds;
        static long decompressMilliseconds;
        static long averageCompressMilliseconds;
        static long averageDecompressMilliseconds;
#endif
        public static byte[] Compress(char[] buffer, int length)
        {
            byte[] result;
#if COMPRESSOR_TIMING
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif
            using (var inflatedBytes = new CharStream(buffer, length))
            {
                using (var deflatedBytes = new MemoryStream(length / 9))    // guess size of compressed text
                {
                    using (DeflateStream compress = new DeflateStream(deflatedBytes, CompressionMode.Compress))
                    {
                        inflatedBytes.CopyTo(compress);
                    }
                    result = deflatedBytes.GetBuffer();
                }
            }

#if COMPRESSOR_TIMING
            compressCount++;
            compressMilliseconds += watch.ElapsedMilliseconds;
            averageCompressMilliseconds = compressMilliseconds / compressCount;
#endif
            return result;
        }

        public static void Decompress(byte[] compressed, int length, char[] decompressed)
        {
#if COMPRESSOR_TIMING
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif

            using (var deflatedBytes = new MemoryStream(compressed))
            {
                using (var inflatedChars = new CharStream(decompressed, length))
                {
                    using (DeflateStream decompress = new DeflateStream(deflatedBytes, CompressionMode.Decompress))
                    {
                        decompress.CopyTo(inflatedChars);
                    }
                }
            }

#if COMPRESSOR_TIMING
            decompressCount++;
            decompressMilliseconds += watch.ElapsedMilliseconds;
            averageDecompressMilliseconds = decompressMilliseconds / decompressCount;
#endif
        }
    }
}
