/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/* Note: jgit has implemented its own FileStream in WalkRemoteObjectDatabase */

using System;
using System.IO;

namespace GitSharp.Core.Util
{
    public static class FileStreamExtensions
    {
        public static byte[] toArray(this Stream stream)
        {
            try
            {
                // Note: if we can seek, it's likely we have a length
                if (stream.CanSeek)
                {
                    if (stream.Length >= 0)
                    {
                        byte[] r = new byte[stream.Length];
                        IO.ReadFully(stream, r, 0, r.Length);
                        return r;
                    }
                }

                var m = new MemoryStream();
                var buf = new byte[2048];
                int n;
                while ((n = stream.Read(buf, 0, buf.Length)) > 0)
                    m.Write(buf, 0, n);
                return m.ToArray();
            }
            finally
            {
                stream.Dispose(); // [nulltoken] Why the heck is the stream disposed here instead of in the caller method ? Weird.
            }
        }

        public static void Clear(this MemoryStream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);
            ms.Write(new byte[ms.Length], 0, Convert.ToInt32(ms.Length));
            ms.Seek(0, SeekOrigin.Begin);
        }

        public static long available(this Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException();
            }

            return stream.Length - stream.Position;
        }

        public static long skip(this Stream stream, long numberOfBytesToSkip)
        {
            if (numberOfBytesToSkip < 0)
            {
                return 0;
            }

            int totalReadBytes = 0;

            int bufSize = numberOfBytesToSkip <= int.MaxValue ? (int)numberOfBytesToSkip : int.MaxValue;

            var buf = new byte[bufSize];

            int readBytes;

            do
            {
                var numberOfBytesToRead = (int) Math.Min(bufSize, numberOfBytesToSkip);
                readBytes = stream.Read(buf, totalReadBytes, numberOfBytesToRead);

                totalReadBytes += readBytes;
                numberOfBytesToSkip -= readBytes;
            } while (numberOfBytesToSkip > 0 && readBytes > 0);


            return totalReadBytes;
        }
    }
}