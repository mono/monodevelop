/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System;
using System.Collections.Generic;
using System.IO;

namespace GitSharp.Core.Util
{
    /// <summary>
    ///   A fully buffered output stream.
    ///   <para />
    ///   Subclasses determine the behavior when the in-memory buffer capacity has been
    ///   exceeded and additional bytes are still being received for output.
    /// </summary>
    public abstract class TemporaryBuffer : IDisposable
    {
        public static int DEFAULT_IN_CORE_LIMIT = 1024 * 1024;

        // Chain of data, if we are still completely in-core; otherwise null.
        private List<Block> _blocks;

        /// <summary>
        ///   Maximum number of bytes we will permit storing in memory.
        ///   <para />
        ///   When this limit is reached the data will be shifted to a file on disk,
        ///   preventing the JVM heap from growing out of control.
        /// </summary>
        private readonly int inCoreLimit;

        /// <summary>
        ///   If
        ///   <see cref="inCoreLimit" />
        ///   has been reached, remainder goes here.
        /// </summary>
        private Stream _overflow;


        /// <summary>
        ///   Create a new empty temporary buffer.
        /// </summary>
        /// <param name="limit">
        ///   maximum number of bytes to store in memory before entering the overflow output path.
        /// </param>
        protected TemporaryBuffer(int limit)
        {
            inCoreLimit = limit;
            reset();
        }

        public void write(int b)
        {
            if (_overflow != null)
            {
                _overflow.WriteByte((byte)b);
                return;
            }

            Block s = last();
            if (s.isFull())
            {
                if (reachedInCoreLimit())
                {
                    _overflow.WriteByte((byte)b);
                    return;
                }

                s = new Block();
                _blocks.Add(s);
            }
            s.buffer[s.count++] = (byte)b;
        }

        public void write(byte[] b, int off, int len)
        {
            if (_overflow == null)
            {
                while (len > 0)
                {
                    Block s = last();
                    if (s.isFull())
                    {
                        if (reachedInCoreLimit())
                            break;

                        s = new Block();
                        _blocks.Add(s);
                    }

                    int n = Math.Min(Block.SZ - s.count, len);
                    Array.Copy(b, off, s.buffer, s.count, n);
                    s.count += n;
                    len -= n;
                    off += n;
                }
            }

            if (len > 0)
                _overflow.Write(b, off, len);
        }


        public void write(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Copy all bytes remaining on the input stream into this buffer.
        /// </summary>
        /// <param name="in">the stream to Read from, until EOF is reached.</param>
        public void copy(Stream @in)
        {
            if (@in == null)
                throw new ArgumentNullException("in");

            if (_blocks != null)
            {
                for (; ; )
                {
                    Block s = last();
                    if (s.isFull())
                    {
                        if (reachedInCoreLimit())
                            break;
                        s = new Block();
                        _blocks.Add(s);
                    }

                    int n = @in.Read(s.buffer, s.count, Block.SZ - s.count);
                    if (n < 1)
                        return;
                    s.count += n;
                }
            }

            byte[] tmp = new byte[Block.SZ];
            int nn;
            while ((nn = @in.Read(tmp, 0, tmp.Length)) > 0)
                _overflow.Write(tmp, 0, nn);
        }

        /// <summary>
        /// Obtain the length (in bytes) of the buffer.
        /// <para />
        /// The length is only accurate After <see cref="close()"/> has been invoked.
        /// </summary>
        public virtual long Length
        {
            get
            {
                Block last = this.last();
                return ((long)_blocks.Count) * Block.SZ - (Block.SZ - last.count);
            }
        }

        /// <summary>
        /// Convert this buffer's contents into a contiguous byte array.
        /// <para />
        /// The buffer is only complete After {@link #close()} has been invoked.
        /// </summary>
        /// <returns>the complete byte array; length matches <see cref="Length"/>.</returns>
        public virtual byte[] ToArray()
        {
            long len = Length;
            if (int.MaxValue < len)
                throw new OutOfMemoryException("Length exceeds maximum array size");

            byte[] @out = new byte[(int)len];
            int outPtr = 0;
            foreach (Block b in _blocks)
            {
                Array.Copy(b.buffer, 0, @out, outPtr, b.count);
                outPtr += b.count;
            }
            return @out;
        }

        /// <summary>
        /// Send this buffer to an output stream.
        /// <para />
        /// This method may only be invoked After {@link #close()} has completed
        /// normally, to ensure all data is completely transferred.
        /// </summary>
        /// <param name="os">stream to send this buffer's complete content to.</param>
        /// <param name="pm">
        /// if not null progress updates are sent here. Caller should
        /// initialize the task and the number of work units to
        /// <code><see cref="Length"/>/1024</code>.
        /// </param>
        public virtual void writeTo(Stream os, ProgressMonitor pm)
        {
            if (os == null)
                throw new ArgumentNullException("os");
            if (pm == null)
                pm = new NullProgressMonitor();
            foreach (Block b in _blocks)
            {
                os.Write(b.buffer, 0, b.count);
                pm.Update(b.count / 1024);
            }
        }


        /// <summary>
        /// Reset this buffer for reuse, purging all buffered content.
        /// </summary>
        public void reset()
        {
            if (_overflow != null)
            {
                destroy();
            }
            _blocks = new List<Block>(inCoreLimit / Block.SZ);
            _blocks.Add(new Block());
        }


        /// <summary>
        /// Open the overflow output stream, so the remaining output can be stored.
        /// </summary>
        /// <returns>
        /// the output stream to receive the buffered content, followed by
        /// the remaining output.
        /// </returns>
        protected abstract Stream overflow();

        private Block last()
        {
            return _blocks[_blocks.Count - 1];
        }

        private bool reachedInCoreLimit()
        {
            if (_blocks.Count * Block.SZ < inCoreLimit)
                return false;

            _overflow = overflow();

            Block last = _blocks[_blocks.Count - 1];
            _blocks.RemoveAt(_blocks.Count - 1);
            foreach (Block b in _blocks)
                _overflow.Write(b.buffer, 0, b.count);
            _blocks = null;

            _overflow = new BufferedStream(_overflow, Block.SZ);
            _overflow.Write(last.buffer, 0, last.count);
            return true;
        }

        public virtual void close()
        {
            if (_overflow != null)
            {
                try
                {
                    _overflow.Close();
                }
                finally
                {
                    _overflow = null;
                }
            }
#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

        public void Dispose()
        {
            close();
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~TemporaryBuffer()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed.");
        }
#endif

        /// <summary>
        ///   Clear this buffer so it has no data, and cannot be used again.
        /// </summary>
        public virtual void destroy()
        {
            _blocks = null;

            close();

            if (_overflow != null)
            {
                try
                {
                    _overflow.Close();
                }
                catch (IOException)
                {
                    // We shouldn't encounter an error closing the file.
                }
                finally
                {
                    _overflow = null;
                }
            }
        }
    }

    /// <summary>
    ///   A fully buffered output stream using local disk storage for large data.
    ///   <para />
    ///   Initially this output stream buffers to memory and is therefore similar
    ///   to ByteArrayOutputStream, but it shifts to using an on disk temporary
    ///   file if the output gets too large.
    ///   <para />
    ///   The content of this buffered stream may be sent to another OutputStream
    ///   only after this stream has been properly closed by
    ///   <see cref="TemporaryBuffer.close" />
    ///   .
    /// </summary>
    public class LocalFileBuffer : TemporaryBuffer
    {
        /// <summary>
        ///   Location of our temporary file if we are on disk; otherwise null.
        ///   <para />
        ///   If we exceeded the {@link #inCoreLimit} we nulled out {@link #blocks}
        ///   and created this file instead. All output goes here through
        ///   {@link #overflow}.
        /// </summary>
        private FileInfo onDiskFile;

        /// <summary>
        ///   Create a new temporary buffer.
        /// </summary>
        public LocalFileBuffer()
            : this(DEFAULT_IN_CORE_LIMIT)
        {
        }

        /// <summary>
        ///   Create a new temporary buffer, limiting memory usage.
        /// </summary>
        /// <param name="inCoreLimit">
        ///   maximum number of bytes to store in memory. Storage beyond
        ///   this limit will use the local file.
        /// </param>
        public LocalFileBuffer(int inCoreLimit)
            : base(inCoreLimit)
        {
        }

        protected override Stream overflow()
        {
            onDiskFile = new FileInfo("gitsharp_" + Path.GetRandomFileName() + ".buffer");
            return new FileStream(onDiskFile.FullName, System.IO.FileMode.CreateNew, FileAccess.Write);
        }

        public override long Length
        {
            get
            {
                if (onDiskFile == null)
                {
                    return base.Length;
                }
                return onDiskFile.Length;
            }
        }

        public override byte[] ToArray()
        {
            if (onDiskFile == null)
            {
                return base.ToArray();
            }

            long len = Length;
            if (int.MaxValue < len)
                throw new OutOfMemoryException("Length exceeds maximum array size");
            byte[] @out = new byte[(int)len];

            //using (var @in = new FileInputStream(onDiskFile.FullName))
			using (var @in = new FileStream(onDiskFile.FullName, System.IO.FileMode.Open))
            {
                IO.ReadFully(@in, @out, 0, (int)len);
            }

            return @out;
        }

        public override void writeTo(Stream os, ProgressMonitor pm)
        {
            if (onDiskFile == null)
            {
                base.writeTo(os, pm);
                return;
            }
            if (pm == null)
                pm = NullProgressMonitor.Instance;
            using (FileStream @in = new FileStream(onDiskFile.FullName, System.IO.FileMode.Open, FileAccess.Read))
            {
                int cnt;
                byte[] buf = new byte[Block.SZ];
                while ((cnt = @in.Read(buf, 0, buf.Length)) > 0)
                {
                    os.Write(buf, 0, cnt);
                    pm.Update(cnt / 1024);
                }
            }
        }

        public override void destroy()
        {
            base.destroy();

            if (onDiskFile != null)
            {
                try
                {
                    onDiskFile.DeleteFile();
                }
                finally
                {
                    onDiskFile = null;
                }
            }
        }
    }

    /// <summary>
    ///   A temporary buffer that will never exceed its in-memory limit.
    ///   <para />
    ///   If the in-memory limit is reached an IOException is thrown, rather than
    ///   attempting to spool to local disk.
    /// </summary>
    public class HeapBuffer : TemporaryBuffer
    {
        /// <summary>
        ///   Create a new heap buffer with a maximum storage limit.
        /// </summary>
        /// <param name="limit">
        ///   maximum number of bytes that can be stored in this buffer.
        ///   Storing beyond this many will cause an IOException to be
        ///   thrown during write.
        /// </param>
        public HeapBuffer(int limit)
            : base(limit)
        {
        }

        protected override Stream overflow()
        {
            throw new IOException("In-memory buffer limit exceeded");
        }
    }

    public class Block
    {
        public static int SZ = 8 * 1024;

        public byte[] buffer = new byte[SZ];

        public int count;

        public bool isFull()
        {
            return count == SZ;
        }
    }
}