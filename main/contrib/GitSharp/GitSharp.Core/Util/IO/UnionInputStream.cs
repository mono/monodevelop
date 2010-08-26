/*
 * Copyright (C) 2009, Google Inc.
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

namespace GitSharp.Core.Util
{
    /// <summary>
    /// An InputStream which reads from one or more InputStreams.
    /// <para/>
    /// This stream may enter into an EOF state, returning -1 from any of the read
    /// methods, and then later successfully read additional bytes if a new
    /// InputStream is added after reaching EOF.
    /// <para/>
    /// Currently this stream does not support the mark/reset APIs. If mark and later
    /// reset functionality is needed the caller should wrap this stream with a
    /// {@link java.io.BufferedInputStream}.
    /// </summary>
    public class UnionInputStream : DumbStream
    {
        private static readonly Stream Eof = new EofStream();

        private class EofStream : DumbStream
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }
        };

        private readonly LinkedList<Stream> _streams = new LinkedList<Stream>();

        /// <summary>
        /// Create an empty InputStream that is currently at EOF state.
        /// </summary>
        public UnionInputStream()
        {
            // Do nothing.
        }

        /// <summary>
        /// Create an InputStream that is a union of the individual streams.
        /// <para/>
        /// As each stream reaches EOF, it will be automatically closed before bytes
        /// from the next stream are read.
        /// </summary>
        /// <param name="inputStreams">streams to be pushed onto this stream.</param>
        public UnionInputStream(params Stream[] inputStreams)
        {
            foreach (Stream i in inputStreams)
                add(i);
        }

        private Stream head()
        {
            return _streams.Count == 0 ? Eof : _streams.First.Value;
        }

        private void pop()
        {
            if (_streams.Count != 0)
            {
                Stream stream = _streams.First.Value;
                _streams.RemoveFirst();
                stream.Dispose();
            }
        }

        /// <summary>
        /// Add the given InputStream onto the end of the stream queue.
        /// <para/>
        /// When the stream reaches EOF it will be automatically closed.
        /// </summary>
        /// <param name="in">the stream to add; must not be null.</param>
        public void add(Stream @in)
        {
            _streams.AddLast(@in);
        }

        /// <summary>
        /// Returns true if there are no more InputStreams in the stream queue.
        /// <para/>
        /// If this method returns <code>true</code> then all read methods will signal EOF
        /// by returning -1, until another InputStream has been pushed into the queue
        /// with <see cref="add"/>.
        /// </summary>
        /// <returns>true if there are no more streams to read from.</returns>
        public bool isEmpty()
        {
            return _streams.Count == 0;
        }

        public int read()
        {
            return ReadByte();
        }

        public override int ReadByte()
        {
            for (; ; )
            {
                Stream @in = head();
                int r = @in.ReadByte();
                if (0 <= r)
                    return r;
                else if (@in == Eof)
                    return -1;
                else
                    pop();
            }
        }

        public override int Read(byte[] b, int off, int len)
        {
            int cnt = 0;
            while (0 < len)
            {
                Stream @in = head();
                int n = @in.Read(b, off, len);
                if (0 < n)
                {
                    cnt += n;
                    off += n;
                    len -= n;
                }
                else if (@in == Eof)
                    return 0 < cnt ? cnt : -1;
                else
                    pop();
            }
            return cnt;
        }

        public int available()
        {
            return (int)head().available();
        }

        public long skip(long len)
        {
            long cnt = 0;
            while (0 < len)
            {
                Stream @in = head();
                long n = @in.skip(len);
                if (0 < n)
                {
                    cnt += n;
                    len -= n;

                }
                else if (@in == Eof)
                {
                    return cnt;

                }
                else
                {
                    // Is this stream at EOF? We can't tell from skip alone.
                    // Read one byte to test for EOF, discard it if we aren't
                    // yet at EOF.
                    //
                    int r = @in.ReadByte();
                    if (r < 0)
                    {
                        pop();
                    }
                    else
                    {
                        cnt += 1;
                        len -= 1;
                    }
                }
            }
            return cnt;
        }

        public override void Close()
        {
            IOException err = null;

            for (var i = new LinkedListIterator<Stream>(_streams); i.hasNext(); )
            {
                try
                {
                    i.next().Dispose();
                }
                catch (IOException closeError)
                {
                    err = closeError;
                }
                i.remove();
            }

            if (err != null)
                throw err;
        }
    
        public bool markSupported()
        {
            return false; // TODO : CanSeek ?  
        }
    }

    public abstract class DumbStream : Stream
    {
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }
    }
}
