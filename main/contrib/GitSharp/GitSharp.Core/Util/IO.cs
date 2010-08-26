/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitSharp.Core.Util
{
    /// <summary>
    /// Input/Output utilities
    /// </summary>
    public static class IO
    {
        /// <summary>
        /// Read an entire local file into memory as a byte array.
        /// </summary>
        /// <param name="path">Location of the file to read.</param>
        /// <returns>Complete contents of the requested local file.</returns>
        /// <exception cref="IOException">
        /// The file exists, but its contents cannot be read.
        /// </exception>
        public static byte[] ReadFully(FileInfo path)
        {
            return ReadFully(path, int.MaxValue);
        }

        /// <summary>
        /// Read an entire local file into memory as a byte array.
        /// </summary>
        /// <param name="path">Location of the file to read.</param>
        /// <param name="max">
        /// Maximum number of bytes to Read, if the file is larger than
        /// this limit an IOException is thrown.
        /// </param>
        /// <returns>
        /// Complete contents of the requested local file.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// The file exists, but its contents cannot be Read.
        /// </exception>
        /// <exception cref="IOException"></exception>
        public static byte[] ReadFully(FileInfo path, int max)
        {
            using (var @in = new FileStream(path.FullName, System.IO.FileMode.Open, FileAccess.Read))
            {
                long sz = @in.Length;
                if (sz > max)
                    throw new IOException("File is too large: " + path);
                var buf = new byte[(int)sz];
                ReadFully(@in, buf, 0, buf.Length);
                return buf;
            }
        }

        /// <summary>
        /// Read the entire byte array into memory, or throw an exception.
        /// </summary>
        /// <param name="fd">Input stream to read the data from.</param>
        /// <param name="dst">buffer that must be fully populated</param>
        /// <param name="off">position within the buffer to start writing to.</param>
        /// <param name="len">number of bytes that must be read.</param>
        /// <exception cref="EndOfStreamException">
        /// The stream ended before <paramref name="dst"/> was fully populated.
        /// </exception>
        /// <exception cref="IOException">
        /// There was an error reading from the stream.
        /// </exception>
        public static void ReadFully(Stream fd, byte[] dst, int off, int len)
        {
            while (len > 0)
            {
                int r = fd.Read(dst, off, len);
                if (r <= 0)
                    throw new EndOfStreamException("Short Read of block.");
                off += r;
                len -= r;
            }
        }

        /// <summary>
        /// Read the entire byte array into memory, or throw an exception.
        /// </summary>
        /// <param name="fd">Stream to read the data from.</param>
        /// <param name="pos">Position to read from the file at.</param>
        /// <param name="dst">Buffer that must be fully populated, [off, off+len].</param>
        /// <param name="off">position within the buffer to start writing to.</param>
        /// <param name="len">number of bytes that must be read.</param>
        /// <exception cref="EndOfStreamException">
        /// The <paramref name="fd"/> ended before the requested number of 
        /// bytes were read.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The <paramref name="fd"/> does not supports seeking.
        /// </exception>
        /// <exception cref="IOException">
        /// There was an error reading from the stream.
        /// </exception>
        public static void ReadFully(Stream fd, long pos, byte[] dst, int off, int len)
        {
            while (len > 0)
            {
                fd.Position = pos;
                int r = fd.Read(dst, off, len);
                if (r <= 0)
                    throw new EndOfStreamException("Short Read of block.");
                pos += r;
                off += r;
                len -= r;
            }
        }

        /// <summary>
        /// Skip an entire region of an input stream.
        /// <para />
        /// The input stream's position is moved forward by the number of requested
        /// bytes, discarding them from the input. This method does not return until
        /// the exact number of bytes requested has been skipped.
        /// </summary>
        /// <param name="fd">The stream to skip bytes from.</param>
        /// <param name="toSkip">
        /// Total number of bytes to be discarded. Must be >= 0.
        /// </param>
        /// <exception cref="EndOfStreamException">
        /// The stream ended before the requested number of bytes were
        /// skipped.
        /// </exception>
        /// <exception cref="IOException">
        /// There was an error reading from the stream.
        /// </exception>
        public static void skipFully(Stream fd, long toSkip)
        {
            while (toSkip > 0)
            {
                var r = fd.Seek(toSkip, SeekOrigin.Current);
                if (r <= 0)
                    throw new EndOfStreamException("Short skip of block");
                toSkip -= r;
            }
        }
    }
}
